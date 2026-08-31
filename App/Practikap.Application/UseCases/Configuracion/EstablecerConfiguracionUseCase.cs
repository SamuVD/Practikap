using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Configuracion;
using Practikap.Application.Validators.Configuracion;
using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

// El espacio de nombres de este archivo se llama igual que la entidad, que es el
// unico sitio del proyecto donde eso ocurre: las demas carpetas de UseCases van en
// plural y sus entidades en singular. Dentro de Practikap.Application.UseCases.
// Configuracion, el identificador Configuracion resuelve al espacio de nombres
// antes que al tipo, de modo que la entidad se importa con alias.
using EntradaDeConfiguracion = Practikap.Domain.Entities.Configuracion;

namespace Practikap.Application.UseCases.Configuracion;

/// <summary>
/// Establece el valor de una clave de configuracion, creando la entrada si no
/// existia. Reservado al Administrador (RF-09, CU-08, RN-08).
/// </summary>
/// <remarks>
/// <b>Es el unico caso de uso de M8 que escribe</b>, y el unico del modulo que usa
/// IContextoUsuario para algo mas que la barrera de rol: _contexto.UsuarioId es
/// quien queda registrado como responsable del valor (RF-09, P3).
///
/// Crea o actualiza, y <b>la diferencia es observable</b>: el resultado lleva la
/// marca con la que el controlador elige entre 201 y 200. Es la misma forma con la
/// que el POST de reportes distingue el caso de O8, aunque no puede usar su truco de
/// mirar el Id: ConfiguracionResponse no lo expone, porque la identidad publica de
/// una entrada es su clave.
///
/// <b>Quien decide si crea o actualiza es este archivo, no el repositorio</b> (P4).
/// El contrato del scaffolding declaraba un EstablecerAsync(clave, valor, ...) que
/// habria obligado a la Infraestructura a cargar-o-crear la entrada e invocar
/// Configuracion.Establecer, que es dominio llamado desde la Infraestructura: lo que
/// H28 descarto en M3, I9 aplico en M4, J7 extendio a M5, L8 a M6, N8 a M2 y O5 a M7.
///
/// El camino de actualizacion <b>no llama a ningun ActualizarAsync</b>, y no falta:
/// ObtenerPorClaveAsync devuelve la entrada rastreada y EF Core detecta el cambio
/// solo. Es la razon de que el contrato no declare ese metodo.
///
/// <b>La descripcion solo se escribe al crear.</b> Configuracion.Establecer no la
/// toca, de modo que un PUT sobre una entrada existente conserva la del catalogo con
/// que nacio. Describe lo que la clave significa para el sistema, y eso no depende
/// del valor que tenga hoy.
///
/// Los tres codigos se reparten con el criterio de N1, N2 y O19: <b>400</b> en el
/// validador para el valor vacio, que es forma; <b>422</b> aqui para la clave fuera
/// del catalogo y para el valor que la clave no admite, que son decisiones de
/// producto sobre columnas que el DDL declara VARCHAR libres.
/// </remarks>
public sealed class EstablecerConfiguracionUseCase
{
    private readonly IConfiguracionRepository _configuracionRepo;
    private readonly IContextoUsuario _contexto;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IValidator<EstablecerConfiguracionRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<EstablecerConfiguracionUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="configuracionRepo">Acceso al almacen clave/valor.</param>
    /// <param name="contexto">Identidad del solicitante (ADR-03).</param>
    /// <param name="unidadDeTrabajo">Punto de confirmacion (ADR-02).</param>
    /// <param name="validador">Validador de forma del DTO (RN-15).</param>
    /// <param name="mapeador">Proyeccion a DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public EstablecerConfiguracionUseCase(
        IConfiguracionRepository configuracionRepo,
        IContextoUsuario contexto,
        IUnidadDeTrabajo unidadDeTrabajo,
        IValidator<EstablecerConfiguracionRequest> validador,
        IMapper mapeador,
        ILogger<EstablecerConfiguracionUseCase> registro)
    {
        _configuracionRepo = configuracionRepo;
        _contexto = contexto;
        _unidadDeTrabajo = unidadDeTrabajo;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Aplica el valor a la clave, creando la entrada si hiciera falta.</summary>
    /// <param name="clave">Clave de configuracion, tal como viaja en la ruta.</param>
    /// <param name="request">Valor a establecer.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>La entrada resultante y la marca de si se creo.</returns>
    /// <exception cref="ValidationException">Si el valor viene vacio (400).</exception>
    /// <exception cref="AutorizacionException">Si el solicitante no tiene alcance Global (403).</exception>
    /// <exception cref="ReglaDeDominioException">
    /// Si la clave no pertenece al catalogo de P8, o si el valor no es de los que esa
    /// clave admite (422).
    /// </exception>
    public async Task<ResultadoDeConfiguracion> ExecuteAsync(
        string clave, EstablecerConfiguracionRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        if (_contexto.Alcance != AlcanceConsulta.Global)
            throw new AutorizacionException(
                "Solo el Administrador puede modificar la configuracion del sistema.");

        ReglasDeConfiguracion.ExigirClaveValida(clave);
        ReglasDeConfiguracion.ExigirValorValido(clave, request.Valor);

        var entrada = await _configuracionRepo.ObtenerPorClaveAsync(clave, ct);
        var creada = entrada is null;

        if (entrada is null)
        {
            entrada = new EntradaDeConfiguracion(
                clave,
                request.Valor,
                _contexto.UsuarioId,
                ReglasDeConfiguracion.DescripcionDe(clave));

            await _configuracionRepo.AgregarAsync(entrada, ct);
        }
        else
        {
            // El dominio se invoca desde aqui y no desde el repositorio (P4).
            entrada.Establecer(request.Valor, _contexto.UsuarioId);
        }

        await _unidadDeTrabajo.GuardarCambiosAsync(ct);

        _registro.LogInformation(
            "Configuracion {Clave} {Accion} por el administrador {AdministradorId} con el valor {Valor}.",
            entrada.Clave, creada ? "creada" : "actualizada", _contexto.UsuarioId, entrada.Valor);

        return new ResultadoDeConfiguracion(
            _mapeador.Map<ConfiguracionResponse>(entrada), creada);
    }
}

/// <summary>
/// Entrada de configuracion resultante y la marca de si el PUT la creo.
/// </summary>
/// <remarks>
/// Vive en el archivo del caso de uso y no en DTOs/, con el criterio de
/// ArchivoExportado: no es un contrato HTTP sino el par que el controlador necesita
/// para elegir entre 201 y 200. Lo que viaja por la red es
/// <see cref="ConfiguracionResponse"/> solo; la marca se gasta en el codigo de
/// respuesta y no aparece en el cuerpo.
/// </remarks>
/// <param name="Entrada">La entrada, con su valor ya aplicado.</param>
/// <param name="Creada">true si la entrada no existia y se creo; false si se actualizo.</param>
public sealed record ResultadoDeConfiguracion(ConfiguracionResponse Entrada, bool Creada);
