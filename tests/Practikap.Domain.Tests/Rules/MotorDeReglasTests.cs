using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Rules;

namespace Practikap.Domain.Tests.Rules;

/// <summary>
/// Pruebas del Motor de Reglas Dinamicas. Cubren el determinismo de RN-07, los
/// seis operadores de la columna reglas.operador y el caso de que ninguna regla
/// coincida.
/// </summary>
/// <remarks>
/// Sin dependencias externas de ningun tipo, que es lo que el Doc_Tecnico 5.12 pide
/// de este proyecto y lo que ADR-04 hizo posible al dejar el Motor sin acceso a
/// datos: todas las reglas se construyen en memoria.
///
/// Las reglas se identifican por su nombre y no por su Id. Regla.Id lo asigna
/// MySQL y aqui vale cero en todas: no hay base de datos, y no la necesitamos.
///
/// Los valores decimales viajan como double en los InlineData porque decimal no es
/// un tipo admitido en argumentos de atributo de C#. Los literales elegidos son
/// todos exactos en binario, de modo que la conversion no introduce error.
/// </remarks>
public class MotorDeReglasTests
{
    private const string Calificacion = "calificacion_acumulada";
    private const string DiasSinSeguimiento = "dias_sin_seguimiento";

    /// <summary>Construye una regla en memoria, sin base de datos.</summary>
    private static Regla Crear(
        string nombre,
        OperadorComparacion operador,
        decimal valorCondicion,
        int prioridad = 0,
        string campoEvaluado = Calificacion) =>
        new(nombre,
            campoEvaluado,
            operador,
            valorCondicion,
            umbral: valorCondicion,
            "MarcarEnRiesgo",
            creadoPor: 1,
            prioridad);

    // -----------------------------------------------------------------
    // Los seis operadores de la columna reglas.operador.
    // -----------------------------------------------------------------

    /// <summary>
    /// Cada uno de los seis operadores decide la coincidencia como su simbolo, en
    /// los dos sentidos. Los casos limite —el valor exactamente igual a la
    /// condicion— son los que separan Mayor de MayorOIgual y Menor de MenorOIgual.
    /// </summary>
    [Theory]
    // Mayor: solo por encima.
    [InlineData(OperadorComparacion.Mayor, 3.5, 4.0, true)]
    [InlineData(OperadorComparacion.Mayor, 3.5, 3.5, false)]
    [InlineData(OperadorComparacion.Mayor, 3.5, 3.0, false)]
    // MayorOIgual: por encima y el limite.
    [InlineData(OperadorComparacion.MayorOIgual, 3.5, 4.0, true)]
    [InlineData(OperadorComparacion.MayorOIgual, 3.5, 3.5, true)]
    [InlineData(OperadorComparacion.MayorOIgual, 3.5, 3.0, false)]
    // Menor: solo por debajo. Es el operador del ejemplo canonico de RN-09.
    [InlineData(OperadorComparacion.Menor, 3.5, 3.0, true)]
    [InlineData(OperadorComparacion.Menor, 3.5, 3.5, false)]
    [InlineData(OperadorComparacion.Menor, 3.5, 4.0, false)]
    // MenorOIgual: por debajo y el limite.
    [InlineData(OperadorComparacion.MenorOIgual, 3.5, 3.0, true)]
    [InlineData(OperadorComparacion.MenorOIgual, 3.5, 3.5, true)]
    [InlineData(OperadorComparacion.MenorOIgual, 3.5, 4.0, false)]
    // Igual.
    [InlineData(OperadorComparacion.Igual, 3.5, 3.5, true)]
    [InlineData(OperadorComparacion.Igual, 3.5, 3.51, false)]
    // Distinto.
    [InlineData(OperadorComparacion.Distinto, 3.5, 3.51, true)]
    [InlineData(OperadorComparacion.Distinto, 3.5, 3.5, false)]
    public void Evaluar_AplicaCadaOperadorComoSuSimbolo(
        OperadorComparacion operador, double valorCondicion, double valorObservado, bool coincide)
    {
        var regla = Crear("unica", operador, (decimal)valorCondicion);

        var resultado = MotorDeReglas.Evaluar([regla], Calificacion, (decimal)valorObservado);

        if (coincide)
            Assert.Same(regla, resultado);
        else
            Assert.Null(resultado);
    }

