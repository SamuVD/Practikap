using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.Common;

/// <summary>
/// Unica implementacion de <see cref="IRegistradorDeAuditoria"/> (P12). Es el
/// unico archivo del sistema que construye un <see cref="RegistroAuditoria"/> y
/// el unico que redacta el texto de la columna detalle.
/// </summary>
/// <remarks>
/// No depende de IUnidadDeTrabajo, y esa ausencia es deliberada por el mismo
/// motivo que en <see cref="GeneradorDeNotificaciones"/> y en
/// <see cref="EvaluadorDeReglas"/>: es lo que hace imposible que esta clase
/// confirme por su cuenta y lo que deja el asiento dentro de la transaccion de la
/// accion que lo origino (ADR-02).
///
/// Vive en Aplicacion y no en Infraestructura porque no toca EF Core: habla
/// contra <see cref="IAuditoriaRepository"/>, que es un contrato del Dominio.
///
/// <b>Dos colaboradores y ninguna decision de negocio.</b> Lo unico que decide
/// este archivo es el rango ordinal de P14 y la redaccion del detalle. Quien
/// decide si una accion merece asiento es el caso de uso.
/// </remarks>
internal sealed class RegistradorDeAuditoria : IRegistradorDeAuditoria
{
    /// <summary>
    /// Largo de auditoria.detalle en el Script_DDL.sql. Se recorta aqui y no se
    /// confia en que quepa: la columna es VARCHAR(255) NULL y ni
    /// RegistroAuditoriaConfiguration ni la entidad declaran HasMaxLength, de modo
    /// que un texto mas largo no fallaria en el cliente sino en MySQL, y lo haria
    /// tumbando la accion legitima que lo produjo.
    /// </summary>
    private const int LargoMaximoDeDetalle = 255;

    private readonly IAuditoriaRepository _auditoriaRepo;
    private readonly IContextoUsuario _contexto;

    /// <summary>Crea el registrador.</summary>
    /// <param name="auditoriaRepo">Destino del asiento.</param>
    /// <param name="contexto">Actor de la accion, tomado del token (ADR-03, P12).</param>
    public RegistradorDeAuditoria(
        IAuditoriaRepository auditoriaRepo,
        IContextoUsuario contexto)
    {
        _auditoriaRepo = auditoriaRepo;
        _contexto = contexto;
    }

    /// <inheritdoc />
    public Task PorCambioDeRolAsync(
        int usuarioId, string rolAnterior, string rolNuevo, CancellationToken ct) =>
        AsentarAsync(
            EntidadAuditada.Usuarios, usuarioId, AccionAuditoria.CambioRol,
            $"Rol cambiado de {rolAnterior} a {rolNuevo}.", ct);

    /// <inheritdoc />
    public Task PorCambioDeEstadoDeUsuarioAsync(
        int usuarioId, bool activo, CancellationToken ct) =>
        AsentarAsync(
            EntidadAuditada.Usuarios, usuarioId, AccionAuditoria.Otro,
            activo ? "Cuenta activada." : "Cuenta desactivada.", ct);

    /// <inheritdoc />
    public Task PorCambioDeEstadoDePracticaAsync(
        int practicaId, EstadoPractica origen, EstadoPractica destino, CancellationToken ct)
    {
        // P14. Los enumerados viajan como nombre de miembro y no como literal del
        // DDL, igual que en toda respuesta de la API (H31).
        var accion = EsRetroceso(origen, destino)
            ? AccionAuditoria.RetrocesoEstado
            : AccionAuditoria.Otro;

        return AsentarAsync(
            EntidadAuditada.Practicas, practicaId, accion,
            $"Estado cambiado de {origen} a {destino}.", ct);
    }

    /// <inheritdoc />
    public Task PorReasignacionAsync(
        int practicaId,
        int instructorAnterior, int instructorNuevo,
        int aprendizAnterior, int aprendizNuevo,
        CancellationToken ct) =>
        AsentarAsync(
            EntidadAuditada.Practicas, practicaId, AccionAuditoria.Reasignacion,
            $"Instructor {instructorAnterior} a {instructorNuevo}. "
            + $"Aprendiz {aprendizAnterior} a {aprendizNuevo}.", ct);

