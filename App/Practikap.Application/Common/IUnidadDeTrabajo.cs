namespace Practikap.Application.Common;

/// <summary>
/// Punto unico de confirmacion de los cambios registrados durante una peticion
/// (ADR-02). Los repositorios registran cambios en el contexto; el caso de uso
/// decide cuando confirmarlos, y lo hace a traves de este contrato.
/// </summary>
/// <remarks>
/// El contrato vive en Aplicacion y su implementacion en Infraestructura, de
/// modo que un caso de uso pueda confirmar sin conocer EF Core. Eso es lo que
/// permite sustituirlo por un doble en memoria en Practikap.Application.Tests
/// (Fase 6.1), que es el criterio de aceptacion de RNF-08.
///
/// No expone control transaccional explicito, y a estas alturas ya no va a
/// exponerlo. Hasta el paso 4.4 todas las operaciones escribian sobre una sola
/// tabla, y este comentario anunciaba que el contrato se ampliaria al llegar la
/// primera operacion multitabla real: el registro de un reporte y sus vinculos
/// en reporte_practica, en la Fase 4.8.
///
/// La Fase 4.8 llego y la ampliacion no hizo falta. Un unico SaveChanges cubre
/// el INSERT en reportes y los de reporte_practica, porque los vinculos son la
/// coleccion de navegacion del propio agregado, y el DbContext ya envuelve ese
/// SaveChanges en su transaccion (O12). El precedente lo habia sentado antes
/// N11, en el paso 4.7: la calificacion, el cambio de estado de la practica y
/// las dos notificaciones que dispara tambien son cuatro escrituras sobre tres
/// tablas confirmadas de una sola vez.
///
/// Lo que ese anuncio confundia era multitabla con multiconfirmacion. El
/// control transaccional explicito solo haria falta si un caso de uso tuviera
/// que confirmar dos veces y mantener la atomicidad entre las dos
/// confirmaciones, y ningun modulo del backend lo necesita: el punto unico de
/// confirmacion de ADR-02 es precisamente lo que lo evita.
/// </remarks>
public interface IUnidadDeTrabajo
{
    /// <summary>
    /// Confirma en la base de datos los cambios registrados en la peticion.
    /// </summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Numero de filas afectadas.</returns>
    Task<int> GuardarCambiosAsync(CancellationToken ct);
}