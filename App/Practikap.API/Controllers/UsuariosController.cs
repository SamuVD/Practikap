using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Application.UseCases.Usuarios;

namespace Practikap.API.Controllers;

/// <summary>
/// Gestion de usuarios. Modulo M1, CU-01 y CU-08 (RF-02, RN-01, RN-13).
/// </summary>
/// <remarks>
/// No expone DELETE: las cuentas se dan de baja con PATCH sobre el sub-recurso
/// estado, nunca se eliminan (decision F3).
///
/// Los atributos [Authorize(Roles = ...)] son la primera barrera; la segunda
/// vive en los casos de uso, que comprueban el alcance de RN-13 sobre
/// IContextoUsuario (ADR-03). Los endpoints que aceptan tanto al propietario
/// como al Administrador no pueden restringirse por atributo y delegan por
/// completo esa decision en el caso de uso.
/// </remarks>
[ApiController]
[Route("api/usuarios")]
[Authorize]
public sealed class UsuariosController : ControllerBase
{
    private readonly ListarUsuariosUseCase _listar;
    private readonly ObtenerUsuarioUseCase _obtener;
    private readonly CrearUsuarioUseCase _crear;
    private readonly ActualizarPerfilUseCase _actualizarPerfil;
    private readonly CambiarContrasenaUseCase _cambiarContrasena;
    private readonly RestablecerContrasenaUseCase _restablecerContrasena;
    private readonly CambiarRolUseCase _cambiarRol;
    private readonly CambiarEstadoUsuarioUseCase _cambiarEstado;

    /// <summary>Crea el controlador.</summary>
    /// <param name="listar">Listado de usuarios.</param>
    /// <param name="obtener">Consulta de un usuario.</param>
    /// <param name="crear">Alta de usuario.</param>
    /// <param name="actualizarPerfil">Edicion de datos personales.</param>
    /// <param name="cambiarContrasena">Cambio de contrasena propia.</param>
    /// <param name="restablecerContrasena">Restablecimiento administrativo.</param>
    /// <param name="cambiarRol">Reasignacion de rol.</param>
    /// <param name="cambiarEstado">Habilitacion o baja de la cuenta.</param>
    public UsuariosController(
        ListarUsuariosUseCase listar,
        ObtenerUsuarioUseCase obtener,
        CrearUsuarioUseCase crear,
        ActualizarPerfilUseCase actualizarPerfil,
        CambiarContrasenaUseCase cambiarContrasena,
        RestablecerContrasenaUseCase restablecerContrasena,
        CambiarRolUseCase cambiarRol,
        CambiarEstadoUsuarioUseCase cambiarEstado)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizarPerfil = actualizarPerfil;
        _cambiarContrasena = cambiarContrasena;
        _restablecerContrasena = restablecerContrasena;
        _cambiarRol = cambiarRol;
        _cambiarEstado = cambiarEstado;
    }

    /// <summary>Lista los usuarios del sistema, con filtro opcional por rol.</summary>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <param name="rolId">Rol por el que filtrar. Se omite para obtener todos.</param>
    /// <returns>Coleccion de usuarios.</returns>
    /// <response code="200">Listado de usuarios.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    [HttpGet]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Listar(CancellationToken ct, [FromQuery] int? rolId = null) =>
        Ok(await _listar.ExecuteAsync(rolId, ct));

    /// <summary>Obtiene un usuario por su identificador.</summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Datos del usuario.</returns>
    /// <response code="200">Usuario encontrado.</response>
    /// <response code="403">Consulta a otro usuario sin ser Administrador.</response>
    /// <response code="404">El usuario no existe.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) =>
        Ok(await _obtener.ExecuteAsync(id, ct));

    /// <summary>Da de alta un usuario.</summary>
    /// <param name="request">Datos del usuario a crear.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El usuario creado.</returns>
    /// <response code="201">Usuario creado.</response>
    /// <response code="400">Los datos no superan la validacion de forma.</response>
    /// <response code="409">El correo ya esta registrado.</response>
    /// <response code="422">El rol indicado no existe.</response>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(CrearUsuarioRequest request, CancellationToken ct)
    {
        var creado = await _crear.ExecuteAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    /// <summary>Actualiza los datos personales de un usuario.</summary>
    /// <param name="id">Usuario a modificar.</param>
    /// <param name="request">Datos personales nuevos.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El usuario actualizado.</returns>
    /// <response code="200">Perfil actualizado.</response>
    /// <response code="403">Edicion de otro usuario sin ser Administrador.</response>
    /// <response code="404">El usuario no existe.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActualizarPerfil(
        int id, ActualizarPerfilRequest request, CancellationToken ct) =>
        Ok(await _actualizarPerfil.ExecuteAsync(id, request, ct));

    /// <summary>
    /// Cambia la contrasena propia. Revoca el token en curso, de modo que hay
    /// que volver a iniciar sesion (RN-03).
    /// </summary>
    /// <param name="id">Usuario afectado. Debe ser el propio solicitante.</param>
    /// <param name="request">Contrasena actual y nueva.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">Contrasena cambiada y sesion cerrada.</response>
    /// <response code="401">La contrasena actual no es correcta.</response>
    /// <response code="403">Intento de cambiar la contrasena de otro usuario.</response>
    [HttpPut("{id:int}/contrasena")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CambiarContrasena(
        int id, CambiarContrasenaRequest request, CancellationToken ct)
    {
        await _cambiarContrasena.ExecuteAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>
    /// Restablece la contrasena de un usuario sin exigir la actual. Es la unica
    /// salida cuando alguien la olvida, porque en v1 no hay recuperacion
    /// autoservicio (FA-21).
    /// </summary>
    /// <param name="id">Usuario afectado.</param>
    /// <param name="request">Contrasena de reemplazo.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>Sin contenido.</returns>
    /// <response code="204">Contrasena restablecida.</response>
    /// <response code="403">El rol autenticado no es Administrador.</response>
    /// <response code="404">El usuario no existe.</response>
    [HttpPatch("{id:int}/contrasena")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestablecerContrasena(
        int id, RestablecerContrasenaRequest request, CancellationToken ct)
    {
        await _restablecerContrasena.ExecuteAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Reasigna el rol de un usuario (RN-01).</summary>
    /// <param name="id">Usuario afectado.</param>
    /// <param name="request">Rol destino.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El usuario con su rol actualizado.</returns>
    /// <response code="200">Rol cambiado.</response>
    /// <response code="404">El usuario no existe.</response>
    /// <response code="422">Cambio del propio rol, o el rol indicado no existe.</response>
    [HttpPatch("{id:int}/rol")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CambiarRol(
        int id, CambiarRolRequest request, CancellationToken ct) =>
        Ok(await _cambiarRol.ExecuteAsync(id, request, ct));

    /// <summary>Habilita o deshabilita una cuenta. Sustituye a la eliminacion.</summary>
    /// <param name="id">Usuario afectado.</param>
    /// <param name="request">Estado destino.</param>
    /// <param name="ct">Token de cancelacion, inyectado por ASP.NET Core.</param>
    /// <returns>El usuario con su estado actualizado.</returns>
    /// <response code="200">Estado cambiado.</response>
    /// <response code="404">El usuario no existe.</response>
    /// <response code="422">Intento de desactivar la propia cuenta.</response>
    [HttpPatch("{id:int}/estado")]
    [Authorize(Roles = "Administrador")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RespuestaDeError), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CambiarEstado(
        int id, CambiarEstadoRequest request, CancellationToken ct) =>
        Ok(await _cambiarEstado.ExecuteAsync(id, request, ct));
}