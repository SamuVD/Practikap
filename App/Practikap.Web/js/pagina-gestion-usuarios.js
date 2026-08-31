/*
 * Practikap · gestion-usuarios.html
 *
 * Consume las ocho operaciones del Administrador sobre M1:
 *   GET   /api/roles
 *   GET   /api/usuarios?rolId=
 *   GET   /api/usuarios/{id}
 *   POST  /api/usuarios
 *   PUT   /api/usuarios/{id}
 *   PATCH /api/usuarios/{id}/rol
 *   PATCH /api/usuarios/{id}/contrasena
 *   PATCH /api/usuarios/{id}/estado
 */

const administrador = exigirSesion(['Administrador']);

const bandaPagina = document.getElementById('pk-banda');
const bandaAlta = document.getElementById('pk-banda-alta');
const bandaEdicion = document.getElementById('pk-banda-edicion');

const formularioAlta = document.getElementById('pk-formulario-alta');
const formularioEdicion = document.getElementById('pk-formulario-edicion');
const formularioRol = document.getElementById('pk-formulario-rol');
const formularioRestablecer = document.getElementById('pk-formulario-restablecer');

const filtroRol = document.getElementById('pk-filtro-rol');
const cuerpoTabla = document.getElementById('pk-cuerpo-tabla');

let modalAlta;
let modalEdicion;

/** Id del usuario abierto en el modal de edicion. */
let idEnEdicion = null;

if (administrador) iniciarGestion();

async function iniciarGestion() {
    pintarNav();

    modalAlta = new bootstrap.Modal(document.getElementById('pk-modal-alta'));
    modalEdicion = new bootstrap.Modal(document.getElementById('pk-modal-edicion'));

    document.getElementById('pk-abrir-alta').addEventListener('click', abrirAlta);
    filtroRol.addEventListener('change', cargarUsuarios);
    cuerpoTabla.addEventListener('click', atenderAccionDeFila);

    formularioAlta.addEventListener('submit', crearUsuario);
    formularioEdicion.addEventListener('submit', guardarDatos);
    formularioRol.addEventListener('submit', cambiarRol);
    formularioRestablecer.addEventListener('submit', restablecerContrasena);

    await cargarRoles();
    await cargarUsuarios();
}

/* --- Catalogo de roles --------------------------------------------------- */

/**
 * Puebla los tres selectores de rol. El catalogo es fijo y lo sirve
 * GET /api/roles, que solo ve el Administrador.
 */
async function cargarRoles() {
    try {
        const roles = await pedir('GET', '/roles');

        const opciones = roles
            .map(rol => `<option value="${rol.id}">${escapar(rol.nombre)}</option>`)
            .join('');

        filtroRol.innerHTML = '<option value="">Todos los roles</option>' + opciones;
        document.getElementById('pk-alta-rol').innerHTML =
            '<option value="">Seleccione un rol</option>' + opciones;
        document.getElementById('pk-edicion-rol').innerHTML = opciones;
    } catch (error) {
        mostrarError(error, null, bandaPagina);
    }
}

/* --- Listado ------------------------------------------------------------- */

async function cargarUsuarios() {
    const rolId = filtroRol.value;
    const ruta = rolId ? '/usuarios?rolId=' + encodeURIComponent(rolId) : '/usuarios';

    try {
        pintarTabla(await pedir('GET', ruta));
    } catch (error) {
        mostrarError(error, null, bandaPagina);
    }
}

function pintarTabla(usuarios) {
    if (usuarios.length === 0) {
        cuerpoTabla.innerHTML = `<tr>
            <td colspan="8" class="text-center text-body-secondary py-4">
                No hay usuarios con ese filtro.
            </td>
        </tr>`;
        return;
    }

    cuerpoTabla.innerHTML = usuarios.map(fila).join('');
}

/**
 * Una fila de la tabla. El estado se muestra con la etiqueta del enumerado y se
 * cambia con un booleano: UsuarioResponse.estado es texto y
 * CambiarEstadoRequest.Activo es booleano, y no se mandan igual.
 */
