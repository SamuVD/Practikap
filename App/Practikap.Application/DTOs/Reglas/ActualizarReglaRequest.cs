namespace Practikap.Application.DTOs.Reglas;

/// <summary>
/// Edicion completa de una regla del Motor. Es el cuerpo del unico PUT que expone
/// Practikap (RF-10, RN-08, N7).
/// </summary>
/// <remarks>
/// Registro propio y no compartido con <see cref="CrearReglaRequest"/>, aunque hoy
/// declaren los mismos seis campos: son dos esquemas distintos en Swagger y dos
/// contratos que pueden divergir, con el mismo reparto que
/// CrearPracticaRequest y ActualizarPracticaRequest.
///
/// Reemplaza la definicion entera de la regla, pero <b>no su activacion</b>:
/// Regla.Actualizar no toca Activa ni CreadoPor, de modo que editar una regla
/// retirada la deja retirada. Cambiar eso es el PATCH de N6, no este verbo.
///
/// Tampoco lleva Umbral, por la misma razon que el DTO de alta (N3).
/// </remarks>
/// <param name="Nombre">Nombre descriptivo de la regla. Hasta 150 caracteres.</param>
/// <param name="CampoEvaluado">Campo del dominio que se evalua. Uno de los de la lista blanca de N1.</param>
/// <param name="Operador">Operador relacional, como texto: Mayor, MayorOIgual, Menor, MenorOIgual, Igual o Distinto.</param>
/// <param name="ValorCondicion">Valor contra el que se compara. Cabe en DECIMAL(6,2).</param>
/// <param name="AccionResultante">Consecuencia de la coincidencia. Una de las de la lista blanca de N2.</param>
/// <param name="Prioridad">Orden de evaluacion. Menor valor se evalua primero (RN-07).</param>
public sealed record ActualizarReglaRequest
(
    string Nombre,
    string CampoEvaluado,
    string Operador,
    decimal ValorCondicion,
    string AccionResultante,
    int Prioridad
);
