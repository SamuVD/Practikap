using Practikap.Domain.Entities;

namespace Practikap.Domain.Interfaces;

/// <summary>
/// Contrato de acceso a <see cref="Configuracion"/>. Modulo M8.
/// </summary>
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

    /// <summary>
    /// Establece el valor de una clave, creandola si no existia, y registra al
    /// Administrador responsable del cambio.
    /// </summary>
    /// <param name="clave">Clave de configuracion.</param>
    /// <param name="valor">Valor a establecer.</param>
    /// <param name="actualizadoPorId">Administrador que ejecuta el cambio.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task EstablecerAsync(string clave, string valor, int actualizadoPorId, CancellationToken ct);
}
