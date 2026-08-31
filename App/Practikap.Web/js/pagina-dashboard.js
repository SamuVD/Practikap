/*
 * Practikap · dashboard.html
 *
 * Armazon sin indicadores. No llama a ningun endpoint: el saludo sale del
 * usuario guardado en la sesion y las tarjetas de acceso salen de la misma
 * tabla PAGINAS que pinta la barra lateral, de modo que ambas no puedan
 * discrepar sobre lo que un rol ve.
 *
 * La ronda que agregue los indicadores solo suma peticiones aca abajo.
 */

const usuarioDelTablero = exigirSesion();

// exigirSesion ya redirigio: una redireccion no detiene el script en curso, y
// seguir adelante pintaria una pantalla que el usuario no va a ver.
if (usuarioDelTablero) iniciarTablero(usuarioDelTablero);

function iniciarTablero(usuario) {
    pintarNav();

    document.getElementById('pk-saludo').textContent =
        'Hola, ' + usuario.nombre + '.';

    document.getElementById('pk-subtitulo').textContent =
        'Sesión iniciada como ' + usuario.rol + ' · ' + usuario.correo;

    if (new URLSearchParams(window.location.search).get('motivo') === 'sin-permiso') {
        const aviso = document.getElementById('pk-aviso');
        aviso.className = 'alert alert-warning';
        aviso.textContent =
            'Esa pantalla no está disponible para el rol ' + usuario.rol + '.';
    }

    pintarAccesos(usuario);
}

/**
 * Una tarjeta por pantalla que el rol puede abrir, salvo el propio tablero y el
 * login. Las que todavia no existen se pintan apagadas y sin enlace, con el
 * mismo criterio que la barra lateral.
 */
function pintarAccesos(usuario) {
    const contenedor = document.getElementById('pk-accesos');

    const tarjetas = PAGINAS
        .filter(pagina => pagina.roles.includes(usuario.rol))
        .filter(pagina => pagina.href !== 'dashboard.html')
        .map(pagina => pagina.existe ? tarjetaActiva(pagina) : tarjetaPendiente(pagina));

    contenedor.innerHTML = tarjetas.join('');
}

function tarjetaActiva(pagina) {
    return `<div class="col">
        <a class="card h-100 text-decoration-none" href="${pagina.href}">
            <div class="card-body">
                <span class="card-title h6 d-block mb-0">${escapar(pagina.texto)}</span>
            </div>
        </a>
    </div>`;
}

function tarjetaPendiente(pagina) {
    return `<div class="col">
        <div class="card h-100 opacity-50">
            <div class="card-body d-flex justify-content-between align-items-center">
                <span class="card-title h6 d-block mb-0">${escapar(pagina.texto)}</span>
                <span class="pk-pendiente">pendiente</span>
            </div>
        </div>
    </div>`;
}
