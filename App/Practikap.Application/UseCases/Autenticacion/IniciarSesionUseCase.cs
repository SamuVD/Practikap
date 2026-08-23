using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Practikap.Application.Common;
using Practikap.Application.DTOs.Autenticacion;
using Practikap.Application.DTOs.Usuarios;
using Practikap.Domain.Exceptions;
using Practikap.Domain.Interfaces;

namespace Practikap.Application.UseCases.Autenticacion;

/// <summary>
/// Verifica credenciales y emite un JWT. Flujo principal de CU-01 (RF-01).
/// </summary>
public sealed class IniciarSesionUseCase
{
    /// <summary>
    /// Mensaje unico para correo inexistente y contrasena incorrecta. CU-01
    /// exige no revelar cual de los dos fallo, para impedir que un tercero
    /// descubra que correos estan registrados.
    /// </summary>
    private const string MensajeCredenciales = "Correo o contrasena incorrectos.";

    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IServicioDeHash _hasher;
    private readonly IGeneradorDeToken _generador;
    private readonly IValidator<LoginRequest> _validador;
    private readonly IMapper _mapeador;
    private readonly ILogger<IniciarSesionUseCase> _registro;

    /// <summary>Crea el caso de uso.</summary>
    /// <param name="usuarioRepo">Acceso a usuarios.</param>
    /// <param name="hasher">Verificacion de contrasenas (RNF-05).</param>
    /// <param name="generador">Emisor de tokens JWT.</param>
    /// <param name="validador">Validador de forma del DTO de entrada (RN-15).</param>
    /// <param name="mapeador">Proyeccion de la entidad al DTO de salida.</param>
    /// <param name="registro">Registro de eventos.</param>
    public IniciarSesionUseCase(
        IUsuarioRepository usuarioRepo,
        IServicioDeHash hasher,
        IGeneradorDeToken generador,
        IValidator<LoginRequest> validador,
        IMapper mapeador,
        ILogger<IniciarSesionUseCase> registro)
    {
        _usuarioRepo = usuarioRepo;
        _hasher = hasher;
        _generador = generador;
        _validador = validador;
        _mapeador = mapeador;
        _registro = registro;
    }

    /// <summary>Autentica al usuario y devuelve la sesion iniciada.</summary>
    /// <param name="request">Credenciales recibidas.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El JWT emitido y los datos del usuario autenticado.</returns>
    /// <exception cref="ValidationException">Si el DTO no supera la validacion de forma (400).</exception>
    /// <exception cref="CredencialesInvalidasException">Si las credenciales no son validas o la cuenta esta inactiva (401).</exception>
    public async Task<LoginResponse> ExecuteAsync(LoginRequest request, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(request, ct);

        var usuario = await _usuarioRepo.ObtenerPorCorreoAsync(request.Correo, ct);

        if (usuario is null)
        {
            // Se deriva un hash que se descarta, para que un correo inexistente
            // tarde lo mismo que uno existente con contrasena incorrecta. Sin
            // esto, la diferencia de tiempo delataria que correos estan
            // registrados, anulando el mensaje generico de arriba.
            _hasher.Hash(request.Contrasena);
            throw new CredencialesInvalidasException(MensajeCredenciales);
        }

        if (!_hasher.Verificar(request.Contrasena, usuario.ContrasenaHash))
            throw new CredencialesInvalidasException(MensajeCredenciales);

        if (!usuario.EstaActivo)
            throw new CredencialesInvalidasException(
                "La cuenta esta inactiva. Comuniquese con el administrador.");

        var emitido = _generador.Generar(usuario.Id, usuario.Correo, usuario.Rol.Nombre);

        // Se registra el identificador, nunca el correo, la contrasena ni el
        // token (Doc_Tecnico 5.11).
        _registro.LogInformation(
            "Inicio de sesion del usuario {UsuarioId} con rol {Rol}.",
            usuario.Id, usuario.Rol.Nombre);

        return new LoginResponse(
            emitido.Token,
            emitido.ExpiraEn,
            _mapeador.Map<UsuarioResponse>(usuario));
    }
}