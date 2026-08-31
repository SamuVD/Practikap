using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Practica productiva. Raiz central del sistema: gobierna la asignacion unica
/// instructor-aprendiz (RN-04) y la maquina de estados de RN-05.
/// </summary>
public class Practica
{
    /// <summary>
    /// Transiciones de avance permitidas sin intervencion del Administrador.
    /// Cualquier par que no aparezca aqui se considera retroceso y queda
    /// reservado al Administrador, tal como exige RN-05.
    /// </summary>
    private static readonly (EstadoPractica Origen, EstadoPractica Destino)[] TransicionesDeAvance =
    [
        (EstadoPractica.Pendiente, EstadoPractica.EnCurso),
        (EstadoPractica.EnCurso,   EstadoPractica.Finalizada),
        (EstadoPractica.EnCurso,   EstadoPractica.EnRiesgo),
        (EstadoPractica.EnRiesgo,  EstadoPractica.EnCurso),
        (EstadoPractica.EnRiesgo,  EstadoPractica.Finalizada)
    ];

    private readonly List<Seguimiento> _seguimientos = [];

    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Practica() { }

    /// <summary>Crea una practica productiva en estado Pendiente.</summary>
    /// <param name="fichaId">Ficha de formacion del aprendiz.</param>
    /// <param name="instructorId">Unico instructor responsable (RN-04).</param>
    /// <param name="aprendizId">Unico aprendiz titular (RN-04).</param>
    /// <param name="modalidad">Modalidad bajo la que se desarrolla la practica.</param>
    /// <param name="fechaInicio">Fecha de inicio de la practica.</param>
    /// <param name="empresaId">Empresa receptora. Obligatoria salvo en Proyecto productivo y Monitoria.</param>
    /// <param name="fechaFin">Fecha de cierre prevista. Opcional.</param>
    /// <exception cref="ReglaDeDominioException">
    /// Si falta un participante, si instructor y aprendiz coinciden, si la
    /// combinacion de modalidad y empresa es incoherente o si el rango de
    /// fechas es invalido.
    /// </exception>
    public Practica(int fichaId, int instructorId, int aprendizId,
                    ModalidadPractica modalidad, DateOnly fechaInicio,
                    int? empresaId = null, DateOnly? fechaFin = null)
    {
        if (fichaId <= 0)
            throw new ReglaDeDominioException("La practica debe pertenecer a una ficha valida.");
        if (instructorId <= 0)
            throw new ReglaDeDominioException("La practica requiere un instructor asignado.", "RN-04");
        if (aprendizId <= 0)
            throw new ReglaDeDominioException("La practica requiere un aprendiz asignado.", "RN-04");
        if (instructorId == aprendizId)
            throw new ReglaDeDominioException("El instructor y el aprendiz no pueden ser el mismo usuario.", "RN-04");

        ValidarModalidadYEmpresa(modalidad, empresaId);
        ValidarRangoDeFechas(fechaInicio, fechaFin);

        FichaId = fichaId;
        InstructorId = instructorId;
        AprendizId = aprendizId;
        Modalidad = modalidad;
        EmpresaId = empresaId;
        FechaInicio = fechaInicio;
        FechaFin = fechaFin;
        Estado = EstadoPractica.Pendiente;
    }

    /// <summary>Identificador. Columna practicas.id.</summary>
    public int Id { get; private set; }

    /// <summary>Ficha de formacion. Columna practicas.ficha_id.</summary>
    public int FichaId { get; private set; }

    /// <summary>Empresa receptora, nula en Proyecto productivo y Monitoria. Columna practicas.empresa_id.</summary>
    public int? EmpresaId { get; private set; }

    /// <summary>Instructor responsable. Columna practicas.instructor_id.</summary>
    public int InstructorId { get; private set; }

    /// <summary>Aprendiz titular. Columna practicas.aprendiz_id.</summary>
    public int AprendizId { get; private set; }

    /// <summary>Modalidad de la practica. Columna practicas.modalidad.</summary>
    public ModalidadPractica Modalidad { get; private set; }

    /// <summary>Estado actual dentro del ciclo de vida. Columna practicas.estado.</summary>
    public EstadoPractica Estado { get; private set; }

    /// <summary>Fecha de inicio. Columna practicas.fecha_inicio, de tipo DATE.</summary>
    public DateOnly FechaInicio { get; private set; }

