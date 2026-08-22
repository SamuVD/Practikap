namespace Practikap.Application.Common;

/// <summary>
/// Emisor de los JWT de Practikap. El contrato vive en Aplicacion y su
/// implementacion en Infraestructura, de modo que el caso de uso de inicio de
/// sesion (Fase 4.1) no conozca el algoritmo de firma ni la clave.
/// </summary>
public interface IGeneradorDeToken
{
    /// <summary>
    /// Emite un token firmado con los claims que fija el Doc_Tecnico 3.2:
    /// <c>sub</c>, <c>role</c>, <c>exp</c> y <c>jti</c>.
    /// </summary>
    /// <param name="usuarioId">Identificador del usuario autenticado.</param>
    /// <param name="correo">Correo del usuario, emitido como claim informativo.</param>
    /// <param name="rol">Nombre del rol: Administrador, Instructor o Aprendiz.</param>
    /// <returns>El token, su referencia y el momento de expiracion.</returns>
    TokenEmitido Generar(int usuarioId, string correo, string rol);
}
