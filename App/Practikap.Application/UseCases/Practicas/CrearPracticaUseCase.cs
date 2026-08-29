using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Practicas;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Practicas;

/// <summary>
/// Da de alta una practica productiva. Reservado al Administrador (RF-03, H17).
/// </summary>
public sealed class CrearPracticaUseCase
{
    private readonly IPracticaRepository _practicaRepo;
    private readonly IFichaRepository _fichaRepo;
    private readonly IEmpresaRepository _empresaRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearPracticaRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CrearPracticaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="practicaRepo">Acceso a practicas.</param>
    /// <param name="fichaRepo">Acceso a fichas de formacion.</param>
    /// <param name="empresaRepo">Acceso a empresas receptoras.</param>
    /// <param name="usuarioRepo">Acceso a usuarios, para verificar los participantes (H7).</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CrearPracticaUseCase(
        IPracticaRepository practicaRepo,
        IFichaRepository fichaRepo,
        IEmpresaRepository empresaRepo,
        IUsuarioRepository usuarioRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearPracticaRequest> validador,
        IMapper mapeador,
        ILogger<CrearPracticaUseCase> registro)
    {
        _practicaRepo = practicaRepo;
        _fichaRepo = fichaRepo;
        _empresaRepo = empresaRepo;
        _usuarioRepo = usuarioRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Crea la practica y devuelve sus datos.</summary>
    /// <param name="request">Datos de la practica a crear.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La practica creada, ya con su identificador asignado.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="ConflictoException">Si el aprendiz ya tiene una practica activa (409).</exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la ficha o la empresa no existen, si los participantes no cumplen H7 o
    /// si la combinacion de modalidad y empresa es incoherente (422).
    /// </exception>
    public async Task<PracticaResponse> ExecuteAsync(CrearPracticaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        // H17: solo el Administrador crea practicas. La barrera del atributo es la
        // primera; esta es la segunda, y es la que hace la regla verificable sin
        // servidor web (ADR-03, RNF-08).
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede crear practicas.");

        if (await _fichaRepo.ObtenerPorIdAsync(request.FichaId, ct) is null)
            throw new ReglaDeDominioException("La ficha de formacion indicada no existe.");

        if (request.EmpresaId is not null
            && await _empresaRepo.ObtenerPorIdAsync(request.EmpresaId.Value, ct) is null)
            throw new ReglaDeDominioException("La empresa indicada no existe.");

        await ParticipantesDePractica.VerificarAsync(
            _usuarioRepo, request.InstructorId, request.AprendizId, ct);

        // RN-04. No puede garantizarse con un indice unico porque MySQL no admite
        // indices unicos parciales, asi que la verificacion previa es lo unico que
        // sostiene la regla.
        if (await _practicaRepo.TieneActivaAsync(request.AprendizId, ct))
            throw new ConflictoException(
                $"El aprendiz {request.AprendizId} ya tiene una practica sin finalizar.");

        // El validador ya confirmo que el literal es uno de los cuatro (H31).
        var modalidad = Enum.Parse<ModalidadPractica>(request.Modalidad);

        // El constructor aplica H22 y H25 sobre modalidad y empresa, y valida el
        // rango de fechas. La practica nace en estado Pendiente.
        var practica = new Practica(
            request.FichaId,
            request.InstructorId,
            request.AprendizId,
            modalidad,
            request.FechaInicio,
            request.EmpresaId,
            request.FechaFin);

        await _practicaRepo.AgregarAsync(practica, ct);

        // Hasta aqui practica.Id vale 0. Es la confirmacion la que le asigna el
        // valor real.
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Practica {PracticaId} creada para el aprendiz {AprendizId} con el instructor {InstructorId}.",
            practica.Id, practica.AprendizId, practica.InstructorId);

        // Se relee para que el grafo quede cargado: la instancia recien insertada
        // se construyo con identificadores, no con navegaciones, y el perfil de
        // AutoMapper aplana Ficha, Programa, Empresa, Instructor y Aprendiz.
        var creada = await _practicaRepo.ObtenerPorIdAsync(practica.Id, ct);
        return _mapeador.Map<PracticaResponse>(creada!);
    }
}
