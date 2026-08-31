namespace Practikap.Application.DTOs.Reglas;

/// <summary>Representacion de salida de una regla del Motor.</summary>
/// <remarks>
/// Operador viaja como texto y no como numero (H31), y ese texto es el nombre del
/// miembro de OperadorComparacion —Mayor, MayorOIgual…—, no el literal que guarda
/// la columna ENUM. Es la unica salida del sistema donde las dos formas difieren:
/// los simbolos &gt; y &gt;= son detalle de persistencia y su dueno es
/// ConvertidoresDeEnum. El contrato de la API es el mismo nombre que acepta de
/// entrada, de modo que lo que se lee se puede reenviar sin traducir.
///
/// CreadoPor es un identificador desnudo, sin nombre aplanado al lado. Regla.Creador
/// si es navegacion, pero ninguna consulta del repositorio la carga, de modo que
/// ningun Usuario entra en el grafo y la fuga de ContrasenaHash que H32 vigila no
/// tiene por donde ocurrir (RNF-05).
///
/// No expone Umbral (N3). La columna existe y esta poblada, siempre con el mismo
/// valor que ValorCondicion, pero devolverla mostraria un dato redundante que el
/// cliente no puede cambiar y que no significa nada distinto.
/// </remarks>
/// <param name="Id">Identificador de la regla.</param>
/// <param name="Nombre">Nombre descriptivo.</param>
/// <param name="CampoEvaluado">Campo del dominio que se evalua.</param>
/// <param name="Operador">Operador relacional, como texto.</param>
/// <param name="ValorCondicion">Valor contra el que se compara.</param>
/// <param name="AccionResultante">Consecuencia de la coincidencia.</param>
/// <param name="Prioridad">Orden de evaluacion. Menor valor se evalua primero (RN-07).</param>
/// <param name="Activa">Indica si la regla participa en las evaluaciones (RN-08).</param>
/// <param name="CreadoPor">Administrador que la registro (RF-10).</param>
/// <param name="FechaCreacion">Fecha de alta, determinada por el servidor.</param>
/// <param name="FechaActualizacion">Fecha de la ultima modificacion, determinada por el servidor.</param>
public sealed record ReglaResponse
(
    int Id,
    string Nombre,
    string CampoEvaluado,
    string Operador,
    decimal ValorCondicion,
    string AccionResultante,
    int Prioridad,
    bool Activa,
    int CreadoPor,
    DateTime FechaCreacion,
    DateTime FechaActualizacion
);