    /// <summary>Fecha de cierre. Columna practicas.fecha_fin, de tipo DATE.</summary>
    public DateOnly? FechaFin { get; private set; }

    /// <summary>Fecha de alta del registro. La genera MySQL con DEFAULT CURRENT_TIMESTAMP.</summary>
    public DateTime FechaCreacion { get; private set; }

    /// <summary>Ficha de formacion asociada.</summary>
    public Ficha Ficha { get; private set; } = null!;

    /// <summary>Empresa receptora, cuando la modalidad la exige.</summary>
    public Empresa? Empresa { get; private set; }

    /// <summary>Instructor responsable de la practica.</summary>
    public Usuario Instructor { get; private set; } = null!;

    /// <summary>Aprendiz titular de la practica.</summary>
    public Usuario Aprendiz { get; private set; } = null!;

    /// <summary>Historial de seguimientos de la practica.</summary>
    public IReadOnlyCollection<Seguimiento> Seguimientos => _seguimientos;

    /// <summary>
    /// Indica si la practica cuenta como activa para efectos de RN-04, es decir
    /// si esta Pendiente, En curso o En riesgo. Una practica Finalizada no
    /// bloquea la creacion de una nueva para el mismo aprendiz.
    /// </summary>
    public bool EstaActiva => Estado != EstadoPractica.Finalizada;

    /// <summary>
    /// Indica si el par origen-destino es una transicion de avance, es decir una
    /// de las que RN-05 permite ejecutar sin intervencion del Administrador.
    /// </summary>
    /// <param name="origen">Estado de partida.</param>
    /// <param name="destino">Estado al que se quiere llegar.</param>
    /// <returns>true si la transicion es un avance; false si es un retroceso.</returns>
    /// <remarks>
    /// Se expone para que la capa de Aplicacion pueda preguntar por la maquina de
    /// estados <b>sin provocarla</b>. Lo usa el Motor de Reglas al aplicar el
    /// estado por defecto de RN-06 (P17): un valor configurado que no fuera un
    /// avance desde Pendiente haria que <see cref="CambiarEstado"/> lanzara
    /// AutorizacionException y que un POST de calificacion respondiera 403, que
    /// seria absurdo. El Motor comprueba y se degrada; no atrapa la excepcion.
    ///
    /// La alternativa era duplicar el par Pendiente-EnCurso en Aplicacion. Se
    /// descarto: <see cref="TransicionesDeAvance"/> es la unica declaracion de
    /// RN-05 del sistema y debe seguir siendolo. <see cref="CambiarEstado"/>
    /// tambien consume este metodo, de modo que las dos lecturas de la tabla son
    /// literalmente la misma.
    /// </remarks>
    public static bool EsAvance(EstadoPractica origen, EstadoPractica destino) =>
        Array.Exists(TransicionesDeAvance,
            t => t.Origen == origen && t.Destino == destino);

    /// <summary>
    /// Cambia el estado de la practica respetando la maquina de estados de
    /// RN-05: la secuencia de avance es libre para el sistema, mientras que
    /// cualquier retroceso queda reservado al Administrador.
    /// </summary>
    /// <param name="nuevoEstado">Estado destino.</param>
    /// <param name="esAdministrador">true si quien ejecuta la operacion tiene rol Administrador.</param>
    /// <exception cref="ReglaDeDominioException">Si el estado destino coincide con el actual.</exception>
    /// <exception cref="AutorizacionException">Si la transicion es un retroceso y el solicitante no es Administrador.</exception>
    public void CambiarEstado(EstadoPractica nuevoEstado, bool esAdministrador)
    {
        if (nuevoEstado == Estado)
            throw new ReglaDeDominioException($"La practica ya se encuentra en estado {Estado}.", "RN-05");

        if (!EsAvance(Estado, nuevoEstado) && !esAdministrador)
            throw new AutorizacionException(
                "El retroceso de estado de una practica esta reservado al Administrador.");

        Estado = nuevoEstado;
    }

