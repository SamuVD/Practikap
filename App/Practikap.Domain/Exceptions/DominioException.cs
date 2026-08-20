using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Practikap.Domain.Exceptions;

/// <summary>
/// Raiz de todas las excepciones de dominio de Practikap. Existe para que el
/// middleware global de errores pueda distinguir en una sola comprobacion un
/// fallo previsto del dominio de un fallo no controlado, que siempre es 500.
/// </summary>
/// <remarks>
/// El Dominio no conoce codigos HTTP: la traduccion a 404, 409, 422 o 403 la
/// hace el middleware de Practikap.API segun la tabla del Doc_Tecnico 5.9.
/// </remarks>
public abstract class DominioException : Exception
{
    /// <summary>Crea la excepcion con un mensaje descriptivo.</summary>
    /// <param name="mensaje">Texto apto para mostrar al usuario final.</param>
    protected DominioException(string mensaje) : base(mensaje) { }

    /// <summary>Crea la excepcion conservando la excepcion original.</summary>
    /// <param name="mensaje">Texto apto para mostrar al usuario final.</param>
    /// <param name="interna">Excepcion que origino el fallo.</param>
    protected DominioException(string mensaje, Exception interna) : base(mensaje, interna) { }
}
