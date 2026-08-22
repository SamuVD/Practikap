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
/// No expone control transaccional explicito. Todas las operaciones del modulo
/// M1 escriben sobre una sola tabla, y el DbContext ya envuelve cada
/// SaveChanges en su propia transaccion. El contrato se ampliara cuando
/// aparezca la primera operacion multitabla real —el registro de un reporte y
/// sus vinculos en reporte_practica, en la Fase 4.8—, no antes.
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