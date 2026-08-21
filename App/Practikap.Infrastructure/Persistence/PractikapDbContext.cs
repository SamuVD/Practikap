using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;

namespace Practikap.Infrastructure.Persistence;

/// <summary>
/// Contexto de EF Core 9 de Practikap. Expone las 17 entidades del
/// Dominio y delega todo el mapeo en las clases IEntityTypeConfiguration del
/// ensamblado, segun el Doc_Tecnico 5.6.
/// </summary>
/// <remarks>
/// Se registra con alcance Scoped, una instancia por peticion HTTP (ADR-02).
/// No sobrescribe SaveChangesAsync ni instala interceptores: la confirmacion la
/// ejecuta el caso de uso, y las marcas de tiempo las genera MySQL con
/// DEFAULT CURRENT_TIMESTAMP, nunca la aplicacion (RN-11).
/// </remarks>
public class PractikapDbContext : DbContext
{
    /// <summary>Crea el contexto con las opciones que inyecta el contenedor.</summary>
    /// <param name="options">Opciones de configuracion del proveedor.</param>
    public PractikapDbContext(DbContextOptions<PractikapDbContext> options)
        : base(options)
    {
    }

    // --- Nivel 1: plataforma global -----------------------------------

    /// <summary>Catalogo de roles. Tabla roles.</summary>
    public DbSet<Rol> Roles => Set<Rol>();

    /// <summary>Usuarios de la plataforma. Tabla usuarios.</summary>
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    /// <summary>Lista de revocacion de JWT. Tabla tokens_revocados.</summary>
    public DbSet<TokenRevocado> TokensRevocados => Set<TokenRevocado>();

    /// <summary>Reglas del Motor de Reglas Dinamicas. Tabla reglas.</summary>
    public DbSet<Regla> Reglas => Set<Regla>();

    /// <summary>Configuracion clave/valor del sistema. Tabla configuracion.</summary>
    public DbSet<Configuracion> Configuraciones => Set<Configuracion>();

    /// <summary>Bitacora de acciones sensibles. Tabla auditoria.</summary>
    public DbSet<RegistroAuditoria> Auditoria => Set<RegistroAuditoria>();

    // --- Nivel 2: contexto formativo ----------------------------------

    /// <summary>Programas de formacion. Tabla programas.</summary>
    public DbSet<Programa> Programas => Set<Programa>();

    /// <summary>Fichas de formacion. Tabla fichas.</summary>
    public DbSet<Ficha> Fichas => Set<Ficha>();

    /// <summary>Empresas receptoras. Tabla empresas.</summary>
    public DbSet<Empresa> Empresas => Set<Empresa>();

    // --- Nivel 2: practica e historial --------------------------------

    /// <summary>Practicas productivas. Tabla practicas.</summary>
    public DbSet<Practica> Practicas => Set<Practica>();

    /// <summary>Seguimientos de avance. Tabla seguimientos.</summary>
    public DbSet<Seguimiento> Seguimientos => Set<Seguimiento>();

    /// <summary>Observaciones sobre un seguimiento. Tabla observaciones.</summary>
    public DbSet<Observacion> Observaciones => Set<Observacion>();

    /// <summary>Calificaciones del instructor al aprendiz. Tabla calificaciones_instructor.</summary>
    public DbSet<CalificacionInstructor> CalificacionesInstructor => Set<CalificacionInstructor>();

    /// <summary>Calificaciones del aprendiz al instructor. Tabla calificaciones_aprendiz.</summary>
    public DbSet<CalificacionAprendiz> CalificacionesAprendiz => Set<CalificacionAprendiz>();

    // --- Nivel 2: comunicacion y reportes -----------------------------

    /// <summary>Mensajes entre participantes de una practica. Tabla mensajes.</summary>
    public DbSet<Mensaje> Mensajes => Set<Mensaje>();

    /// <summary>Notificaciones del sistema. Tabla notificaciones.</summary>
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();

    /// <summary>Reportes generados. Tabla reportes.</summary>
    public DbSet<Reporte> Reportes => Set<Reporte>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Juego de caracteres y colacion del Script_DDL.sql, aplicados a todo
        // el modelo para no repetirlos tabla por tabla.
        modelBuilder.HasCharSet("utf8mb4");
        modelBuilder.UseCollation("utf8mb4_0900_ai_ci");

        // La tabla puente reporte_practica no tiene clase propia: se declara
        // dentro de ReporteConfiguration como navegacion de salto.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PractikapDbContext).Assembly);
    }
}
