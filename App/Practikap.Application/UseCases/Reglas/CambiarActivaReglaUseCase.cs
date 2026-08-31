using AutoMapper;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Reglas;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Reglas;

/// <summary>
/// Incorpora una regla a las evaluaciones del Motor o la retira de ellas.
/// Reservado al Administrador (RF-10, CU-02, RN-08).
/// </summary>
/// <remarks>
/// Es la otra mitad de RN-08, y la mas literal: <b>una regla entra o sale de
/// operacion sin modificar codigo ni desplegar de nuevo</b>. El efecto es inmediato
/// para toda evaluacion posterior, porque el Motor pide sus reglas en cada
/// invocacion y no las guarda en memoria.
///
/// Es tambien la baja del sistema. Las reglas nunca se eliminan: no hay DELETE
/// (decision F3), y fk_notificaciones_regla es ON DELETE RESTRICT, de modo que una
/// regla que ya disparo alertas no podria borrarse aunque el endpoint existiera.
/// Desactivarla la retira conservando la traza de lo que origino (RN-09).
///
/// Es idempotente. Desactivar dos veces la misma regla devuelve 200 con la misma
/// marca, y no el 422 con el que responden las anulaciones de M4 y M5: aquellas son
/// irreversibles y atribuyen la anulacion a un Administrador, de modo que repetirlas
/// es un error que vale la pena rechazar. Esta es reversible y es la misma intencion
/// repetida. Es el criterio de K9 y L4 aplicado a M2, y descansa en que Activar y
/// Desactivar vienen sin guarda desde el paso 3.1 y asi se quedan.
///
/// Es el unico caso de uso del modulo que no inyecta IValidator&lt;TRequest&gt;. Es
/// una desviacion intencional del Doc_Tecnico 5.2, no un olvido, con el precedente
/// de CambiarEstadoUsuarioUseCase: CambiarActivaReglaRequest declara un unico bool
/// obligatorio y el enlace de modelo de ASP.NET Core ya rechaza cualquier cuerpo que
/// no lo traiga. Un validador aqui no tendria ninguna regla que declarar.
/// </remarks>
public sealed class CambiarActivaReglaUseCase
{
    private readonly IReglaRepository _reglaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IRegistradorDeAuditoria _auditor;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<CambiarActivaReglaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="reglaRepo">Acceso a las reglas del Motor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="auditor">Bitacora de acciones sensibles (P12, P13).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CambiarActivaReglaUseCase(
        IReglaRepository reglaRepo,
        IContextoUsuario contexto,
        IRegistradorDeAuditoria auditor,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<CambiarActivaReglaUseCase> registro)
    {
        _reglaRepo = reglaRepo;
        _contexto = contexto;
        _auditor = auditor;
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica el estado de activacion indicado.</summary>
    /// <param name="id">Regla afectada.</param>
    /// <param name="request">Estado de activacion destino.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La regla con su activacion ya aplicada.</returns>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="NoEncontradoException">Si la regla no existe (404).</exception>
    public async Task<ReglaResponse> ExecuteAsync(
        int id, CambiarActivaReglaRequest request, CancellationToken ct)
    {
        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede configurar el Motor de Reglas.");

        var regla = await _reglaRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Regla", id);

        if (request.Activa)
            regla.Activar();
        else
            regla.Desactivar();

        await _reglaRepo.ActualizarAsync(regla, ct);

        // RN-08. El asiento se escribe tambien cuando la llamada es idempotente y
        // no cambia nada: lo que la bitacora registra es que alguien ejecuto la
        // accion, no que el valor se haya movido. Es lo contrario del criterio de
        // la reasignacion, y la diferencia esta en que alli la operacion abarca
        // mas cosas y el asiento habla solo de una de ellas (P13).
        await _auditor.PorConfiguracionDeReglaAsync(
            regla.Id, regla.Nombre, regla.Activa ? "Activacion" : "Desactivacion", ct);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Regla {ReglaId} {Estado} por el administrador {AdministradorId}.",
            regla.Id, regla.Activa ? "activada" : "desactivada", _contexto.UsuarioId);

        return _mapeador.Map<ReglaResponse>(regla);
    }
}
