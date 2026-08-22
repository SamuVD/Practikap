using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Practikap.Domain.Enums;

namespace Practikap.Infrastructure.Persistence.Configurations;

/// <summary>
/// Traduccion entre los miembros de las enumeraciones del Dominio y los
/// literales exactos que el Script_DDL.sql declara en cada columna ENUM.
/// </summary>
/// <remarks>
/// Los identificadores de C# no admiten espacios ni tildes y varios literales
/// del DDL los tienen, de modo que HasConversion&lt;string&gt;() no sirve: escribiria
/// "EnCurso" donde MySQL espera "En curso". Se aplica un convertidor explicito a
/// las nueve enumeraciones persistidas, incluso a las que coinciden con su
/// nombre en C#, porque un criterio uniforme es mas facil de auditar que uno
/// mixto. AlcanceConsulta no aparece aqui: es un tipo de dominio que no se
/// persiste.
/// </remarks>
public static class ConvertidoresDeEnum
{
    /// <summary>usuarios.estado</summary>
    public static readonly ValueConverter<EstadoUsuario, string> ConvertidorEstadoUsuario =
        new(v => ATexto(v), v => AEstadoUsuario(v));

    /// <summary>tokens_revocados.motivo</summary>
    public static readonly ValueConverter<MotivoRevocacion, string> ConvertidorMotivoRevocacion =
        new(v => ATexto(v), v => AMotivoRevocacion(v));

    /// <summary>reglas.operador</summary>
    public static readonly ValueConverter<OperadorComparacion, string> ConvertidorOperadorComparacion =
        new(v => ATexto(v), v => AOperadorComparacion(v));

    /// <summary>auditoria.accion</summary>
    public static readonly ValueConverter<AccionAuditoria, string> ConvertidorAccionAuditoria =
        new(v => ATexto(v), v => AAccionAuditoria(v));

    /// <summary>auditoria.entidad_afectada (VARCHAR(50), no ENUM)</summary>
    public static readonly ValueConverter<EntidadAuditada, string> ConvertidorEntidadAuditada =
        new(v => ATexto(v), v => AEntidadAuditada(v));

    /// <summary>practicas.modalidad</summary>
    public static readonly ValueConverter<ModalidadPractica, string> ConvertidorModalidadPractica =
        new(v => ATexto(v), v => AModalidadPractica(v));

    /// <summary>practicas.estado</summary>
    public static readonly ValueConverter<EstadoPractica, string> ConvertidorEstadoPractica =
        new(v => ATexto(v), v => AEstadoPractica(v));

    /// <summary>notificaciones.tipo</summary>
    public static readonly ValueConverter<TipoNotificacion, string> ConvertidorTipoNotificacion =
        new(v => ATexto(v), v => ATipoNotificacion(v));

    /// <summary>reportes.tipo</summary>
    public static readonly ValueConverter<TipoReporte, string> ConvertidorTipoReporte =
        new(v => ATexto(v), v => ATipoReporte(v));

    // -----------------------------------------------------------------
    // Dominio -> MySQL
    // -----------------------------------------------------------------

    private static string ATexto(EstadoUsuario v) => v switch
    {
        EstadoUsuario.Activo => "Activo",
        EstadoUsuario.Inactivo => "Inactivo",
        _ => throw Desconocido(nameof(EstadoUsuario), v)
    };

    private static string ATexto(MotivoRevocacion v) => v switch
    {
        MotivoRevocacion.Logout => "Logout",
        MotivoRevocacion.CambioContrasena => "Cambio de contraseña",
        _ => throw Desconocido(nameof(MotivoRevocacion), v)
    };

    private static string ATexto(OperadorComparacion v) => v switch
    {
        OperadorComparacion.Mayor => ">",
        OperadorComparacion.MayorOIgual => ">=",
        OperadorComparacion.Menor => "<",
        OperadorComparacion.MenorOIgual => "<=",
        OperadorComparacion.Igual => "=",
        OperadorComparacion.Distinto => "!=",
        _ => throw Desconocido(nameof(OperadorComparacion), v)
    };

    private static string ATexto(AccionAuditoria v) => v switch
    {
        AccionAuditoria.Anulacion => "Anulacion",
        AccionAuditoria.RetrocesoEstado => "Retroceso_estado",
        AccionAuditoria.CambioRol => "Cambio_rol",
        AccionAuditoria.Reasignacion => "Reasignacion",
        AccionAuditoria.ConfiguracionRegla => "Configuracion_regla",
        AccionAuditoria.Otro => "Otro",
        _ => throw Desconocido(nameof(AccionAuditoria), v)
    };

    private static string ATexto(EntidadAuditada v) => v switch
    {
        EntidadAuditada.Usuarios => "usuarios",
        EntidadAuditada.Practicas => "practicas",
        EntidadAuditada.Seguimientos => "seguimientos",
        EntidadAuditada.Observaciones => "observaciones",
        EntidadAuditada.CalificacionesInstructor => "calificaciones_instructor",
        EntidadAuditada.CalificacionesAprendiz => "calificaciones_aprendiz",
        EntidadAuditada.Reglas => "reglas",
        EntidadAuditada.Configuracion => "configuracion",
        _ => throw Desconocido(nameof(EntidadAuditada), v)
    };

    private static string ATexto(ModalidadPractica v) => v switch
    {
        ModalidadPractica.ContratoDeAprendizaje => "Contrato de aprendizaje",
        ModalidadPractica.Pasantia => "Pasantía",
        ModalidadPractica.ProyectoProductivo => "Proyecto productivo",
        ModalidadPractica.Monitoria => "Monitoría",
        _ => throw Desconocido(nameof(ModalidadPractica), v)
    };

