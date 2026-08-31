using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Reglas;
using Practikap.Application.Validators.Reglas;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Reglas;

/// <summary>
/// Da de alta una regla del Motor de Reglas Dinamicas. Reservado al Administrador
/// (RF-10, CU-02, RN-08).
/// </summary>
/// <remarks>
/// Es la mitad de RN-08 que da nombre al modulo: una condicion nueva entra en
/// operacion sin tocar el codigo fuente ni desplegar de nuevo. La regla nace activa
/// —el constructor de Regla lo fija y no admite lo contrario— y desde la
/// confirmacion participa en toda evaluacion posterior del Motor.
///
/// El campo evaluado y la accion resultante se comprueban contra las listas blancas
/// de ReglasDeMotor y dan 422 (N1, N2). No se validan con FluentValidation a
/// proposito: el middleware traduce toda ValidationException a 400, y esos dos
/// codigos son los que las decisiones aprobadas fijan. El operador si viaja por el
/// validador y da 400, porque es un enumerado cerrado por el DDL.
///
/// Umbral no viaja en el DTO y se escribe igual a ValorCondicion (N3). La columna
/// es NOT NULL y se conserva; el DDL separo condicion y umbral porque el catalogo
/// conceptual los listaba como atributos distintos, pero en la practica la
/// condicion es una sola.
///
/// El creador sale del token y no del cuerpo (RF-10): aceptarlo de fuera permitiria
/// atribuir una regla a otra cuenta de Administrador.
/// </remarks>
public sealed class CrearReglaUseCase
{
    private readonly IReglaRepository _reglaRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IRegistradorDeAuditoria _auditor;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearReglaRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CrearReglaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="reglaRepo">Acceso a las reglas del Motor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="auditor">Bitacora de acciones sensibles (P12, P13).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CrearReglaUseCase(
        IReglaRepository reglaRepo,
        IContextoUsuario contexto,
        IRegistradorDeAuditoria auditor,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearReglaRequest> validador,
        IMapper mapeador,
        ILogger<CrearReglaUseCase> registro)
    {
        _reglaRepo = reglaRepo;
        _contexto = contexto;
        _auditor = auditor;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Registra la regla y la deja disponible para el Motor.</summary>
    /// <param name="request">Definicion de la regla a crear.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La regla creada, con su identificador y sus marcas de tiempo.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion de forma (400).</exception>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si el campo evaluado o la accion resultante quedan fuera de sus listas
    /// blancas (422).
    /// </exception>
    public async Task<ReglaResponse> ExecuteAsync(CrearReglaRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException("Solo el Administrador puede configurar el Motor de Reglas.");

        ReglasDeMotor.ExigirCampoValido(request.CampoEvaluado);
        ReglasDeMotor.ExigirAccionValida(request.AccionResultante);

        // El validador ya confirmo que el literal es uno de los seis (H31).
        var operador = Enum.Parse<OperadorComparacion>(request.Operador);

        var regla = new Regla(
            request.Nombre,
            request.CampoEvaluado,
            operador,
            request.ValorCondicion,
            umbral: request.ValorCondicion,
            request.AccionResultante,
            creadoPor: _contexto.UsuarioId,
            request.Prioridad);

        await _reglaRepo.AgregarAsync(regla, ct);

        // Hasta aqui regla.Id vale 0 y las dos fechas son el valor por defecto de
        // DateTime. La confirmacion asigna el primero y trae de vuelta las que
        // escribio MySQL.
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        // DESVIACION DOCUMENTADA DE P12, y de las dos del proyecto es la unica que
        // cuesta algo. La regla la impone el constructor de RegistroAuditoria, que
        // exige un entidad_id mayor que cero: hasta la linea de arriba no lo hay, y
        // el asiento no se puede componer antes. De modo que aqui hay dos
        // confirmaciones y no una, contra lo que ADR-02 promete en los otros diez
        // enganches.
        //
        // Se paga a sabiendas. La alternativa era no auditar el alta, y RN-08 es
        // justamente la regla que dice que el comportamiento de la plataforma se
        // configura sin desplegar: un alta de regla sin traza de quien la creo
        // vaciaria de sentido a la bitacora en el unico modulo donde mas importa.
        //
        // El riesgo real es acotado: si esta segunda confirmacion falla, la regla
        // queda creada sin asiento y la peticion responde 500. No es un fallo
        // silencioso, que es lo que P15 existe para impedir.
        await _auditor.PorConfiguracionDeReglaAsync(regla.Id, regla.Nombre, "Alta", ct);
        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Regla {ReglaId} creada por el administrador {AdministradorId}: "
            + "{CampoEvaluado} {Operador} {ValorCondicion} produce {AccionResultante}, prioridad {Prioridad}.",
            regla.Id, _contexto.UsuarioId, regla.CampoEvaluado, regla.Operador,
            regla.ValorCondicion, regla.AccionResultante, regla.Prioridad);

        return _mapeador.Map<ReglaResponse>(regla);
    }
}