    /// <summary>
    /// Marca la practica como En riesgo. Es la accion que el Motor de Reglas
    /// ejecuta cuando una regla activa coincide (RN-09).
    /// </summary>
    /// <exception cref="ReglaDeDominioException">Si la practica ya esta En riesgo o esta Finalizada.</exception>
    public void MarcarEnRiesgo()
    {
        if (Estado == EstadoPractica.Finalizada)
            throw new ReglaDeDominioException(
                "Una practica finalizada no puede marcarse En riesgo.", "RN-09");

        CambiarEstado(EstadoPractica.EnRiesgo, esAdministrador: false);
    }

    /// <summary>Cierra la practica registrando su fecha de finalizacion.</summary>
    /// <param name="fechaFin">Fecha de cierre. No puede ser anterior a la de inicio.</param>
    /// <param name="esAdministrador">true si quien ejecuta la operacion tiene rol Administrador.</param>
    /// <exception cref="ReglaDeDominioException">Si el rango de fechas es invalido.</exception>
    public void Finalizar(DateOnly fechaFin, bool esAdministrador = false)
    {
        ValidarRangoDeFechas(FechaInicio, fechaFin);
        CambiarEstado(EstadoPractica.Finalizada, esAdministrador);
        FechaFin = fechaFin;
    }

    /// <summary>
    /// Reasigna instructor y aprendiz. RN-04 exige que la reasignacion sea una
    /// accion explicita del Administrador; la verificacion del rol ocurre en la
    /// capa de API y la de practica activa duplicada, en el caso de uso.
    /// </summary>
    /// <param name="instructorId">Nuevo instructor responsable.</param>
    /// <param name="aprendizId">Nuevo aprendiz titular.</param>
    /// <exception cref="ReglaDeDominioException">Si algun participante es invalido o si ambos coinciden.</exception>
    public void Reasignar(int instructorId, int aprendizId)
    {
        if (instructorId <= 0)
            throw new ReglaDeDominioException("La practica requiere un instructor asignado.", "RN-04");
        if (aprendizId <= 0)
            throw new ReglaDeDominioException("La practica requiere un aprendiz asignado.", "RN-04");
        if (instructorId == aprendizId)
            throw new ReglaDeDominioException("El instructor y el aprendiz no pueden ser el mismo usuario.", "RN-04");

        InstructorId = instructorId;
        AprendizId = aprendizId;
    }

    /// <summary>Cambia la modalidad y, con ella, la empresa receptora.</summary>
    /// <param name="modalidad">Nueva modalidad.</param>
    /// <param name="empresaId">Empresa receptora. Debe ser nula en Proyecto productivo y en Monitoria.</param>
    /// <exception cref="ReglaDeDominioException">Si la combinacion es incoherente.</exception>
    public void CambiarModalidad(ModalidadPractica modalidad, int? empresaId)
    {
        ValidarModalidadYEmpresa(modalidad, empresaId);
        Modalidad = modalidad;
        EmpresaId = empresaId;
    }

    /// <summary>
    /// Replica en el dominio la restriccion chk_practicas_empresa_modalidad del
    /// Script_DDL.sql: Proyecto productivo y Monitoria se desarrollan sin empresa
    /// receptora; las demas modalidades la exigen.
    /// </summary>
    /// <remarks>
    /// La monitoria transcurre dentro del propio centro de formacion, de modo que
    /// no solo no exige empresa: no admite ninguna. El criterio es una particion
    /// entre dos grupos de modalidades, igual que la restriccion de la base.
    /// </remarks>
    private static void ValidarModalidadYEmpresa(ModalidadPractica modalidad, int? empresaId)
    {
        var admiteEmpresa = modalidad is not (ModalidadPractica.ProyectoProductivo
                                           or ModalidadPractica.Monitoria);

        if (!admiteEmpresa && empresaId is not null)
            throw new ReglaDeDominioException(
                "Las modalidades Proyecto productivo y Monitoria no admiten empresa asociada.");

        if (admiteEmpresa && empresaId is null)
            throw new ReglaDeDominioException(
                "Esta modalidad requiere una empresa receptora asociada.");
    }

    /// <summary>
    /// Replica en el dominio la restriccion chk_practicas_fechas del
    /// Script_DDL.sql: la fecha de cierre nunca precede a la de inicio.
    /// </summary>
    private static void ValidarRangoDeFechas(DateOnly inicio, DateOnly? fin)
    {
        if (fin is not null && fin < inicio)
            throw new ReglaDeDominioException(
                "La fecha de finalizacion no puede ser anterior a la de inicio.");
    }
}
