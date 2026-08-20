using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.ValueObjects;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Reporte"/>. Modulo M7.
/// </summary>
/// <remarks>
/// La tabla puente reporte_practica no tiene repositorio propio: se gestiona
/// desde aqui, tal como fija el Doc_Arquitectura 6.
/// </remarks>
public interface IReporteRepository
{
    /// <summary>
    /// Persiste el reporte y sus vinculos con las practicas consolidadas dentro
    /// de la misma operacion atomica, segun la transaccion explicita que
    /// describe ADR-02.
    /// </summary>
    /// <param name="reporte">Reporte a persistir.</param>
    /// <param name="practicaIds">Identificadores de las practicas consolidadas.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Identificador asignado al reporte.</returns>
    Task<int> RegistrarAsync(Reporte reporte, IEnumerable<int> practicaIds, CancellationToken ct);

    /// <summary>Obtiene un reporte por su identificador.</summary>
    /// <param name="id">Identificador del reporte.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El reporte, o null si no existe.</returns>
    Task<Reporte?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Selecciona las practicas que satisfacen un filtro, limitadas al alcance
    /// que el rol del solicitante permite. El alcance es un parametro explicito
    /// para que RN-13 no pueda incumplirse por omision.
    /// </summary>
    /// <param name="filtro">Criterios de seleccion de practicas.</param>
    /// <param name="alcance">Amplitud de datos autorizada al solicitante (RN-13).</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las practicas consolidables.</returns>
    Task<IReadOnlyList<Practica>> ConsolidarAsync(FiltroReporte filtro, AlcanceConsulta alcance, CancellationToken ct);
}
