using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Registro de la generacion de un reporte (RF-08). Raiz de agregado del
/// modulo M7: guarda el rastro de que se consolido, con que filtros y quien
/// lo genero, no el contenido exportado.
/// </summary>
/// <remarks>
/// La relacion con las practicas consolidadas se materializa en la tabla
/// puente reporte_practica, que el Doc_Arquitectura 5.2 clasifica como
/// relacion pura y que por eso no tiene clase propia: se expresa como la
/// coleccion de navegacion <see cref="Practicas"/>.
/// </remarks>
public class Reporte
{
    private readonly List<Practica> _practicas = [];

    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Reporte() { }

    /// <summary>Registra la generacion de un reporte.</summary>
    /// <param name="tipo">Alcance funcional del reporte.</param>
    /// <param name="filtros">Criterios aplicados, serializados como JSON.</param>
    /// <param name="generadoPor">Instructor o Administrador que lo genera.</param>
    /// <exception cref="ReglaDeDominioException">Si los filtros o el generador son invalidos.</exception>
    public Reporte(TipoReporte tipo, string filtros, int generadoPor)
    {
        if (string.IsNullOrWhiteSpace(filtros))
            throw new ReglaDeDominioException("Los filtros del reporte son obligatorios.");
        if (generadoPor <= 0)
            throw new ReglaDeDominioException("El reporte debe tener un generador valido.");

        Tipo = tipo;
        Filtros = filtros;
        GeneradoPor = generadoPor;
    }

    /// <summary>Identificador. Columna reportes.id.</summary>
    public int Id { get; private set; }

    /// <summary>Alcance funcional. Columna reportes.tipo.</summary>
    public TipoReporte Tipo { get; private set; }

    /// <summary>
    /// Criterios aplicados, serializados como JSON. Columna reportes.filtros.
    /// La forma tipada de estos criterios es
    /// <see cref="ValueObjects.FiltroReporte"/>; la serializacion corresponde
    /// a la capa de Aplicacion.
    /// </summary>
    public string Filtros { get; private set; } = null!;

    /// <summary>Usuario que lo genero. Columna reportes.generado_por.</summary>
    public int GeneradoPor { get; private set; }

    /// <summary>Momento de la generacion. La genera MySQL con DEFAULT CURRENT_TIMESTAMP.</summary>
    public DateTime FechaGeneracion { get; private set; }

    /// <summary>
    /// Practicas consolidadas en el reporte. Se persiste en la tabla puente
    /// reporte_practica mediante una navegacion de salto configurada en la
    /// capa de Infraestructura.
    /// </summary>
    public IReadOnlyCollection<Practica> Practicas => _practicas;

    /// <summary>Vincula una practica al reporte, sin duplicarla si ya estaba.</summary>
    /// <param name="practica">Practica consolidada.</param>
    /// <exception cref="ReglaDeDominioException">Si la practica es nula.</exception>
    public void VincularPractica(Practica practica)
    {
        if (practica is null)
            throw new ReglaDeDominioException("La practica a consolidar es obligatoria.");

        if (!_practicas.Contains(practica))
            _practicas.Add(practica);
    }

    /// <summary>
    /// Verifica que un reporte Individual consolide exactamente una practica y
    /// que uno Grupal consolide al menos una.
    /// </summary>
    /// <returns>true si la composicion es coherente con el tipo declarado.</returns>
    public bool ComposicionEsCoherente() => Tipo switch
    {
        TipoReporte.Individual => _practicas.Count == 1,
        TipoReporte.Grupal => _practicas.Count >= 1,
        _ => false
    };
}
