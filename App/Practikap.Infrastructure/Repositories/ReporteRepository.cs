using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IReporteRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M7.
/// </summary>
/// <remarks>
/// El repositorio no invoca dominio (O5, que extiende a M7 lo que decidieron H28,
/// I9, J7, L8 y N8): no hay un metodo que reciba identificadores de practica y
/// llame a Reporte.VincularPractica. El agregado llega compuesto desde el caso de
/// uso y aqui solo se registra.
///
/// Tampoco filtra ni decide alcance. Los nueve criterios del filtro se aplican en
/// memoria dentro del caso de uso (O4, H27) y los dos alcances de RN-13 se
/// resuelven eligiendo entre los dos listados, sobre IContextoUsuario (ADR-03).
///
/// No escribe la tabla puente. reporte_practica se persiste sola, como efecto de
/// la coleccion de navegacion que trae el reporte, dentro del mismo SaveChanges
/// que inserta la fila de reportes: es la navegacion de salto que declara
/// ReporteConfiguration.
///
/// Este es el unico repositorio del proyecto cuya consulta por identificador
/// carga Usuario en el grafo sin que eso ponga en riesgo H32. La proteccion no
/// esta en la consulta sino en la salida: ningun perfil de M7 declara un
/// CreateMap desde Usuario, y LineaDeReporteResponse solo proyecta el nombre
/// completo. Es el mismo reparto que PracticaRepository.ConGrafoCompleto y
/// PracticaMappingProfile hacen desde M3 (RNF-05).
/// </remarks>
internal sealed class ReporteRepository : IReporteRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public ReporteRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de que
    /// el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync.
    ///
    /// Un solo Add cubre las dos tablas. Las practicas que el reporte trae en su
    /// coleccion ya estan rastreadas —el caso de uso las obtuvo con
    /// IPracticaRepository.ListarPorIdsAsync—, de modo que EF Core no intenta
    /// insertarlas de nuevo y se limita a escribir las filas de reporte_practica
    /// en el mismo lote (O12).
    ///
    /// Tampoco escribe FechaGeneracion. La columna esta mapeada como generada por
    /// la base con DEFAULT CURRENT_TIMESTAMP, de modo que la marca de tiempo la
    /// pone MySQL y no hay ninguna linea de C# que pudiera sustituirla por la del
    /// cliente. Eso es RN-11.
    /// </remarks>
    public Task<int> AgregarAsync(Reporte reporte, CancellationToken ct)
    {
        _contexto.Reportes.Add(reporte);
        return Task.FromResult(reporte.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Es la unica consulta con grafo del repositorio, y lo lleva porque O14 pide
    /// recomponer el contenido del reporte con los datos actuales de cada
    /// practica: sin ficha, programa, empresa, instructor y aprendiz cargados, la
    /// consulta de un reporte devolveria identificadores en lugar de nombres, o
    /// pagaria una consulta por practica al aplanarlos.
    ///
    /// Va con AsNoTracking, a diferencia de las consultas por identificador de M3,
    /// M4 y M5: M7 no tiene ningun PATCH y un reporte no se modifica nunca despues
    /// de generado. Nada de lo que devuelve esta consulta se va a escribir.
    /// </remarks>
    public Task<Reporte?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.Reportes
            .AsNoTracking()
            .Include(reporte => reporte.Practicas).ThenInclude(practica => practica.Ficha)
                .ThenInclude(ficha => ficha.Programa)
            .Include(reporte => reporte.Practicas).ThenInclude(practica => practica.Empresa)
            .Include(reporte => reporte.Practicas).ThenInclude(practica => practica.Instructor)
            .Include(reporte => reporte.Practicas).ThenInclude(practica => practica.Aprendiz)
            .FirstOrDefaultAsync(reporte => reporte.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// Sin Include, a diferencia de ObtenerPorIdAsync: el listado devuelve solo el
    /// rastro de cada reporte y no su contenido, de modo que cargar el grafo de
    /// las practicas de cada fila seria traer el historico entero para no leerlo.
    ///
    /// El orden es descendente por identificador, que en esta tabla equivale al
    /// cronologico inverso porque la clave es autoincremental y la fecha la pone
    /// la base en el mismo INSERT. El reporte mas reciente encabeza la lista.
    /// </remarks>
    public async Task<IReadOnlyList<Reporte>> ListarTodosAsync(CancellationToken ct) =>
        await _contexto.Reportes
            .AsNoTracking()
            .OrderByDescending(reporte => reporte.Id)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// Filtra por la clave foranea y no por la navegacion Creador, que esta
    /// entidad no declara: generado_por basta y ningun Usuario entra en el grafo.
    /// </remarks>
    public async Task<IReadOnlyList<Reporte>> ListarPorGeneradorAsync(
        int generadoPorId, CancellationToken ct) =>
        await _contexto.Reportes
            .AsNoTracking()
            .Where(reporte => reporte.GeneradoPor == generadoPorId)
            .OrderByDescending(reporte => reporte.Id)
            .ToListAsync(ct);
}
