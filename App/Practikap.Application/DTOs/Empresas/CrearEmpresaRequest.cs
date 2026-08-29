namespace Practikap.Application.DTOs.Empresas;

/// <summary>
/// Alta de una empresa receptora. Solo el Administrador puede enviarla (FA-26).
/// </summary>
/// <param name="RazonSocial">Razon social de la empresa.</param>
/// <param name="Nit">NIT de la empresa, unico en el sistema.</param>
/// <param name="JefeInmediatoNombre">Nombre del jefe inmediato. Opcional.</param>
/// <param name="JefeInmediatoCorreo">Correo del jefe inmediato. Opcional.</param>
/// <param name="JefeInmediatoTelefono">Telefono del jefe inmediato. Opcional.</param>
public sealed record CrearEmpresaRequest
(
    string RazonSocial,
    string Nit,
    string? JefeInmediatoNombre,
    string? JefeInmediatoCorreo,
    string? JefeInmediatoTelefono
);
