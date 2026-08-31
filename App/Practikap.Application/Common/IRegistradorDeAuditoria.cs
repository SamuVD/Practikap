using Practikap.Domain.Enums;

namespace Practikap.Application.Common;

/// <summary>
/// Punto unico de escritura de la bitacora de acciones sensibles (CU-08, P12).
/// Los casos de uso que ejecutan una accion auditable lo invocan; ninguno
/// construye un <see cref="Domain.Entities.RegistroAuditoria"/> por su cuenta.
/// </summary>
/// <remarks>
/// <para>
/// <b>Solo registra, nunca confirma.</b> No depende de IUnidadDeTrabajo y no
/// tiene forma de llamar a GuardarCambiosAsync. Quien confirma es el caso de uso
/// invocante, de modo que la accion y su asiento caen en el mismo SaveChanges y,
/// por tanto, en la misma transaccion implicita del DbContext (ADR-02). Es la
/// misma forma exacta de <see cref="IGeneradorDeNotificaciones"/> (L6) y de
/// <see cref="IEvaluadorDeReglas"/> (N11), y por el mismo motivo: si la accion
/// falla al confirmarse no queda un asiento anunciando algo que nunca ocurrio.
/// </para>
/// <para>
/// De ahi la regla de invocacion: la llamada va <b>antes</b> de
/// GuardarCambiosAsync, no despues. Colocarla despues produciria dos
/// confirmaciones separadas y romperia la promesa sin que nada dejara de
/// compilar. Y el asiento <b>no se envuelve en un try</b> (P15): si falla, la
/// operacion falla con el. Una bitacora que se puede saltar en silencio no es
/// una bitacora.
/// </para>
/// <para>
/// <b>Dos caminos se desvian de esa regla, forzados por el lenguaje y por la
/// base.</b> El alta de una regla y la creacion de una entrada de configuracion
/// componen un agregado cuyo Id vale 0 hasta que MySQL lo asigna, y el
/// constructor de RegistroAuditoria exige un identificador mayor que cero. Esos
/// dos confirman, componen el asiento con el Id ya asignado y confirman por
/// segunda vez. La desviacion queda anotada en el punto exacto donde ocurre.
/// </para>
/// <para>
/// <b>El actor sale de <see cref="IContextoUsuario"/> dentro del registrador, no
/// por parametro.</b> Quien invoca ya esta autenticado, y admitir el
/// identificador desde afuera seria darle a cada caso de uso la oportunidad de
/// mentir sobre quien ejecuto la accion. Es el mismo criterio con el que
/// PorRiesgoAsync deriva su regla_id. Esto hace del registrador el primer
/// servicio transversal de Aplicacion que conoce al solicitante: ni el generador
/// de notificaciones ni el evaluador de reglas lo necesitan.
/// </para>
/// <para>
/// Un metodo por accion y no una firma generica con la accion y el texto por
/// parametro. La redaccion del detalle vive en un solo archivo y la
/// <see cref="AccionAuditoria"/> la elige quien sabe de auditoria, no cada modulo
/// invocante. <see cref="PorAnulacionAsync"/> es la unica excepcion y recibe la
/// <see cref="EntidadAuditada"/>: las cuatro anulaciones de M4 y M5 son el mismo
/// suceso sobre cuatro tablas distintas, de modo que ahi la entidad es un dato y
/// no un tipo de suceso.
/// </para>
/// <para>
/// El contrato es publico y su implementacion internal sealed, con el mismo
/// reparto de las otras dos piezas transversales: los casos de uso que lo reciben
/// son clases publicas, y un constructor publico no admite un parametro de un
/// tipo menos accesible. Que la implementacion no salga del ensamblado es lo que
/// mantiene el punto de escritura unico.
/// </para>
/// <para>
/// <b>La bitacora registra acciones de un actor humano</b> (RN-01, RN-05, RN-08,
/// RN-12). Las transiciones automaticas no dejan asiento: ni el MarcarEnRiesgo
/// del Motor ni el estado por defecto de RN-06 (P18).
/// </para>
/// </remarks>
public interface IRegistradorDeAuditoria
{
    /// <summary>
    /// Asienta el cambio de rol de un usuario (RN-01), sobre
    /// <see cref="EntidadAuditada.Usuarios"/> y con accion
    /// <see cref="AccionAuditoria.CambioRol"/>.
    /// </summary>
    /// <param name="usuarioId">Usuario afectado, no el administrador que ejecuta.</param>
    /// <param name="rolAnterior">Nombre del rol que tenia.</param>
    /// <param name="rolNuevo">Nombre del rol que pasa a tener.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    Task PorCambioDeRolAsync(
        int usuarioId, string rolAnterior, string rolNuevo, CancellationToken ct);

    /// <summary>
    /// Asienta el alta o la baja de una cuenta, sobre
    /// <see cref="EntidadAuditada.Usuarios"/> y con accion
    /// <see cref="AccionAuditoria.Otro"/>.
    /// </summary>
    /// <param name="usuarioId">Usuario afectado.</param>
    /// <param name="activo">true si la cuenta queda habilitada.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <remarks>
    /// La accion es Otro y no una propia porque el ENUM de auditoria.accion es
    /// cerrado en el DDL y no la contempla. Ampliarlo exigiria una migracion, y
    /// esta ronda no toca el esquema.
    /// </remarks>
    Task PorCambioDeEstadoDeUsuarioAsync(int usuarioId, bool activo, CancellationToken ct);

    /// <summary>
    /// Asienta un cambio manual de estado de practica (RN-05), sobre
    /// <see cref="EntidadAuditada.Practicas"/>.
    /// </summary>
    /// <param name="practicaId">Practica afectada.</param>
    /// <param name="origen">Estado desde el que se movio, capturado antes de mutar la entidad.</param>
    /// <param name="destino">Estado al que llego.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <remarks>
    /// P14 decide la accion. Se compara el rango ordinal de los dos estados
    /// —Pendiente 0, En curso y En riesgo 1, Finalizada 2—: si el destino es
    /// menor que el origen, la accion es
    /// <see cref="AccionAuditoria.RetrocesoEstado"/>; en cualquier otro caso,
    /// <see cref="AccionAuditoria.Otro"/>.
    ///
    /// <b>Se audita todo cambio manual, no solo los retrocesos.</b> Perder el
    /// rastro de un avance ejecutado a mano seria peor que rotularlo de mas. El
    /// paso lateral En curso ↔ En riesgo cae en Otro, que es lo correcto: no
    /// retrocede en la secuencia de RN-05.
    /// </remarks>
    Task PorCambioDeEstadoDePracticaAsync(
        int practicaId, EstadoPractica origen, EstadoPractica destino, CancellationToken ct);

    /// <summary>
    /// Asienta la reasignacion de los participantes de una practica (RN-04),
    /// sobre <see cref="EntidadAuditada.Practicas"/> y con accion
    /// <see cref="AccionAuditoria.Reasignacion"/>.
    /// </summary>
    /// <param name="practicaId">Practica afectada.</param>
    /// <param name="instructorAnterior">Instructor que tenia, capturado antes de reasignar.</param>
    /// <param name="instructorNuevo">Instructor que pasa a tener.</param>
    /// <param name="aprendizAnterior">Aprendiz que tenia, capturado antes de reasignar.</param>
    /// <param name="aprendizNuevo">Aprendiz que pasa a tener.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <remarks>
    /// Quien decide si hay asiento es el caso de uso, no este metodo: una edicion
    /// que solo cambia la modalidad no es una reasignacion y no llega aqui.
    /// </remarks>
    Task PorReasignacionAsync(
        int practicaId,
        int instructorAnterior, int instructorNuevo,
        int aprendizAnterior, int aprendizNuevo,
        CancellationToken ct);

    /// <summary>
    /// Asienta la anulacion de un registro del historial (RN-12), con accion
    /// <see cref="AccionAuditoria.Anulacion"/>.
    /// </summary>
    /// <param name="entidad">
    /// Tabla sobre la que se anulo: seguimientos, observaciones,
    /// calificaciones_instructor o calificaciones_aprendiz.
    /// </param>
    /// <param name="registroId">Registro anulado.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <remarks>
    /// <b>Es el unico metodo que recibe la entidad por parametro, y no contradice
    /// el criterio de un metodo por evento.</b> Las cuatro anulaciones de M4 y M5
    /// son el mismo suceso —el Administrador retira un registro del historial sin
    /// borrarlo— ejecutado sobre cuatro tablas. La entidad es entonces un dato de
    /// ese suceso y no un tipo de suceso distinto, de modo que separarlo en cuatro
    /// metodos identicos solo repetiria la redaccion cuatro veces.
    /// </remarks>
    Task PorAnulacionAsync(EntidadAuditada entidad, int registroId, CancellationToken ct);

    /// <summary>
    /// Asienta el alta, la edicion o el cambio de estado de una regla del Motor
    /// (RN-08), sobre <see cref="EntidadAuditada.Reglas"/> y con accion
    /// <see cref="AccionAuditoria.ConfiguracionRegla"/>.
    /// </summary>
    /// <param name="reglaId">Regla afectada. En el alta, ya confirmada.</param>
    /// <param name="nombreDeRegla">Nombre de la regla, para que el asiento se lea sin cruzar tablas.</param>
    /// <param name="operacion">
    /// Verbo de lo ocurrido, tal como debe leerse en el detalle: "Alta",
    /// "Edicion", "Activacion" o "Desactivacion". Lo aporta el invocante porque
    /// los tres casos de uso de reglas comparten accion y solo difieren en el
    /// verbo.
    /// </param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <remarks>
    /// Es el asiento que da sentido a RN-08: si el comportamiento de la plataforma
    /// se cambia sin desplegar, la unica traza de quien lo cambio es esta.
    /// </remarks>
    Task PorConfiguracionDeReglaAsync(
        int reglaId, string nombreDeRegla, string operacion, CancellationToken ct);

    /// <summary>
    /// Asienta el establecimiento de una clave de configuracion (RF-09, RN-08),
    /// sobre <see cref="EntidadAuditada.Configuracion"/> y con accion
    /// <see cref="AccionAuditoria.Otro"/>.
    /// </summary>
    /// <param name="entradaId">Entrada afectada. En la creacion, ya confirmada.</param>
    /// <param name="clave">Clave establecida.</param>
    /// <param name="valor">Valor que pasa a tener.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <remarks>
    /// La accion es Otro por la misma razon que en el cambio de estado de una
    /// cuenta: el ENUM del DDL no tiene un literal para esto y la ronda no toca el
    /// esquema. Configuracion_regla existe, pero nombra al Motor y usarla aqui
    /// haria que un filtro por esa accion devolviera dos cosas distintas.
    /// </remarks>
    Task PorCambioDeConfiguracionAsync(
        int entradaId, string clave, string valor, CancellationToken ct);
}
