using AutoMapper;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Usuarios;

/// <summary>
/// Habilita o deshabilita una cuenta. Reservado al Administrador (RF-02).
/// </summary>
/// <remarks>
/// Es la baja del sistema: las cuentas nunca se eliminan, porque las claves
/// foraneas usan ON DELETE RESTRICT y el historial de practicas debe
/// conservarse (decision F3).
///
/// Una cuenta desactivada deja de poder iniciar sesion, pero su token vigente
/// sigue siendo valido hasta expirar (FA-20).
///
/// Es el unico caso de uso del modulo M1 que no inyecta IValidator&lt;TRequest&gt;.
/// Es una desviacion intencional de la decision D3, no un olvido:
/// CambiarEstadoRequest declara un unico bool obligatorio y el enlace de modelo
/// de ASP.NET Core ya rechaza cualquier cuerpo que no lo traiga. Un validador
/// aqui no tendria ninguna regla que declarar.
/// </remarks>
public sealed class CambiarEstadoUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IRegistradorDeAuditoria _auditor;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IMapper _mapeador;
    private readonly ILogger<CambiarEstadoUsuarioUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="auditor">Bitacora de acciones sensibles (P12, P13).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CambiarEstadoUsuarioUseCase(
        IUsuarioRepository usuarioRepo,
        IContextoUsuario contexto,
        IRegistradorDeAuditoria auditor,
        IUnidadDeTrabajo unidadDeTrabajo,
        IMapper mapeador,
        ILogger<CambiarEstadoUsuarioUseCase> registro)
    {
        _usuarioRepo = usuarioRepo;
        _contexto = contexto;
        _auditor = auditor;
        _unidadDeTrabajo = unidadDeTrabajo;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica el estado indicado a la cuenta.</summary>
    /// <param name="id">Usuario afectado.</param>
    /// <param name="request">Estado destino.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El usuario con su estado ya actualizado.</returns>
    /// <exception cref="NoEncontradoException">Si el usuario no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">Si intenta desactivarse a si mismo (422).</exception>
    public async Task<UsuarioResponse> ExecuteAsync(
        int id, CambiarEstadoRequest request, CancellationToken ct)
    {
        // Autoproteccion (decision F4). Junto con la del cambio de rol, garantiza
        // que siempre quede al menos una cuenta de Administrador activa, sin
        // necesidad de contar administradores en cada operacion.
        if (_contexto.UsuarioId == id && !request.Activo)
            throw new ReglaDeDominioException("No puede desactivar su propia cuenta.", "RN-01");

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Usuario", id);

        if (request.Activo)
            usuario.Activar();
        else
            usuario.Desactivar();

        // La baja de una cuenta es una accion sensible aunque no tenga literal
        // propio en el ENUM de auditoria.accion: entra como Otro (P13).
        await _auditor.PorCambioDeEstadoDeUsuarioAsync(usuario.Id, request.Activo, ct);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Estado del usuario {UsuarioId} cambiado a {Estado} por el administrador {AdministradorId}.",
            usuario.Id, usuario.Estado, _contexto.UsuarioId);

        return _mapeador.Map<UsuarioResponse>(usuario);
    }
}