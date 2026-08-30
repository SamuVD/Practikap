using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Practicas;

/// <summary>
/// Verificacion compartida de los dos participantes de una practica (H7).
/// </summary>
/// <remarks>
/// La comparten CrearPracticaUseCase y ActualizarPracticaUseCase, que son los
/// dos puntos donde entra un par instructor-aprendiz desde el exterior. Es una
/// clase estatica y no un servicio: no tiene estado ni dependencias propias,
/// asi que no entra en el contenedor y no contradice ADR-05, que enumera casos
/// de uso.
///
/// Los tres fallos son 422 y no 404: el identificador no viaja en la ruta sino
/// en el cuerpo, de modo que no es el recurso lo que falta, es la solicitud lo
/// que no se puede procesar.
/// </remarks>
internal static class ParticipantesDePractica
{
    private const string RolInstructor = "Instructor";
    private const string RolAprendiz = "Aprendiz";

    /// <summary>
    /// Comprueba que instructor y aprendiz existan, tengan el rol que les
    /// corresponde y conserven la cuenta activa.
    /// </summary>
    /// <param name="usuarioRepo">Acceso a usuarios, del modulo M1.</param>
    /// <param name="instructorId">Instructor responsable propuesto.</param>
    /// <param name="aprendizId">Aprendiz titular propuesto.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <exception cref="ReglaDeDominioException">
    /// Si alguno no existe, no tiene el rol esperado o su cuenta esta inactiva (422).
    /// </exception>
    public static async Task VerificarAsync(
        IUsuarioRepository usuarioRepo, int instructorId, int aprendizId, CancellationToken ct)
    {
        await VerificarUnoAsync(usuarioRepo, instructorId, RolInstructor, ct);
        await VerificarUnoAsync(usuarioRepo, aprendizId, RolAprendiz, ct);
    }

    private static async Task VerificarUnoAsync(
        IUsuarioRepository usuarioRepo, int usuarioId, string rolEsperado, CancellationToken ct)
    {
        // ObtenerPorIdAsync incluye la navegacion Rol, de modo que el nombre del
        // rol esta disponible sin una consulta adicional.
        var usuario = await usuarioRepo.ObtenerPorIdAsync(usuarioId, ct)
            ?? throw new ReglaDeDominioException(
                $"El usuario {usuarioId}, indicado como {rolEsperado.ToLowerInvariant()}, no existe.",
                "RN-04");

        if (usuario.Rol.Nombre != rolEsperado)
            throw new ReglaDeDominioException(
                $"El usuario {usuarioId} no tiene rol {rolEsperado}.", "RN-04");

        if (!usuario.EstaActivo)
            throw new ReglaDeDominioException(
                $"La cuenta del {rolEsperado.ToLowerInvariant()} {usuarioId} esta inactiva.", "RN-04");
    }
}
