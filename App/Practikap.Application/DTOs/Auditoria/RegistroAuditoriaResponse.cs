namespace Practikap.Application.DTOs.Auditoria;

/// <summary>Representacion de salida de un asiento de la bitacora de auditoria.</summary>
/// <remarks>
/// EntidadAfectada y Accion viajan como <b>texto</b> y no como numero (H31), y ese
/// texto es el nombre del miembro del enumerado —Practicas, RetrocesoEstado—, no el
/// literal que guarda la columna —"practicas", "Retroceso_estado"—. Es el mismo
/// reparto que ReglaResponse hace entre Mayor y &gt;: los literales del DDL son
/// detalle de persistencia y su dueno es ConvertidoresDeEnum. El contrato de la API
/// es el nombre del miembro, que es tambien el que los dos filtros del GET aceptan
/// de entrada: lo que se lee se puede reenviar sin traducir.
///
/// UsuarioId es un identificador desnudo, y aqui no es una eleccion sino la
/// consecuencia de ADR-06: RegistroAuditoria no declara <b>ninguna</b> propiedad de
/// navegacion, ni hacia el actor ni hacia el objeto afectado. H32 queda garantizada
/// por la forma de la entidad y no por la disciplina de la consulta.
///
/// El par EntidadAfectada y EntidadId es la referencia polimorfica de ADR-06: no
/// tiene clave foranea fisica porque apunta a ocho tablas distintas. Quien quiera
/// seguirla compone la ruta del recurso con las dos mitades.
/// </remarks>
/// <param name="Id">Identificador del asiento.</param>
/// <param name="UsuarioId">Actor que ejecuto la accion.</param>
/// <param name="EntidadAfectada">Entidad sobre la que se actuo, como texto.</param>
/// <param name="EntidadId">Identificador del registro afectado.</param>
/// <param name="Accion">Tipo de accion ejecutada, como texto.</param>
/// <param name="Detalle">Descripcion complementaria, si el asiento la trae.</param>
/// <param name="FechaRegistro">Momento del asiento, determinado por el servidor (RN-11).</param>
public sealed record RegistroAuditoriaResponse
(
    int Id,
    int UsuarioId,
    string EntidadAfectada,
    int EntidadId,
    string Accion,
    string? Detalle,
    DateTime FechaRegistro
);