function fila(usuario) {
    const activo = usuario.estado === 'Activo';

    return `<tr>
        <td>${usuario.id}</td>
        <td>${escapar(usuario.nombreCompleto)}</td>
        <td>${escapar(usuario.correo)}</td>
        <td>${escapar(usuario.telefono || '—')}</td>
        <td>${escapar(usuario.rol)}</td>
        <td>
            <span class="badge text-bg-${activo ? 'success' : 'secondary'}">
                ${escapar(etiquetaDe(EstadoUsuario, usuario.estado))}
            </span>
        </td>
        <td>${new Date(usuario.fechaCreacion).toLocaleDateString('es-CO')}</td>
        <td class="text-nowrap">
            <button type="button" class="btn btn-sm btn-outline-primary"
                    data-accion="editar" data-id="${usuario.id}">Editar</button>
            <button type="button" class="btn btn-sm btn-outline-primary"
                    data-accion="estado" data-id="${usuario.id}"
                    data-activo="${activo}">${activo ? 'Desactivar' : 'Activar'}</button>
        </td>
    </tr>`;
}

function atenderAccionDeFila(evento) {
    const boton = evento.target.closest('button[data-accion]');
    if (!boton) return;

    const id = Number(boton.dataset.id);

    if (boton.dataset.accion === 'editar') abrirEdicion(id);
    if (boton.dataset.accion === 'estado') cambiarEstado(id, boton.dataset.activo === 'true');
}

/* --- Alta ---------------------------------------------------------------- */

function abrirAlta() {
    formularioAlta.reset();
    limpiar(formularioAlta, bandaAlta);
    modalAlta.show();
}

/** POST /api/usuarios. Un correo ya registrado responde 409, no 422. */
async function crearUsuario(evento) {
    evento.preventDefault();
    limpiar(formularioAlta, bandaAlta);

    const boton = document.getElementById('pk-crear');
    boton.disabled = true;

    const telefono = document.getElementById('pk-alta-telefono').value.trim();

    try {
        await pedir('POST', '/usuarios', {
            RolId: Number(document.getElementById('pk-alta-rol').value),
            Correo: document.getElementById('pk-alta-correo').value,
            Contrasena: document.getElementById('pk-alta-contrasena').value,
            Nombre: document.getElementById('pk-alta-nombre').value,
            Apellido: document.getElementById('pk-alta-apellido').value,
            Telefono: telefono.length > 0 ? telefono : null
        });

        modalAlta.hide();
        avisoDePagina('alert-success', 'El usuario se creó.');
        await cargarUsuarios();
    } catch (error) {
        mostrarError(error, formularioAlta, bandaAlta);
    } finally {
        boton.disabled = false;
    }
}

/* --- Edicion ------------------------------------------------------------- */

/** GET /api/usuarios/{id}. Se relee la cuenta al abrir: no hay cache (Q15). */
async function abrirEdicion(id) {
    limpiar(formularioEdicion, bandaEdicion);
    limpiar(formularioRol, bandaEdicion);
    limpiar(formularioRestablecer, bandaEdicion);
    formularioRestablecer.reset();

    try {
        const usuario = await pedir('GET', '/usuarios/' + id);

        idEnEdicion = usuario.id;

        document.getElementById('pk-titulo-edicion').textContent =
            'Editar a ' + usuario.nombreCompleto;
        document.getElementById('pk-edicion-resumen').textContent =
            usuario.correo + ' · ' + usuario.rol + ' · '
            + etiquetaDe(EstadoUsuario, usuario.estado);

        document.getElementById('pk-edicion-nombre').value = usuario.nombre;
        document.getElementById('pk-edicion-apellido').value = usuario.apellido;
        document.getElementById('pk-edicion-telefono').value = usuario.telefono || '';

        const selectorDeRol = document.getElementById('pk-edicion-rol');
        const opcion = Array.from(selectorDeRol.options)
            .find(elemento => elemento.textContent === usuario.rol);
        if (opcion) selectorDeRol.value = opcion.value;

        modalEdicion.show();
    } catch (error) {
        mostrarError(error, null, bandaPagina);
    }
}

