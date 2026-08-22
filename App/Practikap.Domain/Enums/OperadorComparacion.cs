namespace Practikap.Domain.Enums;

/// <summary>
/// Operador relacional que una regla del Motor aplica entre el valor evaluado
/// y su valor de condicion (RN-06, RN-07).
/// Corresponde a la columna reglas.operador del Script_DDL.sql.
/// </summary>
public enum OperadorComparacion
{
    /// <summary>Estrictamente mayor. Literal en base de datos: "&gt;".</summary>
    Mayor = 1,

    /// <summary>Mayor o igual. Literal en base de datos: "&gt;=".</summary>
    MayorOIgual = 2,

    /// <summary>Estrictamente menor. Literal en base de datos: "&lt;".</summary>
    Menor = 3,

    /// <summary>Menor o igual. Literal en base de datos: "&lt;=".</summary>
    MenorOIgual = 4,

    /// <summary>Igualdad. Literal en base de datos: "=".</summary>
    Igual = 5,

    /// <summary>Desigualdad. Literal en base de datos: "!=".</summary>
    Distinto = 6
}
