using Microsoft.Extensions.Logging;
using Practikap.Application.Validators.Reglas;
using Practikap.Domain.Entities;
using Practikap.Domain.Enums;
using Practikap.Domain.Interfaces;
using Practikap.Domain.Rules;

namespace Practikap.Application.Common;

/// <summary>
/// Unica implementacion de <see cref="IEvaluadorDeReglas"/> (N11). Es el cableado
/// que convierte al Motor de Reglas en un componente vivo: carga las reglas que el
/// Administrador configuro, mide la practica, deja que
/// <see cref="MotorDeReglas"/> decida cual gana y persiste la consecuencia.
/// </summary>
/// <remarks>
/// No depende de IUnidadDeTrabajo, y esa ausencia es deliberada por el mismo
/// motivo que en <see cref="GeneradorDeNotificaciones"/>: es lo que hace imposible
/// que esta clase confirme por su cuenta y lo que deja el cambio de estado y su
/// notificacion dentro de la transaccion de la calificacion que los origino
/// (ADR-02).
///
/// Vive en Aplicacion y no en Infraestructura porque no toca EF Core: habla contra
/// contratos del Dominio, igual que cualquier caso de uso. Y no es estatica como
/// <c>AccesoALaPractica</c> porque tiene cinco colaboradores fijos, que conviene
/// inyectar una vez y no arrastrar por la firma de los dos puntos de enganche.
///
/// <b>Ninguna condicion vive en este archivo.</b> La aritmetica es
/// <c>Regla.SeCumple</c>, la seleccion es <c>MotorDeReglas.Evaluar</c> y los
/// literales admitidos son <see cref="ReglasDeMotor"/>. Lo que aporta esta clase es
/// la medicion —que valor real tiene cada campo— y la ejecucion —que hacer con la
/// regla ganadora—. Eso es lo que sostiene RN-08: cambiar el comportamiento de la
/// plataforma es un POST a /api/reglas, no un despliegue.
/// </remarks>
internal sealed class EvaluadorDeReglas : IEvaluadorDeReglas
{
    private readonly IReglaRepository _reglaRepo;
    private readonly ICalificacionInstructorRepository _calificacionRepo;
    private readonly ISeguimientoRepository _seguimientoRepo;
    private readonly IPracticaRepository _practicaRepo;
    private readonly IGeneradorDeNotificaciones _generador;
    private readonly ILogger<EvaluadorDeReglas> _registro;

    /// <summary>Crea el evaluador.</summary>
    /// <param name="reglaRepo">Origen de las reglas activas ya ordenadas (RN-07).</param>
    /// <param name="calificacionRepo">Serie de calificaciones del instructor, insumo de calificacion_acumulada.</param>
    /// <param name="seguimientoRepo">Fecha del ultimo seguimiento, insumo de dias_sin_seguimiento.</param>
    /// <param name="practicaRepo">Registro del cambio de estado de la practica.</param>
    /// <param name="generador">Emision de la notificacion de tipo Riesgo (RN-09, L6).</param>
    /// <param name="registro">Registro de eventos.</param>
    public EvaluadorDeReglas(
        IReglaRepository reglaRepo,
        ICalificacionInstructorRepository calificacionRepo,
        ISeguimientoRepository seguimientoRepo,
        IPracticaRepository practicaRepo,
        IGeneradorDeNotificaciones generador,
        ILogger<EvaluadorDeReglas> registro)
    {
        _reglaRepo = reglaRepo;
        _calificacionRepo = calificacionRepo;
        _seguimientoRepo = seguimientoRepo;
        _practicaRepo = practicaRepo;
        _generador = generador;
        _registro = registro;
    }

    /// <inheritdoc />
    public async Task PorCalificacionRegistradaAsync(
        Practica practica, decimal valorRegistrado, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(practica);

        // La calificacion pendiente se agrega a mano porque todavia no esta en la
        // base y ninguna consulta la veria (N15).
        var vigentes = await ValoresVigentesAsync(practica.Id, excluido: null, ct);
        vigentes.Add(valorRegistrado);

        await EvaluarAsync(practica, Promediar(vigentes), ct);
    }

    /// <inheritdoc />
    public async Task PorCalificacionAnuladaAsync(
        Practica practica, int calificacionAnuladaId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(practica);

        // La marca de anulacion tampoco esta confirmada, asi que la calificacion
        // se descarta por identificador en lugar de confiar en su bandera (N15).
        var vigentes = await ValoresVigentesAsync(practica.Id, calificacionAnuladaId, ct);

        await EvaluarAsync(practica, Promediar(vigentes), ct);
    }

