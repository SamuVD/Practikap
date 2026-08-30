using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Calificacion en direccion Aprendiz hacia Instructor: <b>la emite el Aprendiz
/// sobre el Instructor</b> de la practica. Entidad dependiente de
/// <see cref="Practica"/> y completamente independiente de su contraparte:
/// RN-10 establece que ninguna de las dos direcciones condiciona a la otra.
/// </summary>
/// <remarks>
/// La direccion queda escrita y no deducida del nombre de la clase (J1), porque
/// el nombre por si solo admite las dos lecturas: quien califica o a quien se
/// califica. Aqui el Aprendiz es el emisor. Su contraparte,
/// <see cref="CalificacionInstructor"/>, es la que emite el Instructor.
///
/// CU-05 y HU-07 exigen tablas separadas en base de datos, de ahi que existan
/// dos entidades de forma identica en lugar de una sola con un discriminador.
/// </remarks>
public class CalificacionAprendiz
{
    /// <summary>Valor minimo admitido para una calificacion.</summary>
    public const decimal ValorMinimo = 0.0m;

    /// <summary>Valor maximo admitido para una calificacion.</summary>
    public const decimal ValorMaximo = 5.0m;

    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private CalificacionAprendiz() { }

    /// <summary>Registra una calificacion sobre una practica.</summary>
    /// <param name="practicaId">Practica calificada.</param>
    /// <param name="valor">Valor entre 0.0 y 5.0.</param>
    /// <param name="comentario">Comentario cualitativo. Opcional.</param>
    /// <exception cref="ReglaDeDominioException">Si la practica es invalida o el valor esta fuera de rango.</exception>
    public CalificacionAprendiz(int practicaId, decimal valor, string? comentario = null)
    {
        if (practicaId <= 0)
            throw new ReglaDeDominioException("La calificacion debe pertenecer a una practica valida.");

        ValidarValor(valor);

        PracticaId = practicaId;
        Valor = valor;
        Comentario = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
        Anulado = false;
    }

    /// <summary>Identificador. Columna calificaciones_aprendiz.id.</summary>
    public int Id { get; private set; }

    /// <summary>Practica calificada. Columna calificaciones_aprendiz.practica_id.</summary>
    public int PracticaId { get; private set; }

    /// <summary>Valor de la calificacion, entre 0.0 y 5.0. Columna calificaciones_aprendiz.valor.</summary>
    public decimal Valor { get; private set; }

    /// <summary>Comentario cualitativo. Columna calificaciones_aprendiz.comentario.</summary>
    public string? Comentario { get; private set; }

    /// <summary>Momento del registro. La genera MySQL con DEFAULT CURRENT_TIMESTAMP (RN-11).</summary>
    public DateTime FechaRegistro { get; private set; }

    /// <summary>Marca de anulacion. Columna calificaciones_aprendiz.anulado.</summary>
    public bool Anulado { get; private set; }

    /// <summary>Administrador que anulo el registro. Columna calificaciones_aprendiz.anulado_por.</summary>
    public int? AnuladoPor { get; private set; }

    /// <summary>Practica a la que pertenece la calificacion.</summary>
    public Practica Practica { get; private set; } = null!;

    /// <summary>Indica si la calificacion cuenta para el promedio vigente.</summary>
    public bool EsVigente => !Anulado;

    /// <summary>
    /// Marca la calificacion como anulada. Unica modificacion permitida por
    /// RN-12 y reservada al Administrador.
    /// </summary>
    /// <param name="anuladoPorId">Administrador que ejecuta la anulacion.</param>
    /// <exception cref="ReglaDeDominioException">Si ya estaba anulada o el actor es invalido.</exception>
    public void Anular(int anuladoPorId)
    {
        if (anuladoPorId <= 0)
            throw new ReglaDeDominioException("La anulacion requiere un actor valido.", "RN-12");
        if (Anulado)
            throw new ReglaDeDominioException("La calificacion ya se encuentra anulada.", "RN-12");

        Anulado = true;
        AnuladoPor = anuladoPorId;
    }

    /// <summary>
    /// Replica en el dominio la restriccion chk_calificaciones_aprendiz_valor del
    /// Script_DDL.sql: el valor debe estar entre 0.0 y 5.0.
    /// </summary>
    private static void ValidarValor(decimal valor)
    {
        if (valor < ValorMinimo || valor > ValorMaximo)
            throw new ReglaDeDominioException(
                $"La calificacion debe estar entre {ValorMinimo:0.0} y {ValorMaximo:0.0}.");
    }
}
