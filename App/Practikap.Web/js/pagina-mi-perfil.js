/*
 * Practikap · mi-perfil.html
 *
 * Consume GET /api/usuarios/{id}, PUT /api/usuarios/{id} y
 * PUT /api/usuarios/{id}/contrasena, siempre sobre la cuenta propia.
 */

const usuarioDelPerfil = exigirSesion();

const formularioPerfil = document.getElementById('pk-formulario-perfil');
const formularioClave = document.getElementById('pk-formulario-clave');
const bandaPerfil = document.getElementById('pk-banda-perfil');
const bandaClave = document.getElementById('pk-banda-clave');

if (usuarioDelPerfil) iniciarPerfil();

function iniciarPerfil() {
    pintarNav();

    formularioPerfil.addEventListener('submit', guardarPerfil);
    formularioClave.addEventListener('submit', cambiarClave);

    cargarPerfil();
}

/**
 * Pide la cuenta propia. Cada pagina pide sus datos al cargar y no hay cache
 * (Q15): recargar es volver a pedir.
 */
async function cargarPerfil() {
    try {
        const usuario = await pedir('GET', '/usuarios/' + usuarioDelPerfil.id);
        volcar(usuario);
    } catch (error) {
        mostrarError(error, formularioPerfil, bandaPerfil);
    }
}

/** Escribe la respuesta en los campos. Los tres primeros van deshabilitados. */
function volcar(usuario) {
    document.getElementById('pk-correo').value = usuario.correo;
    document.getElementById('pk-rol').value = usuario.rol;
    document.getElementById('pk-estado').value = usuario.estado;
    document.getElementById('pk-nombre').value = usuario.nombre;
    document.getElementById('pk-apellido').value = usuario.apellido;
    document.getElementById('pk-telefono').value = usuario.telefono || '';
}

async function guardarPerfil(evento) {
    evento.preventDefault();
    limpiar(formularioPerfil, bandaPerfil);

    const boton = document.getElementById('pk-guardar-perfil');
    boton.disabled = true;

    const telefono = document.getElementById('pk-telefono').value.trim();

    try {
        const actualizado = await pedir('PUT', '/usuarios/' + usuarioDelPerfil.id, {
            Nombre: document.getElementById('pk-nombre').value,
            Apellido: document.getElementById('pk-apellido').value,
            // Telefono es opcional en el DTO: en blanco se manda ausente, no
            // como cadena vacia.
            Telefono: telefono.length > 0 ? telefono : null
        });

        volcar(actualizado);
        refrescarUsuarioDeSesion(actualizado);

        banda(bandaPerfil, 'alert-success', 'Los datos se guardaron.');
    } catch (error) {
        mostrarError(error, formularioPerfil, bandaPerfil);
    } finally {
        boton.disabled = false;
    }
}

/**
 * Sustituye el usuario guardado por el que acaba de devolver el servidor, para
 * que la barra superior no siga mostrando el nombre viejo. El token y su
 * vencimiento no se tocan: la edicion de perfil no los renueva.
 */
function refrescarUsuarioDeSesion(usuario) {
    const sesion = leerSesion();
    if (!sesion) return;

    guardarSesion({ token: sesion.token, expiraEn: sesion.expiraEn, usuario: usuario });
    pintarNav();
}

/**
 * Cambia la contrasena propia. El 204 revoca el token en curso (RN-03): a
 * partir de ahi la sesion esta muerta, asi que se borra y se manda al login.
 * Es la unica operacion de la ronda que se auto-invalida.
 */
async function cambiarClave(evento) {
    evento.preventDefault();
    limpiar(formularioClave, bandaClave);

    const boton = document.getElementById('pk-guardar-clave');
    boton.disabled = true;

    try {
        await pedir('PUT', '/usuarios/' + usuarioDelPerfil.id + '/contrasena', {
            ContrasenaActual: document.getElementById('pk-clave-actual').value,
            ContrasenaNueva: document.getElementById('pk-clave-nueva').value
        });

        borrarSesion();
        irALogin('contrasena-cambiada');
    } catch (error) {
        mostrarError(error, formularioClave, bandaClave);
        boton.disabled = false;
    }
}

/* --- Errores (Q12) ------------------------------------------------------- */

function limpiar(formulario, elementoDeBanda) {
    elementoDeBanda.className = 'alert d-none';
    elementoDeBanda.innerHTML = '';

    formulario.querySelectorAll('.is-invalid')
        .forEach(campo => campo.classList.remove('is-invalid'));
    formulario.querySelectorAll('.invalid-feedback')
        .forEach(nota => { nota.textContent = ''; });
}

function mostrarError(error, formulario, elementoDeBanda) {
    // Un 401 aca significa sesion vencida o revocada, y api.js ya cerro y
    // redirigio. Pintar la banda solo mostraria un parpadeo antes de navegar.
    // La excepcion es el 401 del cambio de contrasena, que es "la actual no es
    // correcta" y sube con la sesion todavia viva.
    if (error.codigo === 401 && formulario !== formularioClave) return;

    if (error.codigo === 400 && error.detalles.length > 0) {
        const sobrantes = [];

        error.detalles.forEach(detalle => {
            const campo = formulario.querySelector(`[name="${detalle.campo}"]`);

            if (campo) {
                campo.classList.add('is-invalid');
                const nota = campo.parentElement.querySelector('.invalid-feedback');
                if (nota) nota.textContent = detalle.error;
            } else {
                sobrantes.push(detalle.error);
            }
        });

        if (sobrantes.length > 0) banda(elementoDeBanda, 'alert-danger', sobrantes.join(' '));
        return;
    }

    if (error.codigo === 422) {
        banda(elementoDeBanda, 'alert-danger pk-banda-destacada', error.mensaje);
        return;
    }

    if (error.codigo === 500) {
        banda(elementoDeBanda, 'alert-danger', error.mensaje, error.traza);
        return;
    }

    banda(elementoDeBanda, 'alert-danger', error.mensaje);
}

function banda(elemento, clases, mensaje, traza) {
    elemento.className = 'alert ' + clases;
    elemento.textContent = mensaje;

    if (traza) {
        const linea = document.createElement('div');
        linea.className = 'pk-traza mt-2';
        linea.textContent = 'Traza: ' + traza;
        elemento.appendChild(linea);
    }
}
