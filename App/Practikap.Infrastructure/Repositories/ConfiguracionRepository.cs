using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IConfiguracionRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M8, panel de administracion.
/// </summary>
/// <remarks>
/// No invoca dominio, con el criterio de H28, I9, J7, L8, N8 y O5 (P4). El contrato
/// que dejo el paso 3.1 declaraba un EstablecerAsync que recibia clave y valor y
/// habria obligado a este archivo a cargar-o-crear la entrada y llamarle
/// Configuracion.Establecer; se elimino, y quien decide si crea o actualiza es
/// EstablecerConfiguracionUseCase.
///
/// Tampoco valida nada. Este archivo lee y registra entradas; que la clave este en
/// el catalogo cerrado de P8 y que el valor sea de los que esa clave admite lo
/// comprueba ReglasDeConfiguracion, en la Aplicacion, antes de llegar aqui.
///
/// <b>Ninguna consulta lleva Include.</b> Configuracion.Actualizador es la unica
/// propiedad de navegacion de la entidad y no la lee nadie: ConfiguracionResponse
/// expone ActualizadoPor desnudo, de modo que ningun Usuario entra en el grafo y
/// H32 no tiene por donde romperse (RNF-05). Es el mismo criterio de
/// ReglaRepository.
/// </remarks>
internal sealed class ConfiguracionRepository : IConfiguracionRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public ConfiguracionRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// Proyecta a string? en el servidor, sin materializar la entidad: es la
    /// consulta que el Motor de Reglas usara para resolver el estado por defecto
    /// (RN-06) y no necesita ni la descripcion, ni el responsable, ni la fecha.
    ///
    /// FirstOrDefaultAsync sobre una proyeccion de string devuelve null tanto si la
    /// clave no existe como si su valor fuera nulo, pero la columna es NOT NULL en
    /// el DDL y el constructor de la entidad rechaza el nulo, de modo que el unico
    /// null posible es el de la clave ausente.
    /// </remarks>
    public async Task<string?> ObtenerValorAsync(string clave, CancellationToken ct) =>
        await _contexto.Configuraciones
            .AsNoTracking()
            .Where(configuracion => configuracion.Clave == clave)
            .Select(configuracion => configuracion.Valor)
            .FirstOrDefaultAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// Orden ascendente por clave y no por identificador: el panel muestra un
    /// catalogo, no un historico, y el orden alfabetico es estable frente al orden
    /// en que se hayan ido creando las entradas.
    ///
    /// Con AsNoTracking, a diferencia de ObtenerPorClaveAsync: nada de lo que
    /// devuelve este listado se va a escribir.
    /// </remarks>
    public async Task<IReadOnlyList<Configuracion>> ListarAsync(CancellationToken ct) =>
        await _contexto.Configuraciones
            .AsNoTracking()
            .OrderBy(configuracion => configuracion.Clave)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// Se rastrea, sin AsNoTracking: es la consulta que alimenta al PUT, que aplica
    /// Configuracion.Establecer sobre la instancia devuelta y confia en que EF Core
    /// detecte el cambio. Es lo que hace innecesario un ActualizarAsync en el
    /// contrato. Mismo criterio que ReglaRepository.ObtenerPorIdAsync.
    ///
    /// La busqueda va contra clave y no contra el identificador, que es lo que hace
    /// que la clave sea la identidad publica de la entrada y lo que viaja en las
    /// tres rutas del controlador. La sostiene uq_configuracion_clave.
    /// </remarks>
    public Task<Configuracion?> ObtenerPorClaveAsync(string clave, CancellationToken ct) =>
        _contexto.Configuraciones
            .FirstOrDefaultAsync(configuracion => configuracion.Clave == clave, ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): la fila solo existe despues de que el caso de uso llame
    /// IUnidadDeTrabajo.GuardarCambiosAsync.
    ///
    /// Tampoco escribe FechaActualizacion. La columna esta mapeada como generada por
    /// la base con DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP, de modo que
    /// la marca de tiempo la pone MySQL tanto en el alta como en cada cambio de valor,
    /// y no hay ninguna linea de C# que pudiera sustituirla por la del cliente (RN-11).
    /// </remarks>
    public Task AgregarAsync(Configuracion configuracion, CancellationToken ct)
    {
        _contexto.Configuraciones.Add(configuracion);
        return Task.CompletedTask;
    }
}
