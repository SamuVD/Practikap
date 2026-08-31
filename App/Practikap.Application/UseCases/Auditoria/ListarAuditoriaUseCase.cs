using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Auditoria;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Auditoria;

/// <summary>
/// Consulta la bitacora de acciones sensibles del sistema. Reservado al
/// Administrador (RF-09, CU-08, RN-01, RN-05, RN-08, RN-12).
/// </summary>
/// <remarks>
/// Los cinco criterios de P6 son opcionales y se combinan con Y logico. Este archivo
/// solo los <b>traduce</b>: los dos literales de enumerado a sus miembros y el rango
/// a su comprobacion de coherencia. <b>El WHERE lo arma el repositorio y viaja al
/// servidor</b>, a diferencia de los nueve filtros de M7, que O4 resolvio en memoria.
/// La diferencia esta razonada en IAuditoriaRepository: alli habia un listado de
/// alcance previo del que colgarse; aqui no, y la tabla crece con cada accion
/// sensible del sistema.
///
/// <b>No llama a ValidateAndThrowAsync</b>, que es una desviacion explicita de la
/// primera de las cuatro lineas del Doc_Tecnico 5.2. No hay DTO de entrada que
/// validar: los cinco criterios llegan como escalares [FromQuery] y FluentValidation
/// valida objetos. Es el mismo caso de ListarPracticasUseCase, que tampoco tiene
/// validador y comprueba su parametro estado aqui dentro.
///
/// Los dos codigos se reparten con el criterio de N1, N2 y O19, y la consecuencia de
/// no tener validador es que el 400 se lanza a mano:
///
/// - <b>400</b> para el rango invertido. Es forma: que hasta preceda a desde se ve
///   mirando la peticion y ninguna consulta lo volveria valido, exactamente como
///   GenerarReporteRequestValidator trata el suyo. Se emite como ValidationException,
///   que el middleware global traduce a 400 con el mismo contrato de detalles que
///   produciria un validador.
/// - <b>422</b> para un literal desconocido de EntidadAuditada o de AccionAuditoria.
///   Un literal que el sistema no reconoce no es una peticion mal formada sino una
///   que no se puede procesar, con las mismas palabras con que ListarPracticasUseCase
///   trata su parametro estado.
///
/// <b>Un criterio que no encuentra nada devuelve 200 con lista vacia</b>, no 404: un
/// filtro sin resultados es una respuesta, no un error del solicitante (O8).
///
/// Sin alcance por rol: todo M8 es del Administrador (P3), con un unico alcance vivo,
/// Global. La bitacora no se recorta por RN-13, y no podria: su razon de ser es que
/// alguien vea lo que los demas hicieron.
///
/// <b>Hoy devuelve vacio siempre, y es lo esperado.</b> Nada de esta ronda escribe en
/// auditoria: el registrador y los once puntos de enganche son la Ronda 2.
/// </remarks>
public sealed class ListarAuditoriaUseCase
{
    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="auditoriaRepo">Acceso a la bitacora.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarAuditoriaUseCase(
        IAuditoriaRepository auditoriaRepo, IContextoUsuario contexto, IMapper mapeador)
    {
        _auditoriaRepo = auditoriaRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve los asientos que satisfacen los cinco criterios.</summary>
    /// <param name="entidadAfectada">Entidad por la que filtrar, como texto. Null no filtra.</param>
    /// <param name="accion">Tipo de accion por el que filtrar, como texto. Null no filtra.</param>
    /// <param name="usuarioId">Actor por el que filtrar. Null no filtra.</param>
    /// <param name="desde">Limite inferior del rango, inclusive. Null no acota.</param>
    /// <param name="hasta">Limite superior del rango, inclusive. Null no acota.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Los asientos, del mas reciente al mas antiguo.</returns>
    /// <exception cref="ValidationException">Si el rango de fechas esta invertido (400).</exception>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la entidad afectada o la accion traen un literal desconocido (422).
    /// </exception>
    public async Task<IReadOnlyList<RegistroAuditoriaResponse>> ExecuteAsync(
        string? entidadAfectada,
        string? accion,
        int? usuarioId,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken ct)
    {
        ExigirRangoCoherente(desde, hasta);

        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede consultar la bitacora de auditoria.");

        var entidadFiltro = TraducirEntidad(entidadAfectada);
        var accionFiltro = TraducirAccion(accion);

        var asientos = await _auditoriaRepo.ListarAsync(
            entidadFiltro, accionFiltro, usuarioId, desde, hasta, ct);

        return _mapeador.Map<IReadOnlyList<RegistroAuditoriaResponse>>(asientos);
    }

    /// <summary>
    /// Rechaza el rango invertido con 400. Se emite como ValidationException y no
    /// como ReglaDeDominioException porque el codigo que O19 fija para la forma es
    /// 400, y el middleware traduce toda ReglaDeDominioException a 422.
    /// </summary>
    private static void ExigirRangoCoherente(DateTime? desde, DateTime? hasta)
    {
        if (desde is null || hasta is null || hasta >= desde)
            return;

        throw new ValidationException(
        [
            new ValidationFailure(
                nameof(hasta),
                "La fecha hasta no puede ser anterior a la fecha desde.")
        ]);
    }

    /// <summary>
    /// Traduce el literal de entidad afectada a su miembro. Se compara contra los
    /// nombres del enumerado y no con Enum.TryParse, que tambien aceptaria la
    /// representacion numerica (H31).
    /// </summary>
    private static EntidadAuditada? TraducirEntidad(string? entidadAfectada)
    {
        if (string.IsNullOrWhiteSpace(entidadAfectada))
            return null;

        if (!Enum.GetNames<EntidadAuditada>().Contains(entidadAfectada, StringComparer.Ordinal))
            throw new ReglaDeDominioException(
                $"La entidad afectada debe ser una de estas: "
                + $"{string.Join(", ", Enum.GetNames<EntidadAuditada>())}.",
                "RF-09");

        return Enum.Parse<EntidadAuditada>(entidadAfectada);
    }

    /// <summary>Traduce el literal de accion a su miembro, con el mismo criterio.</summary>
    private static AccionAuditoria? TraducirAccion(string? accion)
    {
        if (string.IsNullOrWhiteSpace(accion))
            return null;

        if (!Enum.GetNames<AccionAuditoria>().Contains(accion, StringComparer.Ordinal))
            throw new ReglaDeDominioException(
                $"La accion debe ser una de estas: "
                + $"{string.Join(", ", Enum.GetNames<AccionAuditoria>())}.",
                "RF-09");

        return Enum.Parse<AccionAuditoria>(accion);
    }
}