    /// <summary>Los seis miembros del enumerado estan cubiertos por la teoria de arriba.</summary>
    /// <remarks>
    /// Es la guarda de la cobertura: si alguien agrega un septimo operador al
    /// enumerado y al ENUM del DDL, esta prueba falla y obliga a extender la teoria
    /// en lugar de dejar el operador nuevo sin ejercitar.
    /// </remarks>
    [Fact]
    public void Enumerado_DeclaraExactamenteLosSeisOperadoresProbados()
    {
        Assert.Equal(6, Enum.GetValues<OperadorComparacion>().Length);
    }

    // -----------------------------------------------------------------
    // RN-07: una sola regla aplica, por orden de prioridad.
    // -----------------------------------------------------------------

    /// <summary>
    /// Con dos reglas que coinciden las dos, gana la de menor prioridad. Es RN-07:
    /// «el motor aplica la primera regla activa que coincida en orden de prioridad»,
    /// y las reglas no se acumulan sobre una misma condicion.
    /// </summary>
    [Fact]
    public void Evaluar_ConDosCoincidencias_DevuelveLaDeMenorPrioridad()
    {
        // Tal como las entrega IReglaRepository.ListarActivasOrdenadasAsync.
        var prioritaria = Crear("prioritaria", OperadorComparacion.Menor, 4.0m, prioridad: 1);
        var secundaria = Crear("secundaria", OperadorComparacion.Menor, 3.5m, prioridad: 2);

        var resultado = MotorDeReglas.Evaluar(
            [prioritaria, secundaria], Calificacion, 3.0m);

        Assert.Same(prioritaria, resultado);
    }

    /// <summary>
    /// Con prioridades iguales gana la primera de la coleccion recibida, que es la
    /// de menor identificador por el desempate de ListarActivasOrdenadasAsync.
    /// </summary>
    [Fact]
    public void Evaluar_ConPrioridadesIguales_DevuelveLaPrimeraDeLaColeccion()
    {
        var antigua = Crear("antigua", OperadorComparacion.Menor, 4.0m, prioridad: 5);
        var reciente = Crear("reciente", OperadorComparacion.Menor, 4.0m, prioridad: 5);

        var resultado = MotorDeReglas.Evaluar([antigua, reciente], Calificacion, 3.0m);

        Assert.Same(antigua, resultado);
    }

    /// <summary>
    /// El Motor respeta el orden que recibe y no reordena por Prioridad. Es la
    /// division de trabajo de ADR-04: ordenar es de la consulta, seleccionar es del
    /// Motor.
    /// </summary>
    /// <remarks>
    /// Se prueba a proposito con la coleccion desordenada. Si el Motor reordenara
    /// por su cuenta duplicaria una responsabilidad que ya vive en el indice
    /// idx_reglas_activa_prioridad, y el resultado dejaria de ser predecible desde
    /// la consulta.
    /// </remarks>
    [Fact]
    public void Evaluar_RespetaElOrdenRecibidoYNoReordenaPorPrioridad()
    {
        var prioridadAlta = Crear("prioridad-alta", OperadorComparacion.Menor, 4.0m, prioridad: 9);
        var prioridadBaja = Crear("prioridad-baja", OperadorComparacion.Menor, 4.0m, prioridad: 1);

        // Coleccion deliberadamente desordenada respecto de Prioridad.
        var resultado = MotorDeReglas.Evaluar(
            [prioridadAlta, prioridadBaja], Calificacion, 3.0m);

        Assert.Same(prioridadAlta, resultado);
    }

    /// <summary>
    /// La misma entrada devuelve siempre la misma regla. Es el requisito de
    /// «resultado determinista y reproducible» de RN-07.
    /// </summary>
    [Fact]
    public void Evaluar_ConLaMismaEntrada_EsDeterminista()
    {
        var reglas = new[]
        {
            Crear("primera", OperadorComparacion.Menor, 4.0m, prioridad: 1),
            Crear("segunda", OperadorComparacion.Menor, 4.5m, prioridad: 2),
            Crear("tercera", OperadorComparacion.MenorOIgual, 3.0m, prioridad: 3)
        };

        var primera = MotorDeReglas.Evaluar(reglas, Calificacion, 3.0m);
        var segunda = MotorDeReglas.Evaluar(reglas, Calificacion, 3.0m);
        var tercera = MotorDeReglas.Evaluar(reglas, Calificacion, 3.0m);

        Assert.Same(primera, segunda);
        Assert.Same(segunda, tercera);
        Assert.Same(reglas[0], primera);
    }