    private static string ATexto(EstadoPractica v) => v switch
    {
        EstadoPractica.Pendiente => "Pendiente",
        EstadoPractica.EnCurso => "En curso",
        EstadoPractica.Finalizada => "Finalizada",
        EstadoPractica.EnRiesgo => "En riesgo",
        _ => throw Desconocido(nameof(EstadoPractica), v)
    };

    private static string ATexto(TipoNotificacion v) => v switch
    {
        TipoNotificacion.Calificacion => "Calificacion",
        TipoNotificacion.Mensaje => "Mensaje",
        TipoNotificacion.Observacion => "Observacion",
        TipoNotificacion.Riesgo => "Riesgo",
        _ => throw Desconocido(nameof(TipoNotificacion), v)
    };

    private static string ATexto(TipoReporte v) => v switch
    {
        TipoReporte.Individual => "Individual",
        TipoReporte.Grupal => "Grupal",
        _ => throw Desconocido(nameof(TipoReporte), v)
    };

    // -----------------------------------------------------------------
    // MySQL -> Dominio
    // -----------------------------------------------------------------

    private static EstadoUsuario AEstadoUsuario(string v) => v switch
    {
        "Activo" => EstadoUsuario.Activo,
        "Inactivo" => EstadoUsuario.Inactivo,
        _ => throw Ilegible(nameof(EstadoUsuario), v)
    };

    private static MotivoRevocacion AMotivoRevocacion(string v) => v switch
    {
        "Logout" => MotivoRevocacion.Logout,
        "Cambio de contraseña" => MotivoRevocacion.CambioContrasena,
        _ => throw Ilegible(nameof(MotivoRevocacion), v)
    };

    private static OperadorComparacion AOperadorComparacion(string v) => v switch
    {
        ">" => OperadorComparacion.Mayor,
        ">=" => OperadorComparacion.MayorOIgual,
        "<" => OperadorComparacion.Menor,
        "<=" => OperadorComparacion.MenorOIgual,
        "=" => OperadorComparacion.Igual,
        "!=" => OperadorComparacion.Distinto,
        _ => throw Ilegible(nameof(OperadorComparacion), v)
    };

    private static AccionAuditoria AAccionAuditoria(string v) => v switch
    {
        "Anulacion" => AccionAuditoria.Anulacion,
        "Retroceso_estado" => AccionAuditoria.RetrocesoEstado,
        "Cambio_rol" => AccionAuditoria.CambioRol,
        "Reasignacion" => AccionAuditoria.Reasignacion,
        "Configuracion_regla" => AccionAuditoria.ConfiguracionRegla,
        "Otro" => AccionAuditoria.Otro,
        _ => throw Ilegible(nameof(AccionAuditoria), v)
    };

    private static EntidadAuditada AEntidadAuditada(string v) => v switch
    {
        "usuarios" => EntidadAuditada.Usuarios,
        "practicas" => EntidadAuditada.Practicas,
        "seguimientos" => EntidadAuditada.Seguimientos,
        "observaciones" => EntidadAuditada.Observaciones,
        "calificaciones_instructor" => EntidadAuditada.CalificacionesInstructor,
        "calificaciones_aprendiz" => EntidadAuditada.CalificacionesAprendiz,
        "reglas" => EntidadAuditada.Reglas,
        "configuracion" => EntidadAuditada.Configuracion,
        _ => throw Ilegible(nameof(EntidadAuditada), v)
    };

    private static ModalidadPractica AModalidadPractica(string v) => v switch
    {
        "Contrato de aprendizaje" => ModalidadPractica.ContratoDeAprendizaje,
        "Pasantía" => ModalidadPractica.Pasantia,
        "Proyecto productivo" => ModalidadPractica.ProyectoProductivo,
        "Monitoría" => ModalidadPractica.Monitoria,
        _ => throw Ilegible(nameof(ModalidadPractica), v)
    };

    private static EstadoPractica AEstadoPractica(string v) => v switch
    {
        "Pendiente" => EstadoPractica.Pendiente,
        "En curso" => EstadoPractica.EnCurso,
        "Finalizada" => EstadoPractica.Finalizada,
        "En riesgo" => EstadoPractica.EnRiesgo,
        _ => throw Ilegible(nameof(EstadoPractica), v)
    };

    private static TipoNotificacion ATipoNotificacion(string v) => v switch
    {
        "Calificacion" => TipoNotificacion.Calificacion,
        "Mensaje" => TipoNotificacion.Mensaje,
        "Observacion" => TipoNotificacion.Observacion,
        "Riesgo" => TipoNotificacion.Riesgo,
        _ => throw Ilegible(nameof(TipoNotificacion), v)
    };

    private static TipoReporte ATipoReporte(string v) => v switch
    {
        "Individual" => TipoReporte.Individual,
        "Grupal" => TipoReporte.Grupal,
        _ => throw Ilegible(nameof(TipoReporte), v)
    };

    // -----------------------------------------------------------------

    private static ArgumentOutOfRangeException Desconocido(string enumeracion, object valor) =>
        new(nameof(valor), valor,
            $"Miembro de {enumeracion} sin literal declarado en el Script_DDL.sql.");

    private static InvalidOperationException Ilegible(string enumeracion, string valor) =>
        new($"La base de datos devolvio '{valor}', que no corresponde a ningun miembro de {enumeracion}.");
}
