using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Reglas;
using Practikap.Application.Validators.Reglas;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Reglas;

/// <summary>
/// Reemplaza la definicion de una regla del Motor. Reservado al Administrador
/// (RF-10, CU-02, RN-08).
/// </summary>
/// <remarks>
/// <b>Practikap si expone PUT aqui, y es el unico lugar donde lo hace.</b> M5
/// decidio lo contrario en J3 y con razon: una calificacion es un registro
/// historico, y corregirla en el sitio borraria la traza que RN-12 exige conservar.
/// Una regla no es historia, es configuracion vigente. RN-08 pide exactamente esto:
/// que el Administrador ajuste un umbral, cambie un operador o reordene una
/// prioridad <b>sin modificar el codigo fuente ni desplegar de nuevo</b>. Obligarlo
/// a retirar la regla y crear otra dejaria la tabla llena de versiones muertas y
/// cambiaria el identificador al que apuntan las notificaciones ya emitidas
/// (notificaciones.regla_id, RN-09).
///
/// <b>El PUT no cambia la activacion.</b> Regla.Actualizar no toca Activa ni
/// CreadoPor, de modo que editar una regla retirada la deja retirada y editar una
/// activa la deja activa, con la definicion nueva ya rigiendo. Incorporarla o
/// retirarla es el PATCH, que es una decision distinta y tiene su propio endpoint.
///
/// Las listas blancas dan 422 y el operador da 400, con el mismo reparto que el
/// alta y por la misma razon (N1, N2).
///
/// Umbral se reescribe igual a ValorCondicion en cada edicion (N3): si no se
/// reescribiera, una regla editada conservaria el umbral del valor viejo y las dos
/// columnas dejarian de coincidir sin que nadie lo hubiera pedido.
/// </remarks>
public sealed class ActualizarReglaUseCase
{
    private readonly IReglaRepository _reglaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<ActualizarReglaRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<ActualizarReglaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="reglaRepo">Acceso a las reglas del Motor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public ActualizarReglaUseCase(
        IReglaRepository reglaRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<ActualizarReglaRequest> validador,
        IMapper mapeador,
        ILogger<ActualizarReglaUseCase> registro)
    {
        _reglaRepo = reglaRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica la definicion nueva a la regla.</summary>
    /// <param name="id">Regla a modificar.</param>
    /// <param name="request">Definicion nueva, completa.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La regla con su definicion ya actualizada.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion de forma (400).</exception>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="NoEncontradoException">Si la regla no existe (404).</exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si el campo evaluado o la accion resultante quedan fuera de sus listas
    /// blancas (422).
    /// </exception>
    public async Task<ReglaResponse> ExecuteAsync(
        int id, ActualizarReglaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede configurar el Motor de Reglas.");

        var regla = await _reglaRepo.ObtenerPorIdAsync(id, ct)
            ?? throw new NoEncontradoException("Regla", id);

        ReglasDeMotor.ExigirCampoValido(request.CampoEvaluado);
        ReglasDeMotor.ExigirAccionValida(request.AccionResultante);

        // El validador ya confirmo que el literal es uno de los seis (H31).
        var operador = Enum.Parse<OperadorComparacion>(request.Operador);

        regla.Actualizar(
            request.Nombre,
            request.CampoEvaluado,
            operador,
            request.ValorCondicion,
            umbral: request.ValorCondicion,
            request.AccionResultante,
            request.Prioridad);

        await _reglaRepo.ActualizarAsync(regla, ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Regla {ReglaId} redefinida por el administrador {AdministradorId}: "
            + "{CampoEvaluado} {Operador} {ValorCondicion} produce {AccionResultante}, "
            + "prioridad {Prioridad}. Sigue {Estado}.",
            regla.Id, _contexto.UsuarioId, regla.CampoEvaluado, regla.Operador,
            regla.ValorCondicion, regla.AccionResultante, regla.Prioridad,
            regla.Activa ? "activa" : "inactiva");

        return _mapeador.Map<ReglaResponse>(regla);
    }
}
