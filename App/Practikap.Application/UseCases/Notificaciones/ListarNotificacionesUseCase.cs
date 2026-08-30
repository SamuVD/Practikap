using AutoMapper;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Notificaciones;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Notificaciones;

/// <summary>
/// Devuelve las notificaciones del solicitante, con filtro opcional por las no
/// leidas (RF-07, CU-06, L1, L3).
/// </summary>
/// <remarks>
/// <b>No hay switch sobre AlcanceConsulta, y esa ausencia es L3.</b> Todos los
/// demas listados del sistema traducen los tres alcances de RN-13 a un metodo de
/// repositorio distinto; este no, porque no hay nada que traducir: una
/// notificacion tiene un unico destinatario y nadie lee la de otro. El
/// Administrador incluido, que es donde L3 diverge a proposito de K4: alli leia
/// cualquier hilo de mensajes con alcance de supervision, y aqui solo ve su propia
/// bandeja.
///
/// El identificador nunca llega de afuera. Sale de IContextoUsuario (ADR-03), de
/// modo que no existe forma de pedir la bandeja ajena: no hay parametro donde
/// nombrarla. Es la misma proteccion por forma que K2 le dio al receptor de un
/// mensaje.
///
/// De ahi que el endpoint no declare 403. No hay alcance ajeno que rechazar, y el
/// peor caso de un usuario sin notificaciones es una lista vacia.
///
/// Sin validador y sin confirmacion: es una consulta pura, con la misma forma que
/// ListarMensajesDePracticaUseCase. Las dos desviaciones respecto de las cuatro
/// lineas del Doc_Tecnico 5.2 son las mismas que ya tienen los demas casos de uso
/// de lectura del proyecto.
/// </remarks>
public sealed class ListarNotificacionesUseCase
{
    private readonly INotificacionRepository _notificacionRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IMapper _mapeador;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="notificacionRepo">Acceso a las notificaciones.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    public ListarNotificacionesUseCase(
        INotificacionRepository notificacionRepo,
        IContextoUsuario contexto,
        IMapper mapeador)
    {
        _notificacionRepo = notificacionRepo;
        _contexto = contexto;
        _mapeador = mapeador;
    }

    /// <summary>Devuelve la bandeja del solicitante.</summary>
    /// <param name="soloNoLeidas">true para devolver unicamente las pendientes de lectura.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Sus notificaciones, de la mas reciente a la mas antigua.</returns>
    public async Task<IReadOnlyList<NotificacionResponse>> ExecuteAsync(
        bool soloNoLeidas, CancellationToken ct)
    {
        var notificaciones = await _notificacionRepo.ListarPorUsuarioAsync(
            _contexto.UsuarioId, soloNoLeidas, ct);

        return _mapeador.Map<IReadOnlyList<NotificacionResponse>>(notificaciones);
    }
}