    // -----------------------------------------------------------------
    // N4: si ninguna regla activa coincide, no se hace nada.
    // -----------------------------------------------------------------

    /// <summary>
    /// Sin coincidencias devuelve null. El Motor no aplica estados por defecto ni
    /// consulta la tabla de configuracion: que hacer con la ausencia es decision del
    /// invocante (N4).
    /// </summary>
    [Fact]
    public void Evaluar_SinNingunaCoincidencia_DevuelveNulo()
    {
        var reglas = new[]
        {
            Crear("bajo-tres-cinco", OperadorComparacion.Menor, 3.5m, prioridad: 1),
            Crear("igual-a-cero", OperadorComparacion.Igual, 0m, prioridad: 2)
        };

        var resultado = MotorDeReglas.Evaluar(reglas, Calificacion, 4.8m);

        Assert.Null(resultado);
    }

    /// <summary>
    /// Sin reglas activas devuelve null. Es el estado del sistema recien instalado,
    /// antes de que el Administrador configure la primera regla.
    /// </summary>
    [Fact]
    public void Evaluar_SinReglas_DevuelveNulo()
    {
        var resultado = MotorDeReglas.Evaluar([], Calificacion, 3.0m);

        Assert.Null(resultado);
    }

    // -----------------------------------------------------------------
    // Filtro por campo evaluado.
    // -----------------------------------------------------------------

    /// <summary>
    /// Una regla de otro campo se descarta aunque su condicion se cumpliera con el
    /// valor recibido. Sin este filtro, una regla sobre dias sin seguimiento
    /// responderia a una calificacion y produciria una alerta sin sentido.
    /// </summary>
    [Fact]
    public void Evaluar_DescartaLasReglasDeOtroCampo()
    {
        var otroCampo = Crear(
            "dias-sin-seguimiento", OperadorComparacion.Menor, 4.0m,
            prioridad: 1, campoEvaluado: DiasSinSeguimiento);

        var resultado = MotorDeReglas.Evaluar([otroCampo], Calificacion, 3.0m);

        Assert.Null(resultado);
    }

    /// <summary>
    /// Con reglas de los dos campos mezcladas, gana la primera del campo
    /// consultado, aunque una de otro campo la preceda en la coleccion. Es lo que
    /// permite cargar las reglas una sola vez por peticion y preguntar por cada
    /// campo sin volver a la base.
    /// </summary>
    [Fact]
    public void Evaluar_ConCamposMezclados_DevuelveLaPrimeraDelCampoConsultado()
    {
        var estorbo = Crear(
            "estorbo", OperadorComparacion.Menor, 4.0m,
            prioridad: 1, campoEvaluado: DiasSinSeguimiento);
        var buscada = Crear(
            "buscada", OperadorComparacion.Menor, 4.0m,
            prioridad: 2, campoEvaluado: Calificacion);

        var resultado = MotorDeReglas.Evaluar([estorbo, buscada], Calificacion, 3.0m);

        Assert.Same(buscada, resultado);
    }

    /// <summary>La comparacion del campo es ordinal y distingue mayusculas.</summary>
    [Fact]
    public void Evaluar_ComparaElCampoDeFormaOrdinal()
    {
        var regla = Crear("unica", OperadorComparacion.Menor, 4.0m);

        var resultado = MotorDeReglas.Evaluar([regla], "Calificacion_Acumulada", 3.0m);

        Assert.Null(resultado);
    }

    // -----------------------------------------------------------------
    // Guardas de argumento.
    // -----------------------------------------------------------------

    /// <summary>La coleccion de reglas es obligatoria.</summary>
    [Fact]
    public void Evaluar_SinColeccion_Falla()
    {
        Assert.Throws<ArgumentNullException>(
            () => MotorDeReglas.Evaluar(null!, Calificacion, 3.0m));
    }

    /// <summary>El campo consultado es obligatorio.</summary>
    [Fact]
    public void Evaluar_SinCampo_Falla()
    {
        Assert.Throws<ArgumentNullException>(
            () => MotorDeReglas.Evaluar([], null!, 3.0m));
    }
}
