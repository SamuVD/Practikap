using Practikap.Domain.Entities;

namespace Practikap.Domain.Rules;

/// <summary>
/// Motor de Reglas Dinamicas. Es el componente diferenciador de Practikap: decide
/// que regla configurada por el Administrador se aplica a un valor observado, sin
/// que ninguna condicion viva en codigo compilado (RF-10, CU-02, RN-06, RN-07,
/// RN-08).
/// </summary>
/// <remarks>
/// Servicio de dominio puro, tal como lo fija ADR-04: no accede a la base de
/// datos, no inyecta repositorios, no es asincrono y no tiene estado. Recibe por
/// parametro las reglas activas ya ordenadas y el valor a evaluar, y devuelve el
/// resultado. Cargar las reglas y persistir la consecuencia son responsabilidad
/// del caso de uso que lo invoca.
///
/// De ahi que sea una clase estatica y no se registre en el contenedor de
/// dependencias: no hay nada que inyectarle. Eso es tambien lo que lo deja
/// integramente verificable con datos en memoria, que es el criterio de
/// aceptacion de RNF-08 sobre el Dominio.
///
/// La comparacion en si no vive aqui sino en <see cref="Regla.SeCumple"/>, que es
/// su unica fuente. Este archivo aporta la seleccion —que regla gana— y no la
/// aritmetica.
/// </remarks>
public static class MotorDeReglas
{
    /// <summary>
    /// Selecciona la regla que se aplica a un valor observado.
    /// </summary>
    /// <param name="reglasActivas">
    /// Reglas activas, ya ordenadas por prioridad ascendente. Se recorren en el
    /// orden recibido: el Motor no ordena ni descarta las inactivas, porque no
    /// consulta la base de datos y no tiene como saber si la coleccion esta
    /// completa. Quien la entrega es
    /// <c>IReglaRepository.ListarActivasOrdenadasAsync</c>.
    /// </param>
    /// <param name="campoEvaluado">
    /// Campo del dominio sobre el que se pregunta. Las reglas configuradas para
    /// otro campo se descartan, de modo que una misma coleccion sirve para todas
    /// las evaluaciones de una peticion.
    /// </param>
    /// <param name="valorObservado">Valor real medido sobre la practica.</param>
    /// <returns>
    /// La primera regla del orden recibido que sea del campo pedido y cuya
    /// condicion se cumpla, o <c>null</c> si ninguna coincide.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Si <paramref name="reglasActivas"/> o <paramref name="campoEvaluado"/> son
    /// nulos.
    /// </exception>
    /// <remarks>
    /// <b>Devuelve una sola regla, nunca varias (RN-07).</b> El resultado es
    /// determinista: ante la misma coleccion y el mismo valor, siempre gana la
    /// misma regla. Las reglas no se acumulan sobre una misma condicion.
    ///
    /// <b>Un nulo no es un error.</b> Es el caso de que ninguna regla activa
    /// coincida, y el Motor no hace nada con el: no aplica estados por defecto ni
    /// consulta la tabla de configuracion. Que hacer con la ausencia de
    /// coincidencia es decision del caso de uso invocante.
    /// </remarks>
    public static Regla? Evaluar(
        IReadOnlyList<Regla> reglasActivas,
        string campoEvaluado,
        decimal valorObservado)
    {
        ArgumentNullException.ThrowIfNull(reglasActivas);
        ArgumentNullException.ThrowIfNull(campoEvaluado);

        // Recorrido explicito y no LINQ: el orden de evaluacion es el contrato de
        // RN-07 y conviene que se lea como tal. El primer acierto corta.
        foreach (var regla in reglasActivas)
        {
            if (!string.Equals(regla.CampoEvaluado, campoEvaluado, StringComparison.Ordinal))
                continue;

            if (regla.SeCumple(valorObservado))
                return regla;
        }

        return null;
    }
}
