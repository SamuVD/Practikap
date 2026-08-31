using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Usuarios;

/// <summary>
/// Reasigna el rol de un usuario. Reservado al Administrador (RF-02, RN-01).
/// </summary>
public sealed class CambiarRolUseCase
{
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRolRepository _rolRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IRegistradorDeAuditoria _auditor;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CambiarRolRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CambiarRolUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="rolRepo">Catalogo de roles.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="auditor">Bitacora de acciones sensibles (P12, P13).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CambiarRolUseCase(
        IUsuarioRepository usuarioRepo,
        IRolRepository rolRepo,
        IContextoUsuario contexto,
        IRegistradorDeAuditoria auditor,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CambiarRolRequest> validador,
        IMapper mapeador,
        ILogger<CambiarRolUseCase> registro)
    {
        _usuarioRepo = usuarioRepo;
        _rolRepo = rolRepo;
        _contexto = contexto;
        _auditor = auditor;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Cambia el rol del usuario indicado.</summary>
    /// <param name="id">Usuario afectado.</param>
    /// <param name="request">Rol destino.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El usuario con su rol ya actualizado.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion (400).</exception>
    /// <exception cref="NoEncontradoException">Si el usuario no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">Si cambia su propio rol o el rol no existe (422).</exception>
    public async Task<UsuarioResponse> ExecuteAsync(
        int id, CambiarRolRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        // Autoproteccion (decision F4): un Administrador que se degrade a si
        // mismo pierde el acceso al unico endpoint capaz de revertirlo.
        if (_contexto.UsuarioId == id)
            throw new ReglaDeDominioException(
                "No puede cambiar su propio rol. Solicitelo a otro Administrador.", "RN-01");

        var usuario = await _usuarioRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Usuario", id);

        var roles = await _rolRepo.ListarAsync(ct);
        var rol = roles.FirstOrDefault(candidato => candidato.Id == request.RolId)
            ?? throw new ReglaDeDominioException("El rol indicado no existe.", "RN-01");

        // El rol anterior se resuelve antes de mutar la entidad, y sale del
        // catalogo que ya esta en memoria: no cuesta una consulta mas. CambiarRol
        // pisa RolId, de modo que leerlo despues asentaria el rol nuevo dos veces.
        var rolAnterior = roles.FirstOrDefault(candidato => candidato.Id == usuario.RolId)?.Nombre
            ?? usuario.RolId.ToString();

        usuario.CambiarRol(rol.Id);

        // RN-01. Antes de confirmar, para que el asiento caiga en el mismo
        // SaveChanges que el cambio (P12, ADR-02).
        await _auditor.PorCambioDeRolAsync(usuario.Id, rolAnterior, rol.Nombre, ct);

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Rol del usuario {UsuarioId} cambiado a {Rol} por el administrador {AdministradorId}.",
            usuario.Id, rol.Nombre, _contexto.UsuarioId);

        // Se relee para que la navegacion Rol refleje el rol nuevo y no el que
        // tenia cargado la instancia rastreada.
        var actualizado = await _usuarioRepo.ObtenerPorIdAsync(id, ct);
        return _mapeador.Map<UsuarioResponse>(actualizado!);
    }
}