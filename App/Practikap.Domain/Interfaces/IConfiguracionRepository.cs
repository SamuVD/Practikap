using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Configuracion"/>. Modulo M8.
/// </summary>
/// <remarks>
/// El contrato no ofrece metodo de eliminacion. Una entrada de configuracion no se
/// borra: se le establece otro valor, y el catalogo cerrado de claves que fija P8
/// hace que no haya entradas huerfanas que limpiar. Es la decision F3 aplicada al
/// almacen clave/valor.
///
/// Este contrato tenia un metodo que P4 retiro, y no por gusto:
///
/// EstablecerAsync(clave, valor, actualizadoPorId, ct) recibia clave y valor, de
/// modo que para cumplirlo el repositorio tenia que cargar-o-crear la entrada e
/// invocar <see cref="Configuracion.Establecer"/>. Eso es dominio invocado desde la
/// Infraestructura, que es justo lo que H28 descarto en M3, I9 aplico en M4, J7
/// extendio a M5, L8 a M6, N8 a M2 y O5 a M7. Lo reemplazan
/// <see cref="ObtenerPorClaveAsync"/>, que devuelve la entrada rastreada, y
/// <see cref="AgregarAsync"/>, que recibe la entidad ya construida: quien decide si
/// crea o actualiza, y quien llama a Establecer, es el caso de uso, que es donde
/// vive el dominio.
///
/// <b>No hay un ActualizarAsync</b>, a diferencia de IReglaRepository y de
/// INotificacionRepository. Aquellos lo tienen porque sus entidades pueden llegar
/// desatadas desde mas de un camino; aqui hay uno solo, y la entrada que devuelve
/// ObtenerPorClaveAsync viene rastreada, de modo que EF Core detecta el cambio sin
/// llamada adicional.
///
/// <see cref="ObtenerValorAsync"/> se conserva tal cual (P5): es una proyeccion a
/// string?, no invoca dominio y no tiene nada que retirar. Lo que le faltaba era la
/// puerta de entrada por la que alguien escribiera el valor que devuelve, y eso es
/// lo que agrega este paso.
/// </remarks>
public interface IConfiguracionRepository
{
    /// <summary>
    /// Obtiene el valor asociado a una clave. Es la consulta con la que el
    /// Motor resuelve el estado por defecto que RN-06 exige aplicar cuando
    /// ninguna regla activa coincide.
    /// </summary>
    /// <param name="clave">Clave de configuracion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El valor, o null si la clave no esta configurada.</returns>
    Task<string?> ObtenerValorAsync(string clave, CancellationToken ct);

    /// <summary>Lista todas las entradas de configuracion del sistema.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Coleccion de solo lectura con las entradas de configuracion.</returns>
    Task<IReadOnlyList<Configuracion>> ListarAsync(CancellationToken ct);

    /// <summary>Obtiene la entrada completa de una clave.</summary>
    /// <param name="clave">Clave de configuracion.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La entrada, o null si la clave no esta configurada.</returns>
    /// <remarks>
    /// Devuelve la entidad <b>rastreada</b>, sin AsNoTracking: es la consulta que
    /// alimenta al PUT, que le aplica Establecer sobre la instancia devuelta y
    /// confia en que EF Core detecte el cambio. Es lo que hace innecesario un
    /// ActualizarAsync.
    /// </remarks>
    Task<Configuracion?> ObtenerPorClaveAsync(string clave, CancellationToken ct);

    /// <summary>Registra una entrada de configuracion ya construida.</summary>
    /// <param name="configuracion">Entrada a persistir, con su clave, valor, descripcion y responsable.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task AgregarAsync(Configuracion configuracion, CancellationToken ct);
}
