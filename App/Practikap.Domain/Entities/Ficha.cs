using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Ficha de formacion. Entidad dependiente de <see cref="Programa"/> que
/// vincula la practica con el programa al que pertenece el aprendiz.
/// </summary>
public class Ficha
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Ficha() { }

    /// <summary>Crea una ficha de formacion.</summary>
    /// <param name="numeroFicha">Numero unico de la ficha, por ejemplo "3168939".</param>
    /// <param name="programaId">Programa al que pertenece la ficha.</param>
    /// <exception cref="ReglaDeDominioException">Si el numero o el programa son invalidos.</exception>
    public Ficha(string numeroFicha, int programaId)
    {
        if (string.IsNullOrWhiteSpace(numeroFicha))
            throw new ReglaDeDominioException("El numero de ficha es obligatorio.");
        if (programaId <= 0)
            throw new ReglaDeDominioException("La ficha debe pertenecer a un programa valido.");

        NumeroFicha = numeroFicha.Trim();
        ProgramaId = programaId;
    }

    /// <summary>Identificador. Columna fichas.id.</summary>
    public int Id { get; private set; }

    /// <summary>Numero unico de la ficha. Columna fichas.numero_ficha.</summary>
    public string NumeroFicha { get; private set; } = null!;

    /// <summary>Programa al que pertenece. Columna fichas.programa_id.</summary>
    public int ProgramaId { get; private set; }

    /// <summary>Programa de formacion asociado.</summary>
    public Programa Programa { get; private set; } = null!;
}
