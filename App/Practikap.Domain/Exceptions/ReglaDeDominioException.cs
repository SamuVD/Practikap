using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Practikap.Domain.Exceptions;

/// <summary>
/// Los datos son validos en formato pero incumplen una regla de negocio.
/// El middleware la traduce a HTTP 422.
/// </summary>
/// <remarks>
/// Es la excepcion que lanzan las invariantes de las entidades: transiciones
/// de estado no permitidas (RN-05), anulacion repetida (RN-12), rango de
/// calificacion fuera de 0.0 a 5.0 y coherencia entre modalidad y empresa.
/// </remarks>
public sealed class ReglaDeDominioException : DominioException
{
    /// <summary>Identificador de la regla de negocio incumplida, si se conoce.</summary>
    public string? Regla { get; }

    /// <summary>Crea la excepcion con el motivo del incumplimiento.</summary>
    /// <param name="mensaje">Descripcion de la regla incumplida.</param>
    public ReglaDeDominioException(string mensaje) : base(mensaje) { }

    /// <summary>Crea la excepcion citando la regla de negocio incumplida.</summary>
    /// <param name="mensaje">Descripcion del incumplimiento.</param>
    /// <param name="regla">Identificador canonico, por ejemplo "RN-05".</param>
    public ReglaDeDominioException(string mensaje, string regla) : base(mensaje)
    {
        Regla = regla;
    }
}
