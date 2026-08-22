using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Application.Common;

/// <summary>
/// Identidad del solicitante autenticado, expuesta a los casos de uso.
/// Materializa ADR-03: el aislamiento de datos por rol (RN-13) se resuelve en la
/// capa de Aplicacion, no en el controlador ni en el repositorio.
/// </summary>
/// <remarks>
/// El contrato vive aqui y su implementacion en Practikap.Infrastructure/Security,
/// que lo construye a partir de los claims del JWT. Un caso de uso que necesite
/// saber quien pregunta depende de esta interfaz y de nada mas: eso lo hace
/// verificable con dobles en memoria, sin servidor web (RNF-08).
/// </remarks>
public interface IContextoUsuario
{
    /// <summary>Indica si la solicitud en curso llego con un token valido.</summary>
    bool EstaAutenticado { get; }

    /// <summary>
    /// Identificador del usuario, tomado del claim <c>sub</c> (Doc_Tecnico 3.2).
    /// </summary>
    /// <exception cref="AutorizacionException">
    /// Si la solicitud no esta autenticada o el claim no es un entero valido.
    /// </exception>
    int UsuarioId { get; }

    /// <summary>
    /// Nombre del rol, tomado del claim <c>role</c>. Es el valor sobre el que
    /// se resuelve la autorizacion por rol que exige RN-01.
    /// </summary>
    /// <exception cref="AutorizacionException">Si la solicitud no esta autenticada.</exception>
    string Rol { get; }

    /// <summary>
    /// Claim <c>jti</c> del token en curso. Es el valor que se persiste en
    /// tokens_revocados al cerrar sesion (RN-03).
    /// </summary>
    /// <exception cref="AutorizacionException">Si la solicitud no esta autenticada.</exception>
    string ReferenciaToken { get; }

    /// <summary>
    /// Amplitud de datos que el rol autenticado puede consultar (RN-13). El caso
    /// de uso la traduce al metodo de repositorio correspondiente.
    /// </summary>
    /// <exception cref="AutorizacionException">Si el rol no es uno de los tres conocidos.</exception>
    AlcanceConsulta Alcance { get; }
}
