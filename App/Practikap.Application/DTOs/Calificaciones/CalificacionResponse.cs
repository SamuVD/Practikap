namespace Practikap.Application.DTOs.Calificaciones;

/// <summary>
/// Representacion de salida de una calificacion, en cualquiera de las dos
/// direcciones.
/// </summary>
/// <remarks>
/// Un solo record para las dos entidades: comparten forma exacta, y el perfil de
/// AutoMapper declara un mapa desde cada una. Quien emitio la calificacion se
/// desprende de la lista en la que viaja dentro de
/// <see cref="CalificacionesDePracticaResponse"/>, o del endpoint que la
/// devolvio, no de un campo del propio registro.
///
/// Anulado y AnuladoPor salen siempre, con el mismo criterio que I4 fijo en M4:
/// un registro anulado se devuelve con su marca en lugar de desaparecer, que es
/// lo que hace verificable la inmutabilidad de RN-12 desde fuera del sistema.
///
/// AnuladoPor es el identificador desnudo, sin nombre aplanado al lado. Las dos
/// configuraciones mapean anulado_por con HasOne&lt;Usuario&gt;().WithMany() sin
/// propiedad de navegacion, de modo que ningun Usuario entra en el grafo y la
/// fuga de ContrasenaHash que H32 vigila no tiene por donde ocurrir (RNF-05).
/// </remarks>
/// <param name="Id">Identificador de la calificacion.</param>
/// <param name="PracticaId">Practica a la que pertenece.</param>
/// <param name="Valor">Valor de la calificacion, entre 0.0 y 5.0.</param>
/// <param name="Comentario">Comentario cualitativo, nulo si no se registro ninguno.</param>
/// <param name="FechaRegistro">Momento del registro, determinado por el servidor (RN-11).</param>
/// <param name="Anulado">Marca de anulacion.</param>
/// <param name="AnuladoPor">Identificador del Administrador que anulo, nulo si el registro esta vigente.</param>
public sealed record CalificacionResponse
(
    int Id,
    int PracticaId,
    decimal Valor,
    string? Comentario,
    DateTime FechaRegistro,
    bool Anulado,
    int? AnuladoPor
);
