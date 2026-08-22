using Practikap.Domain.Enums;

namespace Practikap.Domain.ValueObjects;

/// <summary>
/// Criterios de seleccion de practicas para la generacion de un reporte (RF-08).
/// Es un objeto de valor inmutable: no se persiste como tabla propia sino que
/// se serializa a JSON en la columna reportes.filtros del Script_DDL.sql.
/// </summary>
/// <remarks>
/// Todos los criterios son opcionales y se combinan con Y logico: un filtro sin
/// ningun criterio establecido selecciona todas las practicas que el
/// <see cref="AlcanceConsulta"/> del solicitante permita ver (RN-13).
/// La serializacion y deserializacion son responsabilidad de la capa de
/// Aplicacion; el Dominio solo define la forma del criterio.
/// </remarks>
public sealed record FiltroReporte
{
    /// <summary>Instructor responsable de las practicas buscadas.</summary>
    public int? InstructorId { get; init; }

    /// <summary>Aprendiz titular de las practicas buscadas.</summary>
    public int? AprendizId { get; init; }

    /// <summary>Ficha de formacion a la que pertenecen las practicas.</summary>
    public int? FichaId { get; init; }

    /// <summary>Programa de formacion, resuelto a traves de la ficha.</summary>
    public int? ProgramaId { get; init; }

    /// <summary>Empresa donde se desarrolla la practica.</summary>
    public int? EmpresaId { get; init; }

    /// <summary>Estado exacto de la practica.</summary>
    public EstadoPractica? Estado { get; init; }

    /// <summary>Modalidad exacta de la practica.</summary>
    public ModalidadPractica? Modalidad { get; init; }

    /// <summary>Limite inferior del rango de fecha de inicio, inclusive.</summary>
    public DateOnly? Desde { get; init; }

    /// <summary>Limite superior del rango de fecha de inicio, inclusive.</summary>
    public DateOnly? Hasta { get; init; }

    /// <summary>
    /// Indica si el rango de fechas es coherente, es decir si no hay limite
    /// superior anterior al inferior.
    /// </summary>
    /// <returns>true si el rango es utilizable; false en caso contrario.</returns>
    public bool RangoEsValido() => Desde is null || Hasta is null || Hasta >= Desde;

    /// <summary>Indica si el filtro no impone ningun criterio.</summary>
    /// <returns>true si todos los criterios estan sin establecer.</returns>
    public bool EstaVacio() =>
        InstructorId is null && AprendizId is null && FichaId is null &&
        ProgramaId is null && EmpresaId is null && Estado is null &&
        Modalidad is null && Desde is null && Hasta is null;
}