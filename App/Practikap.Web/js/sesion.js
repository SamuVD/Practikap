/*
 * Practikap · Sesion del usuario.
 *
 * Una sola clave en localStorage, practikap.sesion, con la forma
 * { token, expiraEn, usuario }, donde usuario es el UsuarioResponse completo
 * que devolvio el login (Q6). El rol para pintar la pantalla sale de
 * usuario.rol y nunca de decodificar el token a mano.
 *
 * La expiracion es reactiva con guarda preventiva y sin temporizadores (Q7).
 * La guarda vive aca, en exigirSesion, y corta antes de disparar ningun fetch.
 * La mitad reactiva vive en api.js, que borra y redirige ante cualquier 401.
 *
 * El backend emite con ClockSkew = TimeSpan.Zero y 60 minutos exactos, y
 * expiraEn nace de DateTime.UtcNow, de modo que llega en UTC con sufijo Z y la
 * comparacion contra Date.now() es determinista.
 */

const CLAVE_SESION = 'practikap.sesion';

/**
 * Lee la sesion guardada. Devuelve null si no hay ninguna o si el contenido
 * quedo corrupto, que es lo mismo a efectos de la guarda.
 */
function leerSesion() {
    const crudo = window.localStorage.getItem(CLAVE_SESION);
    if (!crudo) return null;

    try {
        const sesion = JSON.parse(crudo);
        if (!sesion || !sesion.token || !sesion.usuario) return null;
        return sesion;
    } catch (error) {
        return null;
    }
}

/** Guarda la respuesta del login tal como llego del servidor. */
function guardarSesion(respuestaDeLogin) {
    window.localStorage.setItem(CLAVE_SESION, JSON.stringify({
        token: respuestaDeLogin.token,
        expiraEn: respuestaDeLogin.expiraEn,
        usuario: respuestaDeLogin.usuario
    }));
}

/** Borra la sesion. Es lo unico que hace: no redirige ni avisa a nadie. */
function borrarSesion() {
    window.localStorage.removeItem(CLAVE_SESION);
}

/** Token en curso, o null. Lo consume api.js para la cabecera Authorization. */
function tokenActual() {
    const sesion = leerSesion();
    return sesion ? sesion.token : null;
}

/** Usuario en curso, o null. */
function usuarioActual() {
    const sesion = leerSesion();
    return sesion ? sesion.usuario : null;
}

/**
 * Comprueba si la sesion ya vencio. Una fecha ilegible cuenta como vencida:
 * ante la duda se cierra, nunca se deja pasar.
 */
function sesionExpirada(sesion) {
    const vence = Date.parse(sesion.expiraEn);
    return Number.isNaN(vence) || vence <= Date.now();
}

/** Manda al login con el motivo que la pantalla va a mostrar. */
function irALogin(motivo) {
    window.location.replace('login.html?motivo=' + encodeURIComponent(motivo));
}

/**
 * Primera instruccion de toda pagina protegida. Devuelve el usuario si la
 * sesion sirve, o null despues de haber redirigido.
 *
 * El llamador tiene que cortar cuando reciba null: una redireccion no detiene
 * el script en curso, y seguir adelante dispararia las peticiones que la guarda
 * existe para evitar.
 *
 * Ocultar en la interfaz no es autorizar (Doc_Arquitectura 3.4). El servidor
 * sigue respondiendo 403 por su cuenta; esto es solo usabilidad.
 *
 * @param {string[]} [rolesPermitidos] Roles que pueden ver la pagina. Omitido o
 *                                     vacio, basta con estar autenticado.
 */
function exigirSesion(rolesPermitidos) {
    const sesion = leerSesion();

    if (!sesion) {
        irALogin('requerida');
        return null;
    }

    if (sesionExpirada(sesion)) {
        borrarSesion();
        irALogin('expirada');
        return null;
    }

    if (Array.isArray(rolesPermitidos) && rolesPermitidos.length > 0
        && !rolesPermitidos.includes(sesion.usuario.rol)) {
        window.location.replace('dashboard.html?motivo=sin-permiso');
        return null;
    }

    return sesion.usuario;
}

/**
 * Cierra la sesion. Llama al endpoint para que el token quede revocado (RN-03)
 * y borra localStorage pase lo que pase: si el POST falla, la sesion local se
 * cierra igual (Q8).
 */
async function cerrarSesion() {
    try {
        await pedir('POST', '/auth/logout');
    } catch (error) {
        // Un token ya expirado o ya revocado responde 401 y no hay nada que
        // salvar: el cierre local ocurre en el finally de todas formas.
    } finally {
        borrarSesion();
        window.location.replace('login.html?motivo=cerrada');
    }
}
