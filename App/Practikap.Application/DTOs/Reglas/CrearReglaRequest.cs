namespace Practikap.Application.DTOs.Reglas;

/// <summary>Alta de una regla del Motor de Reglas Dinamicas (RF-10, N1, N2).</summary>
/// <remarks>
/// No lleva Umbral. La columna es NOT NULL y se conserva, pero el caso de uso la
/// escribe siempre igual a ValorCondicion (N3): el DDL las separo porque el
/// catalogo conceptual las listaba como atributos distintos, y en la practica la
/// condicion es una sola. Exponer las dos obligaria al Administrador a repetir el
/// mismo numero dos veces sin que la diferencia significara nada.
///
/// No lleva Activa. Toda regla nace activa por constructor; retirarla es una
/// operacion aparte, que es el PATCH de N6.
///
/// No lleva CreadoPor. El Administrador que la registra sale del token (RF-10), no
/// del cuerpo: aceptarlo de fuera permitiria atribuir una regla a otra cuenta.
/// </remarks>
/// <param name="Nombre">Nombre descriptivo de la regla. Hasta 150 caracteres.</param>
/// <param name="CampoEvaluado">Campo del dominio que se evalua. Uno de los de la lista blanca de N1.</param>
/// <param name="Operador">Operador relacional, como texto: Mayor, MayorOIgual, Menor, MenorOIgual, Igual o Distinto.</param>
/// <param name="ValorCondicion">Valor contra el que se compara. Cabe en DECIMAL(6,2).</param>
/// <param name="AccionResultante">Consecuencia de la coincidencia. Una de las de la lista blanca de N2.</param>
/// <param name="Prioridad">Orden de evaluacion. Menor valor se evalua primero (RN-07). Cero por omision.</param>
public sealed record CrearReglaRequest
(
    string Nombre,
    string CampoEvaluado,
    string Operador,
    decimal ValorCondicion,
    string AccionResultante,
    int Prioridad
);
