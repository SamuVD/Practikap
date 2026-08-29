namespace Practikap.Application.DTOs.Empresas;

/// <summary>
/// Representacion de salida de una empresa receptora.
/// </summary>
/// <param name="Id">Identificador de la empresa.</param>
/// <param name="RazonSocial">Razon social de la empresa.</param>
/// <param name="Nit">NIT de la empresa.</param>
/// <param name="JefeInmediatoNombre">Nombre del jefe inmediato. Puede venir nulo.</param>
/// <param name="JefeInmediatoCorreo">Correo del jefe inmediato. Puede venir nulo.</param>
/// <param name="JefeInmediatoTelefono">Telefono del jefe inmediato. Puede venir nulo.</param>
public sealed record EmpresaResponse
(
    int Id,
    string RazonSocial,
    string Nit,
    string? JefeInmediatoNombre,
    string? JefeInmediatoCorreo,
    string? JefeInmediatoTelefono
);