    /// <inheritdoc />
    public Task PorAnulacionAsync(
        EntidadAuditada entidad, int registroId, CancellationToken ct) =>
        AsentarAsync(
            entidad, registroId, AccionAuditoria.Anulacion,
            "Registro retirado del historial por supervision (RN-12).", ct);

    /// <inheritdoc />
    public Task PorConfiguracionDeReglaAsync(
        int reglaId, string nombreDeRegla, string operacion, CancellationToken ct) =>
        AsentarAsync(
            EntidadAuditada.Reglas, reglaId, AccionAuditoria.ConfiguracionRegla,
            $"{operacion} de la regla '{nombreDeRegla}'.", ct);

    /// <inheritdoc />
    public Task PorCambioDeConfiguracionAsync(
        int entradaId, string clave, string valor, CancellationToken ct) =>
        AsentarAsync(
            EntidadAuditada.Configuracion, entradaId, AccionAuditoria.Otro,
            $"Clave '{clave}' establecida en '{valor}'.", ct);

    /// <summary>
    /// Rango de un estado dentro de la secuencia de RN-05. En curso y En riesgo
    /// comparten rango porque el paso entre ellos es lateral y no retrocede.
    /// </summary>
    /// <param name="estado">Estado medido.</param>
    /// <returns>0 para Pendiente, 1 para En curso y En riesgo, 2 para Finalizada.</returns>
    private static int RangoDe(EstadoPractica estado) => estado switch
    {
        EstadoPractica.Pendiente => 0,
        EstadoPractica.EnCurso => 1,
        EstadoPractica.EnRiesgo => 1,
        EstadoPractica.Finalizada => 2,
        _ => 1
    };

    /// <summary>Indica si la transicion retrocede en la secuencia de RN-05.</summary>
    /// <param name="origen">Estado de partida.</param>
    /// <param name="destino">Estado de llegada.</param>
    /// <returns>true si el rango del destino es menor que el del origen.</returns>
    /// <remarks>
    /// No se usa <c>Practica.EsAvance</c> aqui, aunque exista: aquel responde si
    /// la transicion esta permitida sin Administrador, que es otra pregunta.
    /// En riesgo a En curso es un avance para RN-05 y aqui tambien sale Otro,
    /// pero Finalizada a En curso es un retroceso en las dos lecturas y En curso a
    /// En riesgo es avance en aquella y Otro en esta. Confundirlas rotularia mal
    /// la mitad de los asientos.
    /// </remarks>
    private static bool EsRetroceso(EstadoPractica origen, EstadoPractica destino) =>
        RangoDe(destino) < RangoDe(origen);

    /// <summary>
    /// Compone el asiento con el actor del token y lo entrega al repositorio.
    /// </summary>
    /// <param name="entidad">Tabla sobre la que se actuo.</param>
    /// <param name="entidadId">Registro afectado. Debe estar confirmado y valer mas que cero.</param>
    /// <param name="accion">Accion ejecutada.</param>
    /// <param name="detalle">Texto redactado por el metodo publico.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    private async Task AsentarAsync(
        EntidadAuditada entidad, int entidadId, AccionAuditoria accion,
        string detalle, CancellationToken ct)
    {
        var registro = new RegistroAuditoria(
            _contexto.UsuarioId, entidad, entidadId, accion, Recortar(detalle));

        await _auditoriaRepo.RegistrarAsync(registro, ct);

        // Sin GuardarCambiosAsync, y sin manera de invocarlo. Confirma el caso de
        // uso que ejecuto la accion, en una sola transaccion con ella (P12,
        // ADR-02).
    }

    /// <summary>Recorta el detalle al largo de la columna.</summary>
    /// <param name="detalle">Texto redactado.</param>
    /// <returns>El texto, nunca mas largo que la columna admite.</returns>
    private static string Recortar(string detalle) =>
        detalle.Length <= LargoMaximoDeDetalle
            ? detalle
            : detalle[..LargoMaximoDeDetalle];
}
