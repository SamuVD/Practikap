namespace Practikap.Application.DTOs.Configuracion;

/// <summary>Representacion de salida de una entrada de configuracion del sistema.</summary>
/// <remarks>
/// <b>No expone Id.</b> La clave es la identidad publica de la entrada —es lo que
/// viaja en las tres rutas del controlador y lo que uq_configuracion_clave garantiza
/// unico—, y devolver ademas el autoincremental daria dos identificadores para lo
/// mismo, uno de los cuales nadie podria usar en ninguna ruta.
///
/// ActualizadoPor es un identificador desnudo, sin nombre aplanado al lado, igual
/// que ReglaResponse.CreadoPor y ReporteResumenResponse.GeneradoPor. Configuracion
/// si declara la navegacion Actualizador, pero ninguna consulta de
/// ConfiguracionRepository la carga, de modo que ningun Usuario entra en el grafo y
/// la fuga de ContrasenaHash que H32 vigila no tiene por donde ocurrir (RNF-05).
/// Quien necesite el nombre lo pide al GET de usuarios que M1 expone desde el 4.1.
///
/// Descripcion sale del catalogo cerrado de ReglasDeConfiguracion y se escribe al
/// crear la entrada (P8). No cambia con los PUT posteriores: describe lo que la
/// clave significa para el sistema, y eso no depende del valor que tenga hoy.
/// </remarks>
/// <param name="Clave">Clave unica de la entrada, una de las del catalogo de P8.</param>
/// <param name="Valor">Valor vigente.</param>
/// <param name="Descripcion">Proposito de la clave, tomado del catalogo al crearla.</param>
/// <param name="ActualizadoPor">Administrador responsable del valor actual (RF-09).</param>
/// <param name="FechaActualizacion">Fecha del ultimo cambio, determinada por el servidor (RN-11).</param>
public sealed record ConfiguracionResponse
(
    string Clave,
    string Valor,
    string? Descripcion,
    int ActualizadoPor,
    DateTime FechaActualizacion
);
