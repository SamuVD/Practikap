using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Autenticacion;

/// <summary>
/// Invalida el token de la sesion en curso registrandolo como revocado (RN-03).
/// Subflujo de cierre de sesion de CU-01.
/// </summary>
/// <remarks>
/// No recibe DTO de entrada: el token a revocar es el de la peticion, que llega
/// por IContextoUsuario. Aceptar un identificador del cliente permitiria que un
/// usuario cerrara la sesion de otro.
///
/// Es idempotente sin logica adicional: un segundo intento con el mismo token lo
/// rechaza el pipeline en OnTokenValidated con 401, sin llegar hasta aqui.
/// </remarks>
public sealed class CerrarSesionUseCase
{
    private readonly ITokenRevocadoRepository _tokenRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly ILogger<CerrarSesionUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="tokenRepo">Acceso a la lista de revocacion.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="registro">Registro de eventos.</param>
    public CerrarSesionUseCase(
        ITokenRevocadoRepository tokenRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        ILogger<CerrarSesionUseCase> registro)
    {
        _tokenRepo = tokenRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _registro = registro;
    }

    /// <summary>Revoca el token de la solicitud en curso.</summary>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var revocacion = new TokenRevocado(
            _contexto.UsuarioId,
            _contexto.ReferenciaToken,
            MotivoRevocacion.Logout);

        await _tokenRepo.RegistrarAsync(revocacion, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation("Cierre de sesion del usuario {UsuarioId}.", _contexto.UsuarioId);
    }
}