/*
 * Practikap · login.html
 *
 * Consume POST /api/auth/login.
 *
 * Al cargar se borra cualquier sesion previa: si el usuario llego hasta aca es
 * porque la anterior no sirve, y dejarla puesta haria que el token viejo
 * viajara en la peticion de login.
 */

const formularioLogin = document.getElementById('pk-formulario');
const bandaLogin = document.getElementById('pk-banda');
const avisoLogin = document.getElementById('pk-aviso');

/** Textos de los cuatro motivos con los que se puede llegar a esta pantalla. */
const MOTIVOS = {
    expirada: {
        clase: 'alert-warning',
        texto: 'Su sesión expiró. Vuelva a iniciar sesión.'
    },
    requerida: {
        clase: 'alert-warning',
        texto: 'Inicie sesión para acceder a esa pantalla.'
    },
    cerrada: {
        clase: 'alert-success',
        texto: 'Sesión cerrada correctamente.'
    },
    'contrasena-cambiada': {
        clase: 'alert-success',
        texto: 'Su contraseña se cambió. Vuelva a iniciar sesión con la nueva.'
    }
};

iniciar();

function iniciar() {
    borrarSesion();

    const motivo = new URLSearchParams(window.location.search).get('motivo');
    const aviso = MOTIVOS[motivo];

    if (aviso) {
        avisoLogin.className = 'alert ' + aviso.clase;
        avisoLogin.textContent = aviso.texto;
    }

    formularioLogin.addEventListener('submit', entrar);
}

async function entrar(evento) {
    evento.preventDefault();
    limpiarErrores();

    const boton = document.getElementById('pk-entrar');
    boton.disabled = true;

    try {
        // El cuerpo va en PascalCase, que es la forma literal de LoginRequest.
        const respuesta = await pedir('POST', '/auth/login', {
            Correo: document.getElementById('pk-correo').value,
            Contrasena: document.getElementById('pk-contrasena').value
        });

        guardarSesion(respuesta);
        window.location.replace('dashboard.html');
    } catch (error) {
        mostrarError(error);
        boton.disabled = false;
    }
}

/** Devuelve el formulario a su estado limpio antes de cada intento. */
function limpiarErrores() {
    avisoLogin.className = 'alert d-none';
    bandaLogin.className = 'alert d-none';
    bandaLogin.innerHTML = '';

    formularioLogin.querySelectorAll('.is-invalid')
        .forEach(campo => campo.classList.remove('is-invalid'));
    formularioLogin.querySelectorAll('.invalid-feedback')
        .forEach(nota => { nota.textContent = ''; });
}

/**
 * Reparte el fallo segun su codigo (Q12).
 *
 * El 401 de esta pantalla es el unico del sistema que no cierra sesion: aca
 * significa credenciales incorrectas o cuenta inactiva, no token vencido, y por
 * eso api.js lo deja pasar sin redirigir.
 */
function mostrarError(error) {
    if (error.codigo === 400 && error.detalles.length > 0) {
        const sobrantes = [];

        error.detalles.forEach(detalle => {
            const campo = formularioLogin.querySelector(`[name="${detalle.campo}"]`);

            if (campo) {
                campo.classList.add('is-invalid');
                const nota = campo.parentElement.querySelector('.invalid-feedback');
                if (nota) nota.textContent = detalle.error;
            } else {
                sobrantes.push(detalle.error);
            }
        });

        if (sobrantes.length > 0) banda('alert-danger', sobrantes.join(' '));
        return;
    }

    if (error.codigo === 422) {
        banda('alert-danger pk-banda-destacada', error.mensaje);
        return;
    }

    if (error.codigo === 500) {
        banda('alert-danger', error.mensaje, error.traza);
        return;
    }

    banda('alert-danger', error.mensaje);
}

/** Pinta la banda superior del formulario, con la traza si la hay. */
function banda(clases, mensaje, traza) {
    bandaLogin.className = 'alert ' + clases;
    bandaLogin.textContent = mensaje;

    if (traza) {
        const linea = document.createElement('div');
        linea.className = 'pk-traza mt-2';
        linea.textContent = 'Traza: ' + traza;
        bandaLogin.appendChild(linea);
    }
}
