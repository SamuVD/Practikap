namespace Practikap.Application.DTOs.Configuracion;

/// <summary>Cuerpo con el que se establece el valor de una clave de configuracion.</summary>
/// <remarks>
/// Un solo campo. <b>La clave no viaja en el cuerpo</b>: va en la ruta, porque es el
/// identificador del recurso sobre el que se hace el PUT. Aceptarla tambien aqui
/// abriria la posibilidad de que las dos no coincidieran, y habria que decidir cual
/// gana.
///
/// Tampoco viaja la descripcion, que sale del catalogo cerrado de
/// ReglasDeConfiguracion y se persiste al crear la entrada (P8): describe lo que la
/// clave significa para el sistema, no lo que el administrador de turno opine de
/// ella. Ni el responsable, que sale del token (RF-09), ni la fecha, que la
/// determina el servidor (RN-11).
/// </remarks>
/// <param name="Valor">Valor a establecer. Lo que se admite depende de la clave (P8).</param>
public sealed record EstablecerConfiguracionRequest
(
    string Valor
);
