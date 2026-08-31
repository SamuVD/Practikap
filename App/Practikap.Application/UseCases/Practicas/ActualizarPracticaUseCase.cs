using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Practicas;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Practicas;

/// <summary>
/// Reasigna los participantes de una practica y cambia su modalidad. Reservado
/// al Administrador (RF-03, H17).
/// </summary>
/// <remarks>
/// H29 acota el alcance: no edita fechas. La practica se carga rastreada, se
/// modifica con sus metodos de dominio y el repositorio solo registra el cambio
/// (H28).
/// </remarks>
public sealed class ActualizarPracticaUseCase
{
    private readonly IPracticaRepository _practicaRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IRegistradorDeAuditoria _auditor;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<ActualizarPracticaRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<ActualizarPracticaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="practicaRepo">Acceso a practicas.</param>
    /// <param name="empresaRepo">Acceso a empresas receptoras.</param>
    /// <param name="usuarioRepo">Acceso a usuarios, para verificar los participantes (H7).</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="auditor">Bitacora de acciones sensibles (P12, P13).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public ActualizarPracticaUseCase(
        IPracticaRepository practicaRepo,
        IEmpresaRepository empresaRepo,
        IUsuarioRepository usuarioRepo,
        IContextoUsuario contexto,
        IRegistradorDeAuditoria auditor,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<ActualizarPracticaRequest> validador,
        IMapper mapeador,
        ILogger<ActualizarPracticaUseCase> registro)
    {
        _practicaRepo = practicaRepo;
        _empresaRepo = empresaRepo;
        _usuarioRepo = usuarioRepo;
        _contexto = contexto;
        _auditor = auditor;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica la reasignacion y el cambio de modalidad.</summary>
    /// <param name="id">Practica afectada.</param>
    /// <param name="request">Participantes y modalidad nuevos.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La practica con sus datos ya actualizados.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="NoEncontradoException">Si la practica no existe (404).</exception>
    /// <exception cref="ConflictoException">Si el aprendiz nuevo ya tiene una practica activa (409).</exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la empresa no existe, si los participantes no cumplen H7 o si la
    /// combinacion de modalidad y empresa es incoherente (422).
    /// </exception>
    public async Task<PracticaResponse> ExecuteAsync(
        int id, ActualizarPracticaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede modificar practicas.");

        var practica = await _practicaRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Practica", id);

        if (request.EmpresaId is not null
            && await _empresaRepo.ObtenerPorIdAsync(request.EmpresaId.Value, ct) is null)
            throw new ReglaDeDominioException("La empresa indicada no existe.");

        await ParticipantesDePractica.VerificarAsync(
            _usuarioRepo, request.InstructorId, request.AprendizId, ct);

        // H5: RN-04 solo se consulta si el aprendiz cambia. Si no cambiara, la
        // practica que estamos editando seria ella misma la que hiciera fallar la
        // verificacion.
        if (request.AprendizId != practica.AprendizId
            && await _practicaRepo.TieneActivaAsync(request.AprendizId, ct))
            throw new ConflictoException(
                $"El aprendiz {request.AprendizId} ya tiene una practica sin finalizar.");

        // El validador ya confirmo que el literal es uno de los cuatro (H31).
        var modalidad = Enum.Parse<ModalidadPractica>(request.Modalidad);

        // Los dos participantes se capturan antes de reasignar, porque Reasignar
        // los pisa. Son ademas los que deciden si hay asiento.
        var instructorAnterior = practica.InstructorId;
        var aprendizAnterior = practica.AprendizId;

        practica.Reasignar(request.InstructorId, request.AprendizId);
        practica.CambiarModalidad(modalidad, request.EmpresaId);

        await _practicaRepo.ActualizarAsync(practica, ct);

        // RN-04, y solo cuando la asignacion cambia de verdad: una edicion que
        // unicamente mueve la modalidad o la empresa no es una reasignacion y no
        // deja rastro. Asentarla igual llenaria la bitacora de filas que dicen que
        // nada se reasigno (P13).
        if (instructorAnterior != practica.InstructorId
            || aprendizAnterior != practica.AprendizId)
        {
            await _auditor.PorReasignacionAsync(
                practica.Id,
                instructorAnterior, practica.InstructorId,
                aprendizAnterior, practica.AprendizId,
                ct);
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Practica {PracticaId} reasignada al instructor {InstructorId} y al aprendiz {AprendizId} "
            + "por el administrador {AdministradorId}.",
            practica.Id, practica.InstructorId, practica.AprendizId, _contexto.UsuarioId);

        // Se relee para que las navegaciones reflejen los participantes y la
        // empresa nuevos, y no los que tenia cargados la instancia rastreada.
        var actualizada = await _practicaRepo.ObtenerPorIdAsync(id, ct);
        return _mapeador.Map<PracticaResponse>(actualizada!);
    }
}
