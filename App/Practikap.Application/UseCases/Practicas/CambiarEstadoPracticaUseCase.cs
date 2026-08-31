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
/// Mueve una practica dentro del ciclo de vida de RN-05. Reservado al
/// Administrador, incluido el retroceso (H17).
/// </summary>
/// <remarks>
/// Es el caso de uso que materializa H28. La validez de la transicion la decide
/// Practica.CambiarEstado, y el indicador de Administrador que necesita para
/// autorizar un retroceso sale de IContextoUsuario, que es la unica fuente
/// legitima: el repositorio no lo conoce y hasta la Ronda 1 lo cableaba en true.
///
/// El Motor de Reglas del paso 4.7 no pasa por aqui: usa Practica.MarcarEnRiesgo
/// sobre la entidad (H9).
/// </remarks>
public sealed class CambiarEstadoPracticaUseCase
{
    private readonly IPracticaRepository _practicaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IRegistradorDeAuditoria _auditor;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CambiarEstadoPracticaRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CambiarEstadoPracticaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="practicaRepo">Acceso a practicas.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="auditor">Bitacora de acciones sensibles (P12, P13, P14).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CambiarEstadoPracticaUseCase(
        IPracticaRepository practicaRepo,
        IContextoUsuario contexto,
        IRegistradorDeAuditoria auditor,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CambiarEstadoPracticaRequest> validador,
        IMapper mapeador,
        ILogger<CambiarEstadoPracticaUseCase> registro)
    {
        _practicaRepo = practicaRepo;
        _contexto = contexto;
        _auditor = auditor;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica la transicion de estado.</summary>
    /// <param name="id">Practica afectada.</param>
    /// <param name="request">Estado destino y, cuando corresponde, fecha de cierre.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La practica con su estado ya actualizado.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="AutorizacionException">
    /// Si el solicitante no tiene alcance Global, o si la transicion es un
    /// retroceso y no es Administrador (403).
    /// </exception>
    /// <exception cref="NoEncontradoException">Si la practica no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si el estado destino coincide con el actual o si la fecha de cierre
    /// precede a la de inicio (422).
    /// </exception>
    public async Task<PracticaResponse> ExecuteAsync(
        int id, CambiarEstadoPracticaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede cambiar el estado de una practica.");

        var practica = await _practicaRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Practica", id);

        // El validador ya confirmo que el literal es uno de los cuatro (H31).
        var estado = Enum.Parse<EstadoPractica>(request.Estado);

        // H28: el indicador sale del contexto, no de una constante. Hoy la barrera
        // de H17 lo hace siempre verdadero, pero el dia que otro rol alcance esta
        // operacion la regla de RN-05 seguira siendo correcta sin tocar nada.
        var esAdministrador = _contexto.Alcance == AlcanceConsulta.Global;

        // P14, y hay que leerlo aqui: tanto Finalizar como CambiarEstado pisan
        // practica.Estado, de modo que capturarlo despues compararia el destino
        // contra si mismo y todos los asientos saldrian Otro, incluidos los
        // retrocesos que RN-05 reserva al Administrador y que son la razon de que
        // esta accion se audite.
        var estadoAnterior = practica.Estado;

        // H30: Finalizar es lo unico que escribe FechaFin, y siempre junto a la
        // transicion. Sin fecha en el cuerpo se conserva la prevista en el alta.
        if (estado == EstadoPractica.Finalizada && request.FechaFin is not null)
            practica.Finalizar(request.FechaFin.Value, esAdministrador);
        else
            practica.CambiarEstado(estado, esAdministrador);

        await _practicaRepo.ActualizarAsync(practica, ct);

        // RN-05. Se audita todo cambio manual y no solo el retroceso: el
        // registrador decide el rotulo, este archivo solo aporta los dos extremos.
        await _auditor.PorCambioDeEstadoDePracticaAsync(
            practica.Id, estadoAnterior, practica.Estado, ct);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Practica {PracticaId} movida a estado {Estado} por el administrador {AdministradorId}.",
            practica.Id, practica.Estado, _contexto.UsuarioId);

        return _mapeador.Map<PracticaResponse>(practica);
    }
}
