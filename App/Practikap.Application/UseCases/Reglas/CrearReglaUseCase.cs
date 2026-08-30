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
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<CrearReglaRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<CrearReglaUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="reglaRepo">Acceso a las reglas del Motor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public CrearReglaUseCase(
        IReglaRepository reglaRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<CrearReglaRequest> validador,
        IMapper mapeador,
        ILogger<CrearReglaUseCase> registro)
    {
        _reglaRepo = reglaRepo;
        _contexto = contexto;
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

        _registro.LogInformation(
            "Regla {ReglaId} creada por el administrador {AdministradorId}: "
            + "{CampoEvaluado} {Operador} {ValorCondicion} produce {AccionResultante}, prioridad {Prioridad}.",
            regla.Id, _contexto.UsuarioId, regla.CampoEvaluado, regla.Operador,
            regla.ValorCondicion, regla.AccionResultante, regla.Prioridad);

        return _mapeador.Map<ReglaResponse>(regla);
    }
}
