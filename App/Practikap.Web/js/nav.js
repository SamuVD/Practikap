/*
 * Practikap · Barra superior y barra lateral.
 *
 * Cada pagina protegida lleva un <div id="pk-nav"></div> vacio y llama a
 * pintarNav() despues de que exigirSesion haya devuelto usuario (Q9).
 *
 * La tabla PAGINAS declara el inventario completo de doce pantallas desde la
 * Ronda 1, aunque ocho todavia no existan. Cuando una ronda posterior cree su
 * archivo, lo unico que toca de aca es levantar su bandera existe: nadie
 * reescribe la tabla ni la funcion.
 *
 * Filtrar por rol es usabilidad, no autorizacion (Doc_Arquitectura 3.4). El
 * servidor responde 403 por su cuenta, y la guarda de sesion.js corta antes de
 * cualquier peticion.
 */

/**
 * Inventario de las doce paginas. login.html figura como pantalla del sistema
 * pero no es item de barra lateral: se llega a ella por la guarda o por el
 * cierre de sesion.
 *
 * roles  · quien ve el item. Sale de Matriz_de_Roles hoja 4.
 * existe · si el archivo ya esta escrito. Falso se pinta deshabilitado.
 */
const PAGINAS = [
    { href: 'login.html', texto: 'Iniciar sesión', roles: [], existe: true },

    {
        href: 'dashboard.html', texto: 'Tablero',
        roles: ['Administrador', 'Instructor', 'Aprendiz'], existe: true
    },
    {
        href: 'practicas.html', texto: 'Prácticas',
        roles: ['Administrador', 'Instructor', 'Aprendiz'], existe: false
    },
    {
        href: 'seguimiento.html', texto: 'Seguimiento',
        roles: ['Administrador', 'Instructor', 'Aprendiz'], existe: false
    },
    {
        href: 'calificaciones.html', texto: 'Calificaciones',
        roles: ['Administrador', 'Instructor', 'Aprendiz'], existe: false
    },
    {
        href: 'mensajeria.html', texto: 'Mensajería',
        roles: ['Administrador', 'Instructor', 'Aprendiz'], existe: false
    },
    {
        href: 'notificaciones.html', texto: 'Notificaciones',
        roles: ['Administrador', 'Instructor', 'Aprendiz'], existe: false
    },
    {
        href: 'reportes.html', texto: 'Reportes',
        roles: ['Administrador', 'Instructor'], existe: false
    },
    {
        href: 'gestion-usuarios.html', texto: 'Gestión de usuarios',
        roles: ['Administrador'], existe: true
    },
    {
        href: 'motor-reglas.html', texto: 'Motor de reglas',
        roles: ['Administrador'], existe: false
    },
    {
        href: 'panel-administracion.html', texto: 'Panel de administración',
        roles: ['Administrador'], existe: false
    },
    {
        href: 'mi-perfil.html', texto: 'Mi perfil',
        roles: ['Administrador', 'Instructor', 'Aprendiz'], existe: true
    }
];

/**
 * Pinta la barra superior y la barra lateral dentro de #pk-nav.
 * No hace nada si no hay sesion: en ese caso la guarda ya redirigio.
 */
function pintarNav() {
    const usuario = usuarioActual();
    const contenedor = document.getElementById('pk-nav');
    if (!usuario || !contenedor) return;

    const actual = window.location.pathname.split('/').pop();

    const items = PAGINAS
        .filter(pagina => pagina.roles.includes(usuario.rol))
        .map(pagina => itemDeMenu(pagina, actual))
        .join('');

    contenedor.innerHTML = `
        <header class="pk-barra">
            <span class="pk-marca">Practikap</span>
            <div class="pk-barra-usuario">
                <span class="pk-barra-nombre">${escapar(usuario.nombreCompleto)}</span>
                <span class="badge pk-insignia-rol">${escapar(usuario.rol)}</span>
                <button type="button" class="btn btn-sm pk-boton-salir" id="pk-salir">
                    Cerrar sesión
                </button>
            </div>
        </header>
        <nav class="pk-lateral" aria-label="Navegación principal">
            <ul class="nav flex-column">${items}</ul>
        </nav>`;

    document.getElementById('pk-salir').addEventListener('click', cerrarSesion);
}

/** Arma un item de la barra lateral. */
function itemDeMenu(pagina, actual) {
    if (!pagina.existe) {
        return `<li class="nav-item">
            <span class="nav-link disabled" aria-disabled="true"
                  title="Disponible en una ronda posterior">
                ${escapar(pagina.texto)}
                <span class="pk-pendiente">pendiente</span>
            </span>
        </li>`;
    }

    const activo = pagina.href === actual;

    return `<li class="nav-item">
        <a class="nav-link${activo ? ' active' : ''}" href="${pagina.href}"
           ${activo ? 'aria-current="page"' : ''}>${escapar(pagina.texto)}</a>
    </li>`;
}

/**
 * Neutraliza el marcado de un texto antes de insertarlo. El nombre y el correo
 * los escribe un usuario, y llegan a la pagina como datos, no como HTML.
 */
function escapar(texto) {
    const nodo = document.createElement('span');
    nodo.textContent = texto === null || texto === undefined ? '' : String(texto);
    return nodo.innerHTML;
}
