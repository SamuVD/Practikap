/*
 * Practikap · Enumerados y catalogos cerrados del backend.
 *
 * Todo enumerado se pinta como <select>, nunca como caja de texto (Q13): el
 * backend compara contra el nombre del miembro y un literal desconocido
 * devuelve 422.
 *
 * El valor es el literal exacto que viaja por el cable, con la capitalizacion
 * del miembro de C#. La etiqueta es lo que ve el usuario y lleva la ortografia
 * completa del espanol, tildes incluidas: nunca se envia.
 *
 * Estan declarados todos, aunque la Ronda 1 solo consuma EstadoUsuario. Las
 * rondas siguientes leen de aca y no vuelven a declarar ninguno.
 */

/** Practikap.Domain.Enums.EstadoUsuario. Lo consume gestion-usuarios. */
const EstadoUsuario = [
    { valor: 'Activo', etiqueta: 'Activo' },
    { valor: 'Inactivo', etiqueta: 'Inactivo' }
];

/** Practikap.Domain.Enums.EstadoPractica. */
const EstadoPractica = [
    { valor: 'Pendiente', etiqueta: 'Pendiente' },
    { valor: 'EnCurso', etiqueta: 'En curso' },
    { valor: 'Finalizada', etiqueta: 'Finalizada' },
    { valor: 'EnRiesgo', etiqueta: 'En riesgo' }
];

/** Practikap.Domain.Enums.ModalidadPractica. */
const ModalidadPractica = [
    { valor: 'ContratoDeAprendizaje', etiqueta: 'Contrato de aprendizaje' },
    { valor: 'Pasantia', etiqueta: 'Pasantía' },
    { valor: 'ProyectoProductivo', etiqueta: 'Proyecto productivo' },
    { valor: 'Monitoria', etiqueta: 'Monitoría' }
];

/** Practikap.Domain.Enums.OperadorComparacion. Condicion de una regla del Motor. */
const OperadorComparacion = [
    { valor: 'Mayor', etiqueta: 'Mayor que' },
    { valor: 'MayorOIgual', etiqueta: 'Mayor o igual que' },
    { valor: 'Menor', etiqueta: 'Menor que' },
    { valor: 'MenorOIgual', etiqueta: 'Menor o igual que' },
    { valor: 'Igual', etiqueta: 'Igual a' },
    { valor: 'Distinto', etiqueta: 'Distinto de' }
];

/** Practikap.Domain.Enums.TipoReporte. */
const TipoReporte = [
    { valor: 'Individual', etiqueta: 'Individual' },
    { valor: 'Grupal', etiqueta: 'Grupal' }
];

/** Practikap.Domain.Enums.TipoNotificacion. Son cinco: Administrativa la sumo M6. */
const TipoNotificacion = [
    { valor: 'Calificacion', etiqueta: 'Calificación' },
    { valor: 'Mensaje', etiqueta: 'Mensaje' },
    { valor: 'Observacion', etiqueta: 'Observación' },
    { valor: 'Riesgo', etiqueta: 'Riesgo' },
    { valor: 'Administrativa', etiqueta: 'Administrativa' }
];

/** Practikap.Domain.Enums.EntidadAuditada. Filtro de la bitacora. */
const EntidadAuditada = [
    { valor: 'Usuarios', etiqueta: 'Usuarios' },
    { valor: 'Practicas', etiqueta: 'Prácticas' },
    { valor: 'Seguimientos', etiqueta: 'Seguimientos' },
    { valor: 'Observaciones', etiqueta: 'Observaciones' },
    { valor: 'CalificacionesInstructor', etiqueta: 'Calificaciones del instructor' },
    { valor: 'CalificacionesAprendiz', etiqueta: 'Calificaciones del aprendiz' },
    { valor: 'Reglas', etiqueta: 'Reglas' },
    { valor: 'Configuracion', etiqueta: 'Configuración' }
];

/** Practikap.Domain.Enums.AccionAuditoria. Son seis. */
const AccionAuditoria = [
    { valor: 'Anulacion', etiqueta: 'Anulación' },
    { valor: 'RetrocesoEstado', etiqueta: 'Retroceso de estado' },
    { valor: 'CambioRol', etiqueta: 'Cambio de rol' },
    { valor: 'Reasignacion', etiqueta: 'Reasignación' },
    { valor: 'ConfiguracionRegla', etiqueta: 'Configuración de regla' },
    { valor: 'Otro', etiqueta: 'Otro' }
];

/*
 * Los tres que siguen no son enumerados de C# sino listas de cadenas, pero se
 * comportan igual: el backend los valida contra un catalogo cerrado y rechaza
 * con 422 cualquier literal que no este.
 */

/** ReglasDeMotor.Campos. Van en snake_case, a diferencia de los enumerados. */
const CampoDelMotor = [
    { valor: 'calificacion_acumulada', etiqueta: 'Calificación acumulada' },
    { valor: 'dias_sin_seguimiento', etiqueta: 'Días sin seguimiento' }
];

/** ReglasDeMotor.Acciones. */
const AccionDelMotor = [
    { valor: 'MarcarEnRiesgo', etiqueta: 'Marcar en riesgo' },
    { valor: 'NotificarInstructor', etiqueta: 'Notificar al instructor' },
    { valor: 'MarcarEnRiesgoYNotificar', etiqueta: 'Marcar en riesgo y notificar' }
];

/** ReglasDeConfiguracion.Catalogo. */
const ClaveDeConfiguracion = [
    { valor: 'estado_practica_por_defecto', etiqueta: 'Estado por defecto de una práctica' },
    { valor: 'dias_sin_seguimiento_alerta', etiqueta: 'Días sin seguimiento para alertar' }
];

/**
 * Devuelve la etiqueta legible de un literal. Si el valor no esta en el
 * catalogo se muestra tal cual, que es preferible a dejar la celda vacia.
 *
 * @param {Array<{valor: string, etiqueta: string}>} catalogo
 * @param {string} valor
 */
function etiquetaDe(catalogo, valor) {
    const entrada = catalogo.find(elemento => elemento.valor === valor);
    return entrada ? entrada.etiqueta : valor;
}
