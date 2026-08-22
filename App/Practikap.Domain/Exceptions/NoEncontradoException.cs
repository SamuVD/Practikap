using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Practikap.Domain.Exceptions;

/// <summary>
/// El recurso solicitado no existe. El middleware la traduce a HTTP 404.
/// </summary>
public sealed class NoEncontradoException : DominioException
{
    /// <summary>Nombre del recurso buscado, tal como lo conoce el dominio.</summary>
    public string Recurso { get; }

    /// <summary>Identificador con el que se busco el recurso.</summary>
    public object? Identificador { get; }

    /// <summary>Crea la excepcion indicando recurso e identificador.</summary>
    /// <param name="recurso">Nombre del recurso, por ejemplo "Practica".</param>
    /// <param name="identificador">Valor con el que se intento localizarlo.</param>
    public NoEncontradoException(string recurso, object identificador)
        : base($"No se encontro {recurso} con identificador {identificador}.")
    {
        Recurso = recurso;
        Identificador = identificador;
    }

    /// <summary>Crea la excepcion con un mensaje libre.</summary>
    /// <param name="mensaje">Texto descriptivo del recurso ausente.</param>
    public NoEncontradoException(string mensaje) : base(mensaje)
    {
        Recurso = string.Empty;
        Identificador = null;
    }
}
