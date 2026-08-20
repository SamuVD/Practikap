using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Practikap.Domain.Exceptions;

/// <summary>
/// Violacion de una restriccion de unicidad, como un correo o un NIT ya
/// registrado. El middleware la traduce a HTTP 409.
/// </summary>
public sealed class ConflictoException : DominioException
{
    /// <summary>Crea la excepcion con el motivo del conflicto.</summary>
    /// <param name="mensaje">Descripcion del valor duplicado.</param>
    public ConflictoException(string mensaje) : base(mensaje) { }
}
