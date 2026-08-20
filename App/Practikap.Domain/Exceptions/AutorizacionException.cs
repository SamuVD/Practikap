using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Practikap.Domain.Exceptions;

/// <summary>
/// El solicitante esta autenticado pero no puede operar sobre ese recurso.
/// Implementa el rechazo exigido por RN-13. El middleware la traduce a HTTP 403.
/// </summary>
public sealed class AutorizacionException : DominioException
{
    /// <summary>Crea la excepcion con el motivo del rechazo.</summary>
    /// <param name="mensaje">Descripcion del permiso faltante, sin revelar datos ajenos.</param>
    public AutorizacionException(string mensaje) : base(mensaje) { }
}
