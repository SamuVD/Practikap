using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IUsuarioRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M1.
/// </summary>
internal sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public UsuarioRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>Incluye el Rol: todo consumidor necesita el nombre del rol, no solo el Id.</remarks>
    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        _contexto.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    /// <inheritdoc />
    /// <remarks>
    /// Es la consulta del inicio de sesion: se rastrea (sin AsNoTracking) porque
    /// el caso de uso de login no modifica el usuario, pero el de cambio de
    /// contrasena si, y ambos comparten esta unica consulta por correo.
    /// </remarks>
    public Task<Usuario?> ObtenerPorCorreoAsync(string correo, CancellationToken ct) =>
        _contexto.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Correo == correo.Trim().ToLowerInvariant(), ct);

    /// <inheritdoc />
    public Task<bool> ExisteCorreoAsync(string correo, CancellationToken ct) =>
        _contexto.Usuarios
            .AsNoTracking()
            .AnyAsync(u => u.Correo == correo.Trim().ToLowerInvariant(), ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Usuario>> ListarPorRolAsync(int rolId, CancellationToken ct) =>
        await _contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.RolId == rolId)
            .OrderBy(u => u.Apellido).ThenBy(u => u.Nombre)
            .ToListAsync(ct);

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02): el Id que devuelve solo queda poblado despues de
    /// que el caso de uso llame IUnidadDeTrabajo.GuardarCambiosAsync. El valor
    /// de retorno inmediato de esta llamada no debe usarse para construir una
    /// respuesta; se lee usuario.Id sobre la misma instancia tras confirmar.
    /// </remarks>
    public Task<int> AgregarAsync(Usuario usuario, CancellationToken ct)
    {
        _contexto.Usuarios.Add(usuario);
        return Task.FromResult(usuario.Id);
    }

    /// <inheritdoc />
    /// <remarks>
    /// El caso de uso tipico obtiene el usuario con ObtenerPorIdAsync (queda
    /// rastreado), lo modifica con sus metodos de dominio y EF Core detecta el
    /// cambio sin llamada adicional. Este metodo solo actua cuando el usuario
    /// llega desatado -por ejemplo, reconstruido en una prueba-, para no
    /// depender de que el llamador siempre use la instancia rastreada.
    /// </remarks>
    public Task ActualizarAsync(Usuario usuario, CancellationToken ct)
    {
        if (_contexto.Entry(usuario).State == EntityState.Detached)
            _contexto.Usuarios.Update(usuario);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>Materializa RN-01. Lanza si el usuario no existe (HTTP 404).</remarks>
    public async Task CambiarRolAsync(int usuarioId, int rolId, CancellationToken ct)
    {
        var usuario = await _contexto.Usuarios.FindAsync(new object?[] { usuarioId }, ct)
            ?? throw new NoEncontradoException($"No existe un usuario con Id {usuarioId}.");

        usuario.CambiarRol(rolId);
    }
}