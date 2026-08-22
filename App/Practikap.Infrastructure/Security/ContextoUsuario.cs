using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Practikap.Application.Common;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Infrastructure.Security;

/// <summary>
/// Traduce los claims del JWT de la peticion en curso al contrato que consumen
/// los casos de uso (ADR-03).
/// </summary>
internal sealed class ContextoUsuario : IContextoUsuario
{
    private readonly IHttpContextAccessor _acceso;

    /// <summary>Crea el contexto sobre la peticion HTTP en curso.</summary>
    /// <param name="acceso">Acceso al HttpContext de la peticion.</param>
    public ContextoUsuario(IHttpContextAccessor acceso) => _acceso = acceso;

    private ClaimsPrincipal? Principal => _acceso.HttpContext?.User;

    /// <inheritdoc />
    public bool EstaAutenticado => Principal?.Identity?.IsAuthenticated == true;

    /// <inheritdoc />
    public int UsuarioId
    {
        get
        {
            var valor = Claim("sub");
            return int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0
                ? id
                : throw new AutorizacionException("El token no identifica a un usuario valido.");
        }
    }

    /// <inheritdoc />
    public string Rol => Claim("role");

    /// <inheritdoc />
    public string ReferenciaToken => Claim("jti");

    /// <inheritdoc />
    public AlcanceConsulta Alcance => Rol switch
    {
        "Administrador" => AlcanceConsulta.Global,
        "Instructor" => AlcanceConsulta.Asignado,
        "Aprendiz" => AlcanceConsulta.Propio,
        _ => throw new AutorizacionException("El rol del token no corresponde a ninguno de los tres roles del sistema.")
    };

    /// <summary>
    /// Devuelve el valor de un claim exigiendo que la peticion este autenticada.
    /// Implementa el rechazo de RN-13: un caso de uso que pregunta por la
    /// identidad sin que exista no debe recibir un valor por defecto silencioso.
    /// </summary>
    private string Claim(string tipo)
    {
        if (!EstaAutenticado)
            throw new AutorizacionException("La solicitud no esta autenticada.");

        return Principal!.FindFirst(tipo)?.Value
               ?? throw new AutorizacionException($"El token no contiene el claim '{tipo}'.");
    }
}
