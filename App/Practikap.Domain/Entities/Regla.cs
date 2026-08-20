using Practikap.Domain.Enums;
using Practikap.Domain.Exceptions;

namespace Practikap.Domain.Entities;

/// <summary>
/// Regla configurable del Motor de Reglas Dinamicas. Raiz de agregado del
/// modulo M2 y componente diferenciador de Practikap: el Administrador la crea
/// y la activa sin modificar codigo ni desplegar de nuevo (RN-08).
/// </summary>
public class Regla
{
    /// <summary>Constructor sin parametros reservado a EF Core.</summary>
    private Regla() { }

    /// <summary>Crea una regla del Motor.</summary>
    /// <param name="nombre">Nombre descriptivo de la regla.</param>
    /// <param name="campoEvaluado">Campo del dominio que se evalua, por ejemplo "calificacion_acumulada".</param>
    /// <param name="operador">Operador relacional aplicado.</param>
    /// <param name="valorCondicion">Valor contra el que se compara.</param>
    /// <param name="umbral">Valor numerico de alerta asociado a la regla.</param>
    /// <param name="accionResultante">Estado o alerta que produce la coincidencia.</param>
    /// <param name="creadoPor">Administrador que la registra (RF-10).</param>
    /// <param name="prioridad">Orden de evaluacion. Menor valor se evalua primero (RN-07).</param>
    /// <exception cref="ReglaDeDominioException">Si algun dato obligatorio falta o es invalido.</exception>
    public Regla(string nombre, string campoEvaluado, OperadorComparacion operador,
                 decimal valorCondicion, decimal umbral, string accionResultante,
                 int creadoPor, int prioridad = 0)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeDominioException("El nombre de la regla es obligatorio.");
        if (string.IsNullOrWhiteSpace(campoEvaluado))
            throw new ReglaDeDominioException("El campo evaluado es obligatorio.");
        if (string.IsNullOrWhiteSpace(accionResultante))
            throw new ReglaDeDominioException("La accion resultante es obligatoria.");
        if (creadoPor <= 0)
            throw new ReglaDeDominioException("La regla debe tener un creador valido.");
        if (prioridad < 0)
            throw new ReglaDeDominioException("La prioridad no puede ser negativa.", "RN-07");

        Nombre = nombre.Trim();
        CampoEvaluado = campoEvaluado.Trim();
        Operador = operador;
        ValorCondicion = valorCondicion;
        Umbral = umbral;
        AccionResultante = accionResultante.Trim();
        CreadoPor = creadoPor;
        Prioridad = prioridad;
        Activa = true;
    }

    /// <summary>Identificador. Columna reglas.id.</summary>
    public int Id { get; private set; }

    /// <summary>Nombre descriptivo. Columna reglas.nombre.</summary>
    public string Nombre { get; private set; } = null!;

    /// <summary>Campo del dominio evaluado. Columna reglas.campo_evaluado.</summary>
    public string CampoEvaluado { get; private set; } = null!;

    /// <summary>Operador relacional aplicado. Columna reglas.operador.</summary>
    public OperadorComparacion Operador { get; private set; }

    /// <summary>Valor contra el que se compara. Columna reglas.valor_condicion.</summary>
    public decimal ValorCondicion { get; private set; }

    /// <summary>Valor numerico de alerta. Columna reglas.umbral.</summary>
    public decimal Umbral { get; private set; }

    /// <summary>Estado o alerta que produce la coincidencia. Columna reglas.accion_resultante.</summary>
    public string AccionResultante { get; private set; } = null!;

    /// <summary>Orden de evaluacion. Columna reglas.prioridad. Insumo de RN-07.</summary>
    public int Prioridad { get; private set; }

    /// <summary>Indica si la regla participa en las evaluaciones. Columna reglas.activa.</summary>
    public bool Activa { get; private set; }

    /// <summary>Administrador que la registro. Columna reglas.creado_por.</summary>
    public int CreadoPor { get; private set; }

    /// <summary>Fecha de alta. La genera MySQL con DEFAULT CURRENT_TIMESTAMP.</summary>
    public DateTime FechaCreacion { get; private set; }

    /// <summary>Fecha de la ultima modificacion. La genera MySQL con ON UPDATE CURRENT_TIMESTAMP.</summary>
    public DateTime FechaActualizacion { get; private set; }

    /// <summary>Administrador creador de la regla.</summary>
    public Usuario Creador { get; private set; } = null!;

    /// <summary>
    /// Evalua si un valor observado satisface la condicion de esta regla.
    /// Es una comparacion pura, sin efectos secundarios ni acceso a datos:
    /// la decision de que hacer con el resultado corresponde al Motor
    /// (Practikap.Domain.Rules) segun RN-06 y RN-07.
    /// </summary>
    /// <param name="valorObservado">Valor real medido sobre la practica.</param>
    /// <returns>true si la condicion se cumple; false en caso contrario.</returns>
    public bool SeCumple(decimal valorObservado) => Operador switch
    {
        OperadorComparacion.Mayor => valorObservado > ValorCondicion,
        OperadorComparacion.MayorOIgual => valorObservado >= ValorCondicion,
        OperadorComparacion.Menor => valorObservado < ValorCondicion,
        OperadorComparacion.MenorOIgual => valorObservado <= ValorCondicion,
        OperadorComparacion.Igual => valorObservado == ValorCondicion,
        OperadorComparacion.Distinto => valorObservado != ValorCondicion,
        _ => throw new ReglaDeDominioException("Operador de comparacion no soportado.", "RN-07")
    };

    /// <summary>Actualiza la definicion de la regla sin alterar su estado de activacion.</summary>
    /// <param name="nombre">Nombre descriptivo.</param>
    /// <param name="campoEvaluado">Campo del dominio que se evalua.</param>
    /// <param name="operador">Operador relacional aplicado.</param>
    /// <param name="valorCondicion">Valor contra el que se compara.</param>
    /// <param name="umbral">Valor numerico de alerta.</param>
    /// <param name="accionResultante">Estado o alerta que produce.</param>
    /// <param name="prioridad">Orden de evaluacion (RN-07).</param>
    /// <exception cref="ReglaDeDominioException">Si algun dato obligatorio falta o es invalido.</exception>
    public void Actualizar(string nombre, string campoEvaluado, OperadorComparacion operador,
                           decimal valorCondicion, decimal umbral, string accionResultante,
                           int prioridad)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ReglaDeDominioException("El nombre de la regla es obligatorio.");
        if (string.IsNullOrWhiteSpace(campoEvaluado))
            throw new ReglaDeDominioException("El campo evaluado es obligatorio.");
        if (string.IsNullOrWhiteSpace(accionResultante))
            throw new ReglaDeDominioException("La accion resultante es obligatoria.");
        if (prioridad < 0)
            throw new ReglaDeDominioException("La prioridad no puede ser negativa.", "RN-07");

        Nombre = nombre.Trim();
        CampoEvaluado = campoEvaluado.Trim();
        Operador = operador;
        ValorCondicion = valorCondicion;
        Umbral = umbral;
        AccionResultante = accionResultante.Trim();
        Prioridad = prioridad;
    }

    /// <summary>Incorpora la regla a las evaluaciones del Motor (RN-08).</summary>
    public void Activar() => Activa = true;

    /// <summary>Retira la regla de las evaluaciones sin eliminarla (RN-08).</summary>
    public void Desactivar() => Activa = false;
}
