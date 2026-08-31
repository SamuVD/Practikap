/*
 * Practikap · Cliente HTTP.
 *
 * Unico punto del frontend que llama a fetch. Ninguna pagina invoca fetch ni
 * res.json() por su cuenta (Q11).
 *
 * Sobre el formato del JSON. El backend usa AddControllers() sin AddJsonOptions,
 * de modo que rige JsonSerializerDefaults.Web: las respuestas salen en camelCase
 * y las peticiones se leen sin distinguir mayusculas. Por eso se lee camelCase
 * -- { token, expiraEn, usuario }, { codigo, mensaje, detalles, traza } -- y se
 * envia PascalCase, que es la forma literal de los DTO del backend.
 *
 * El contrato de error es uniforme en toda la API, incluidos el 401 y el 403,
 * que estan cableados en OnChallenge y OnForbidden y traen el mismo cuerpo con
 * detalles vacio. No hay casos especiales por codigo (Q12).
 */

/**
 * Fallo devuelto por la API, o de red si el codigo es 0.
 *
 * detalles trae { campo, error } por cada regla incumplida. El campo llega con
 * el nombre de la propiedad de C# en PascalCase, porque sale de PropertyName de
 * FluentValidation: por eso los name de los input se escriben igual.
 */
class ErrorApi extends Error {
    constructor(codigo, mensaje, detalles, traza) {
        super(mensaje);
        this.name = 'ErrorApi';
        this.codigo = codigo;
        this.mensaje = mensaje;
        this.detalles = detalles || [];
        this.traza = traza || '';
    }
}

/**
 * Ejecuta una peticion contra la API.
 *
 * @param {string} metodo GET, POST, PUT o PATCH. El sistema no expone DELETE.
 * @param {string} ruta Ruta relativa a API_BASE, empezando por barra.
 * @param {object} [cuerpo] Cuerpo a serializar, en PascalCase.
 * @returns {Promise<object|null>} El dato ya parseado, o null si la respuesta
 *          no trae contenido.
 * @throws {ErrorApi} Ante cualquier respuesta que no sea 2xx.
 */
async function pedir(metodo, ruta, cuerpo) {
    const cabeceras = {};

    const token = tokenActual();
    if (token) cabeceras['Authorization'] = 'Bearer ' + token;

    const opciones = { method: metodo, headers: cabeceras };

    if (cuerpo !== undefined && cuerpo !== null) {
        cabeceras['Content-Type'] = 'application/json';
        opciones.body = JSON.stringify(cuerpo);
    }

    let respuesta;

    try {
        respuesta = await fetch(API_BASE + ruta, opciones);
    } catch (error) {
        // fetch solo rechaza por fallo de red, CORS o certificado. Un codigo de
        // error HTTP llega como respuesta normal y se traduce mas abajo.
        throw new ErrorApi(
            0,
            'No se pudo contactar con el servidor. Compruebe que la API este '
            + 'en marcha en ' + API_BASE + '.',
            [],
            '');
    }

    if (respuesta.ok) {
        // 204 no tiene cuerpo: intentar parsearlo lanzaria. Tampoco lo tiene un
        // 200 de longitud cero.
        if (respuesta.status === 204 || respuesta.status === 205) return null;

        const texto = await respuesta.text();
        return texto.length === 0 ? null : JSON.parse(texto);
    }

    const fallo = await traducirFallo(respuesta);

    // Mitad reactiva de la expiracion (Q7): cualquier 401 cierra la sesion y
    // redirige, salvo los dos que no hablan de la sesion sino de una credencial
    // que el usuario acaba de escribir.
    if (respuesta.status === 401 && !esVerificacionDeCredenciales(metodo, ruta)) {
        borrarSesion();
        irALogin('expirada');
    }

    throw fallo;
}

/**
 * Los dos endpoints donde un 401 significa "esa contrasena no es correcta" y no
 * "su sesion vencio".
 *
 * En el login no hay sesion que cerrar. En el cambio de contrasena propia la
 * sesion sigue viva: CambiarContrasenaUseCase lanza CredencialesInvalidasException
 * al comprobar la contrasena actual, antes de tocar nada. Cerrar sesion ahi
 * echaria al usuario por un error de tipeo, que no es lo que Q7 describe: lo
 * que si mata la sesion en ese endpoint es el 204, porque revoca el token
 * (RN-03), y de eso se ocupa la pagina.
 */
function esVerificacionDeCredenciales(metodo, ruta) {
    return ruta === '/auth/login'
        || (metodo === 'PUT' && /^\/usuarios\/\d+\/contrasena$/.test(ruta));
}

/**
 * Convierte una respuesta de error en un ErrorApi. Si el cuerpo no viniera con
 * el contrato uniforme se arma uno equivalente, para que quien lo recibe no
 * tenga que distinguir el caso.
 */
async function traducirFallo(respuesta) {
    try {
        const cuerpo = await respuesta.json();
        return new ErrorApi(
            cuerpo.codigo || respuesta.status,
            cuerpo.mensaje || respuesta.statusText,
            cuerpo.detalles,
            cuerpo.traza);
    } catch (error) {
        return new ErrorApi(respuesta.status, respuesta.statusText, [], '');
    }
}