    /// <summary>
    /// Nucleo comun de los dos enganches: elige la regla que gana y aplica su
    /// consecuencia.
    /// </summary>
    /// <param name="practica">Practica evaluada, rastreada por el caso de uso.</param>
    /// <param name="calificacionAcumulada">
    /// Promedio vigente ya corregido con el cambio pendiente, o null si la practica
    /// no tiene ninguna calificacion computable.
    /// </param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    private async Task EvaluarAsync(
        Practica practica, decimal? calificacionAcumulada, CancellationToken ct)
    {
        // N14, y cubre las tres salidas de un golpe. Si la practica ya esta En
        // riesgo no se reaplica la accion ni se vuelve a notificar: la alerta ya
        // esta puesta y repetirla en cada calificacion inundaria la bandeja del
        // instructor. Pendiente y Finalizada quedan fuera porque MarcarEnRiesgo
        // lanzaria sobre ellas, y no es tarea del Motor decidir que hacer con una
        // practica que todavia no arranco o que ya cerro.
        //
        // De aqui sale tambien la otra mitad de N14: el Motor solo sabe llamar a
        // MarcarEnRiesgo y no tiene ninguna ruta hacia CambiarEstado, de modo que
        // una practica no vuelve a En curso porque el promedio se recupere. Ese
        // retroceso es del Administrador (RN-05, H17).
        if (practica.Estado != EstadoPractica.EnCurso)
            return;

        var reglas = await _reglaRepo.ListarActivasOrdenadasAsync(ct);
        if (reglas.Count == 0)
            return;

        // N13. Se recorren los dos campos de la lista blanca y no solo el de la
        // calificacion, de modo que una regla de dias_sin_seguimiento tambien
        // pueda disparar. Un campo que ninguna regla activa nombre se saltea sin
        // medirlo, y el que si se nombra se mide una sola vez por evaluacion.
        Regla? ganadora = null;

        foreach (var campo in ReglasDeMotor.Campos)
        {
            if (!reglas.Any(regla => string.Equals(regla.CampoEvaluado, campo, StringComparison.Ordinal)))
                continue;

            var valor = await ValorObservadoAsync(campo, practica, calificacionAcumulada, ct);
            if (valor is null)
                continue;

            var candidata = MotorDeReglas.Evaluar(reglas, campo, valor.Value);
            if (candidata is not null && EsAnterior(candidata, ganadora))
                ganadora = candidata;
        }

        // N4. Si ninguna regla activa coincide no se hace nada: no se aplica un
        // estado por defecto ni se consulta la tabla de configuracion, que es de
        // M8. La divergencia con el flujo alternativo de CU-02 es deliberada.
        if (ganadora is null)
            return;

        await AplicarAsync(practica, ganadora, ct);
    }

    /// <summary>
    /// Mide el valor real de un campo de la lista blanca sobre una practica.
    /// </summary>
    /// <param name="campo">Literal de <see cref="ReglasDeMotor.Campos"/>.</param>
    /// <param name="practica">Practica medida.</param>
    /// <param name="calificacionAcumulada">Promedio ya calculado por el enganche.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>El valor observado, o null si el campo no es medible en esta practica.</returns>
    private async Task<decimal?> ValorObservadoAsync(
        string campo, Practica practica, decimal? calificacionAcumulada, CancellationToken ct)
    {
        switch (campo)
        {
            // Llega ya corregido con el cambio pendiente. Un null significa que no
            // hay ninguna calificacion computable, y entonces el campo no se
            // evalua: PromedioVigenteAsync devuelve cero en ese caso, y evaluarlo
            // haria que toda practica sin calificar coincidiera con una regla de
            // promedio bajo.
            case "calificacion_acumulada":
                return calificacionAcumulada;

            case "dias_sin_seguimiento":
                return await DiasSinSeguimientoAsync(practica, ct);

            default:
                // Inalcanzable mientras ReglasDeMotor.Campos y este switch
                // coincidan. Se anota en lugar de devolver null en silencio porque
                // el sintoma seria el mismo fallo silencioso contra el que
                // ExigirCampoValido protege: reglas guardadas sin queja que no
                // coinciden nunca con nada. Quien agregue un campo a la lista
                // blanca sin medirlo aqui lo ve en el registro.
                _registro.LogWarning(
                    "El campo '{Campo}' esta en la lista blanca pero el evaluador no sabe medirlo. Las reglas configuradas sobre el no coincidiran nunca.",
                    campo);
                return null;
        }
    }

    /// <summary>
    /// Dias transcurridos desde el ultimo seguimiento no anulado de la practica.
    /// </summary>
    /// <param name="practica">Practica medida.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Los dias transcurridos, nunca negativos.</returns>
    /// <remarks>
    /// N16. Si la practica no tiene ningun seguimiento se mide desde su fecha de
    /// inicio, y no se descarta el campo: la practica que nunca recibio un
    /// seguimiento es exactamente la que una regla de inactividad quiere atrapar, y
    /// dejarla fuera vaciaria la regla de su caso principal.
    ///
    /// El reloj es DateTime.Now y no UtcNow porque seguimientos.fecha_registro lo
    /// escribe MySQL con CURRENT_TIMESTAMP, que es hora local del servidor. Mezclar
    /// los dos relojes introduciria un desfase constante del huso.
    /// </remarks>
    private async Task<decimal?> DiasSinSeguimientoAsync(Practica practica, CancellationToken ct)
    {
        var ultimo = await _seguimientoRepo.FechaUltimoRegistroAsync(practica.Id, ct);

        var referencia = ultimo?.Date
            ?? practica.FechaInicio.ToDateTime(TimeOnly.MinValue).Date;

        var dias = (DateTime.Now.Date - referencia).TotalDays;

        return dias < 0 ? 0m : (decimal)dias;
    }

    /// <summary>
    /// Ejecuta la consecuencia de la regla ganadora. Es el unico lugar del sistema
    /// donde los tres literales de N2 se traducen en efectos.
    /// </summary>
    /// <param name="practica">Practica sobre la que se actua.</param>
    /// <param name="regla">Regla que coincidio.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    private async Task AplicarAsync(Practica practica, Regla regla, CancellationToken ct)
    {
        var marcar = regla.AccionResultante is "MarcarEnRiesgo" or "MarcarEnRiesgoYNotificar";
        var notificar = regla.AccionResultante is "NotificarInstructor" or "MarcarEnRiesgoYNotificar";

        if (!marcar && !notificar)
        {
            // Los validadores acotan la accion en el alta y en la edicion, asi que
            // llegar aqui significa que alguien escribio en MySQL por fuera de la
            // API. Se anota y se sigue: una regla mal escrita no puede tumbar el
            // registro de una calificacion legitima.
            _registro.LogWarning(
                "La regla {ReglaId} declara la accion '{Accion}', que el Motor no sabe ejecutar. No se aplico nada sobre la practica {PracticaId}.",
                regla.Id, regla.AccionResultante, practica.Id);
            return;
        }

        if (marcar)
        {
            practica.MarcarEnRiesgo();
            await _practicaRepo.ActualizarAsync(practica, ct);
        }

        if (notificar)
        {
            // RN-09. El destinatario es el instructor responsable, y la
            // notificacion sale con regla_id poblado a traves de
            // Notificacion.DesdeRegla.
            await _generador.PorRiesgoAsync(practica.InstructorId, practica.Id, regla.Id, ct);
        }

        // Sin GuardarCambiosAsync, y sin manera de invocarlo. Confirma el caso de
        // uso que produjo el evento, en una sola transaccion con el (N11, ADR-02).
        _registro.LogInformation(
            "La regla {ReglaId} coincidio sobre la practica {PracticaId} y aplico la accion {Accion}.",
            regla.Id, practica.Id, regla.AccionResultante);
    }

    /// <summary>
    /// Valores de las calificaciones del instructor que cuentan para el promedio.
    /// </summary>
    /// <param name="practicaId">Practica consultada.</param>
    /// <param name="excluido">Calificacion que el caso de uso acaba de anular, si la hay.</param>
    /// <param name="ct">Token de cancelacion de la solicitud.</param>
    /// <returns>Los valores vigentes, en una lista que el llamador puede completar.</returns>
    /// <remarks>
    /// Se lista y se promedia en memoria en lugar de usar PromedioVigenteAsync
    /// porque el enganche necesita corregir el resultado con un cambio que la base
    /// todavia no tiene (N15). El redondeo replica el de aquel metodo para que el
    /// numero que el Motor evalua sea el mismo que GET /api/calificaciones informa.
    /// </remarks>
    private async Task<List<decimal>> ValoresVigentesAsync(
        int practicaId, int? excluido, CancellationToken ct)
    {
        var calificaciones = await _calificacionRepo.ListarPorPracticaAsync(practicaId, ct);

        return calificaciones
            .Where(calificacion => !calificacion.Anulado && calificacion.Id != excluido)
            .Select(calificacion => calificacion.Valor)
            .ToList();
    }

    /// <summary>Promedia los valores vigentes con el redondeo de M5.</summary>
    /// <param name="valores">Valores que cuentan.</param>
    /// <returns>El promedio, o null si no hay ninguno.</returns>
    private static decimal? Promediar(List<decimal> valores) =>
        valores.Count == 0
            ? null
            : Math.Round(valores.Sum() / valores.Count, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Indica si una candidata precede a la mejor encontrada hasta ahora.
    /// </summary>
    /// <param name="candidata">Regla que coincidio en el campo en curso.</param>
    /// <param name="ganadora">Mejor regla de los campos ya recorridos, si la hay.</param>
    /// <returns>true si la candidata debe reemplazar a la ganadora.</returns>
    /// <remarks>
    /// RN-07 exige una sola regla y un resultado determinista.
    /// <c>MotorDeReglas.Evaluar</c> ya lo garantiza dentro de un campo, porque
    /// recorre la coleccion en el orden recibido y corta en el primer acierto.
    /// Entre campos hace falta este desempate, que reproduce el mismo criterio de
    /// <c>ListarActivasOrdenadasAsync</c>: prioridad ascendente y, como prioridad no
    /// es unica, el identificador.
    /// </remarks>
    private static bool EsAnterior(Regla candidata, Regla? ganadora) =>
        ganadora is null
        || candidata.Prioridad < ganadora.Prioridad
        || (candidata.Prioridad == ganadora.Prioridad && candidata.Id < ganadora.Id);
}
