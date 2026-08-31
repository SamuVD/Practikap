using Microsoft.EntityFrameworkCore;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Interfaces;
using Practikap.Infrastructure.Persistence;

namespace Practikap.Infrastructure.Repositories;

/// <summary>
/// Implementacion de <see cref="IAuditoriaRepository"/> sobre
/// <see cref="PractikapDbContext"/>. Modulo M8, bitacora de acciones sensibles.
/// </summary>
/// <remarks>
/// No invoca dominio, con el criterio de H28 y sus extensiones. RegistrarAsync
/// recibe el asiento ya construido; quien lo construye es el generador que aporta la
/// Ronda 2, y quien decide que una accion es auditable es cada caso de uso.
///
/// <b>Este es el unico repositorio del proyecto que recibe criterios de filtro</b>,
/// y diverge a proposito de H27, que se los saco a IPracticaRepository, y de O4, que
/// se los nego a IReporteRepository (P6). En aquellos dos casos habia un listado de
/// alcance previo del que colgarse y el universo se cuenta en cientos de filas. Aqui
/// no hay listado previo —la bitacora no se restringe por rol, porque a M8 solo
/// entra el Administrador (P3)— y la tabla crece con cada accion sensible del
/// sistema: filtrar en memoria significaria traer la bitacora entera en cada
/// consulta para descartar casi todo.
///
/// Sin Include, y no por omision: ADR-06 fija que RegistroAuditoria no declara
/// ninguna propiedad de navegacion. UsuarioId es una clave foranea desnuda y la
/// referencia al objeto afectado es polimorfica. Ningun Usuario puede entrar en el
/// grafo, de modo que H32 esta garantizada por la forma de la entidad y no por la
/// disciplina de la consulta.
/// </remarks>
internal sealed class AuditoriaRepository : IAuditoriaRepository
{
    private readonly PractikapDbContext _contexto;

    /// <summary>Crea el repositorio sobre el contexto de la peticion.</summary>
    /// <param name="contexto">Contexto de EF Core, con alcance Scoped (ADR-02).</param>
    public AuditoriaRepository(PractikapDbContext contexto) => _contexto = contexto;

    /// <inheritdoc />
    /// <remarks>
    /// No confirma (ADR-02). Es lo que permite que el asiento de una accion sensible
    /// y la accion misma caigan en la misma transaccion, con la misma forma que
    /// IGeneradorDeNotificaciones (L6) y IEvaluadorDeReglas (N11): si la accion se
    /// deshace, su rastro se deshace con ella y la bitacora no queda afirmando algo
    /// que no ocurrio.
    ///
    /// Tampoco escribe FechaRegistro. La columna esta mapeada como generada por la
    /// base con DEFAULT CURRENT_TIMESTAMP, de modo que la marca de tiempo del asiento
    /// la pone MySQL (RN-11).
    ///
    /// Ningun caso de uso lo invoca todavia: los once puntos de enganche son la
    /// Ronda 2 del paso 4.9.
    /// </remarks>
    public Task RegistrarAsync(RegistroAuditoria registro, CancellationToken ct)
    {
        _contexto.Auditoria.Add(registro);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Los cinco criterios se componen sobre el IQueryable <b>antes</b> de
    /// materializar, de modo que el WHERE completo viaja a MySQL. Un criterio nulo no
    /// agrega condicion, que es como se combinan con Y logico sin escribir las
    /// treinta y dos combinaciones a mano.
    ///
    /// Los dos filtros de enumerado comparan contra el miembro y no contra su
    /// literal: ConvertidoresDeEnum traduce EntidadAuditada y AccionAuditoria a
    /// "practicas" y "Retroceso_estado" dentro del SQL generado, de modo que la
    /// traduccion sigue siendo suya y este archivo no repite ni un literal del DDL.
    ///
    /// Tres de los cinco criterios los sostienen los indices que creo la migracion
    /// InicialPractikap: idx_auditoria_usuario para el actor e idx_auditoria_entidad
    /// para la entidad afectada.
    ///
    /// El orden es descendente por identificador, que en esta tabla equivale al
    /// cronologico inverso porque la clave es autoincremental y la fecha la pone la
    /// base en el mismo INSERT. Es el mismo criterio de ReporteRepository, y ordena
    /// por Id y no por FechaRegistro a proposito: dos asientos del mismo segundo
    /// quedarian empatados por fecha y el orden dejaria de ser determinista.
    /// </remarks>
    public async Task<IReadOnlyList<RegistroAuditoria>> ListarAsync(
        EntidadAuditada? entidadAfectada,
        AccionAuditoria? accion,
        int? usuarioId,
        DateTime? desde,
        DateTime? hasta,
        CancellationToken ct)
    {
        var consulta = _contexto.Auditoria.AsNoTracking();

        if (entidadAfectada is not null)
            consulta = consulta.Where(registro => registro.EntidadAfectada == entidadAfectada.Value);

        if (accion is not null)
            consulta = consulta.Where(registro => registro.Accion == accion.Value);

        if (usuarioId is not null)
            consulta = consulta.Where(registro => registro.UsuarioId == usuarioId.Value);

        // Los dos limites son inclusive, como declara el contrato.
        if (desde is not null)
            consulta = consulta.Where(registro => registro.FechaRegistro >= desde.Value);

        if (hasta is not null)
            consulta = consulta.Where(registro => registro.FechaRegistro <= hasta.Value);

        return await consulta
            .OrderByDescending(registro => registro.Id)
            .ToListAsync(ct);
    }
}
