using Practikap.Application.DTOs.Reportes;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;

namespace Practikap.Application.UseCases.Reportes;

/// <summary>
/// Compone el contenido de un reporte a partir de las practicas consolidadas y
/// de los promedios de sus dos direcciones (O9).
/// </summary>
/// <remarks>
/// Es una clase estatica y no un servicio: no tiene estado ni dependencias
/// propias, asi que no entra en el contenedor y no contradice ADR-05, que enumera
/// casos de uso. Mismo criterio que ParticipantesDePractica.
///
/// Existe para que el contenido no se calcule de dos maneras. La generacion y la
/// consulta llegan por caminos distintos —una acaba de filtrar las practicas, la
/// otra las lee de la tabla puente— y las dos terminan aqui, de modo que el
/// reporte que se devuelve al generarlo y el que se lee un mes despues se
/// componen con el mismo codigo. Separarlos habria dejado dos definiciones de
/// "contenido de un reporte" que despues podrian divergir.
///
/// No consulta nada ni conoce repositorios: recibe las practicas con su grafo ya
/// cargado y los dos diccionarios ya resueltos. Es lo que permite que la rama
/// vacia de O8 lo invoque sin haber tocado la base.
/// </remarks>
internal static class ArmadorDeReporte
{
    /// <summary>Compone las lineas y los totales del contenido de un reporte.</summary>
    /// <param name="practicas">Practicas consolidadas, con ficha, programa, empresa, instructor y aprendiz cargados.</param>
    /// <param name="promediosInstructor">Promedios de la direccion Instructor hacia Aprendiz, por practica.</param>
    /// <param name="promediosAprendiz">Promedios de la direccion Aprendiz hacia Instructor, por practica.</param>
    /// <returns>Las lineas, ascendentes por identificador de practica, y su bloque de totales.</returns>
    /// <remarks>
    /// El orden es ascendente por identificador, a diferencia de los listados de
    /// PracticaRepository, que son descendentes. Un listado muestra primero lo
    /// ultimo que paso; un reporte se lee de arriba abajo, y el CSV que la Ronda 2
    /// exporta tiene que salir en el mismo orden que este JSON.
    ///
    /// Una practica ausente de un diccionario vale cero: es lo que documenta O10 y
    /// significa que esa direccion no tiene ninguna calificacion computable.
    /// </remarks>
    public static (IReadOnlyList<LineaDeReporteResponse> Lineas, TotalesDeReporteResponse Totales)
        Componer(
            IEnumerable<Practica> practicas,
            IReadOnlyDictionary<int, decimal> promediosInstructor,
            IReadOnlyDictionary<int, decimal> promediosAprendiz)
    {
        var ordenadas = practicas.OrderBy(practica => practica.Id).ToList();

        var lineas = ordenadas
            .Select(practica => new LineaDeReporteResponse(
                practica.Id,
                practica.Aprendiz.NombreCompleto,
                practica.Instructor.NombreCompleto,
                practica.Ficha.NumeroFicha,
                practica.Ficha.Programa.Nombre,
                // Empresa es nula en ProyectoProductivo y Monitoria (H22, H25).
                practica.Empresa?.RazonSocial,
                practica.Modalidad.ToString(),
                practica.Estado.ToString(),
                practica.FechaInicio,
                practica.FechaFin,
                PromedioDe(promediosInstructor, practica.Id),
                PromedioDe(promediosAprendiz, practica.Id)))
            .ToList();

        return (lineas, ComponerTotales(ordenadas, promediosInstructor));
    }

    /// <summary>
    /// Resuelve el promedio de una practica, o cero si la direccion no tiene
    /// ninguna calificacion computable y por eso no figura en el diccionario.
    /// </summary>
    private static decimal PromedioDe(IReadOnlyDictionary<int, decimal> promedios, int practicaId) =>
        promedios.TryGetValue(practicaId, out var promedio) ? promedio : decimal.Zero;

    /// <summary>
    /// Agrega el conteo, la distribucion por estado y el promedio general de un
    /// conjunto de practicas ya ordenado.
    /// </summary>
    /// <remarks>
    /// La distribucion se siembra con los cuatro estados en cero antes de contar,
    /// de modo que las cuatro claves salen siempre aunque ninguna practica ocupe
    /// alguno de ellos.
    ///
    /// El promedio general solo divide entre las practicas que tienen promedio del
    /// instructor. Contar las que no lo tienen como cero hundiria el numero de un
    /// grupo recien iniciado hasta describir algo que no ocurrio.
    /// </remarks>
    private static TotalesDeReporteResponse ComponerTotales(
        IReadOnlyList<Practica> practicas,
        IReadOnlyDictionary<int, decimal> promediosInstructor)
    {
        var distribucion = Enum.GetNames<EstadoPractica>()
            .ToDictionary(estado => estado, _ => 0, StringComparer.Ordinal);

        foreach (var practica in practicas)
            distribucion[practica.Estado.ToString()]++;

        var computables = practicas
            .Where(practica => promediosInstructor.ContainsKey(practica.Id))
            .Select(practica => promediosInstructor[practica.Id])
            .ToList();

        var promedioGeneral = computables.Count == 0
            ? decimal.Zero
            : Math.Round(computables.Average(), 2, MidpointRounding.AwayFromZero);

        return new TotalesDeReporteResponse(practicas.Count, distribucion, promedioGeneral);
    }
}