/** PUT /api/usuarios/{id}. */
async function guardarDatos(evento) {
    evento.preventDefault();
    limpiar(formularioEdicion, bandaEdicion);

    const boton = document.getElementById('pk-guardar-edicion');
    boton.disabled = true;

    const telefono = document.getElementById('pk-edicion-telefono').value.trim();

    try {
        await pedir('PUT', '/usuarios/' + idEnEdicion, {
            Nombre: document.getElementById('pk-edicion-nombre').value,
            Apellido: document.getElementById('pk-edicion-apellido').value,
            Telefono: telefono.length > 0 ? telefono : null
        });

        banda(bandaEdicion, 'alert-success', 'Los datos se guardaron.');
        await cargarUsuarios();
    } catch (error) {
        mostrarError(error, formularioEdicion, bandaEdicion);
    } finally {
        boton.disabled = false;
    }
}

/**
 * PATCH /api/usuarios/{id}/rol (RN-01). Responde 422 si el Administrador
 * cambia su propio rol: el mensaje va a la banda destacada.
 */
async function cambiarRol(evento) {
    evento.preventDefault();
    limpiar(formularioRol, bandaEdicion);

    const boton = document.getElementById('pk-guardar-rol');
    boton.disabled = true;

    try {
        await pedir('PATCH', '/usuarios/' + idEnEdicion + '/rol', {
            RolId: Number(document.getElementById('pk-edicion-rol').value)
        });

        banda(bandaEdicion, 'alert-success', 'El rol se cambió.');
        await cargarUsuarios();
    } catch (error) {
        mostrarError(error, formularioRol, bandaEdicion);
    } finally {
        boton.disabled = false;
    }
}

/** PATCH /api/usuarios/{id}/contrasena. Devuelve 204 y no cierra ninguna sesion. */
async function restablecerContrasena(evento) {
    evento.preventDefault();
    limpiar(formularioRestablecer, bandaEdicion);

    const boton = document.getElementById('pk-guardar-restablecer');
    boton.disabled = true;

    try {
        await pedir('PATCH', '/usuarios/' + idEnEdicion + '/contrasena', {
            ContrasenaNueva: document.getElementById('pk-edicion-contrasena').value
        });

        formularioRestablecer.reset();
        banda(bandaEdicion, 'alert-success', 'La contraseña se restableció.');
    } catch (error) {
        mostrarError(error, formularioRestablecer, bandaEdicion);
    } finally {
        boton.disabled = false;
    }
}

/**
 * PATCH /api/usuarios/{id}/estado. Sustituye a la eliminacion: el sistema no
 * expone DELETE. Responde 422 si el Administrador desactiva su propia cuenta.
 */
async function cambiarEstado(id, estaActivo) {
    const accion = estaActivo ? 'desactivar' : 'activar';
    if (!window.confirm('¿Confirma ' + accion + ' la cuenta ' + id + '?')) return;

    try {
        await pedir('PATCH', '/usuarios/' + id + '/estado', { Activo: !estaActivo });

        avisoDePagina('alert-success', 'La cuenta ' + id + ' quedó '
            + (estaActivo ? 'inactiva.' : 'activa.'));
        await cargarUsuarios();
    } catch (error) {
        mostrarError(error, null, bandaPagina);
    }
}

/* --- Errores (Q12) ------------------------------------------------------- */

function limpiar(formulario, elementoDeBanda) {
    elementoDeBanda.className = 'alert d-none';
    elementoDeBanda.innerHTML = '';

    if (!formulario) return;

    formulario.querySelectorAll('.is-invalid')
        .forEach(campo => campo.classList.remove('is-invalid'));
    formulario.querySelectorAll('.invalid-feedback')
        .forEach(nota => { nota.textContent = ''; });
}

/**
 * Reparte el fallo. Un 401 no se pinta: api.js ya cerro la sesion y redirigio,
 * y la banda solo alcanzaria a parpadear.
 */
function mostrarError(error, formulario, elementoDeBanda) {
    if (error.codigo === 401) return;

    if (error.codigo === 400 && error.detalles.length > 0 && formulario) {
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

    // 403, 404, 409 y el resto: banda sin destacar. El 409 es el correo ya
    // registrado, que el alta produce y que no es un 422.
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

/** Banda de la pagina, para lo que ocurre fuera de un modal. */
function avisoDePagina(clases, mensaje) {
    banda(bandaPagina, clases, mensaje);
}
