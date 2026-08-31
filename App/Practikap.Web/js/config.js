/*
 * Practikap · Configuracion del entorno.
 *
 * Unico lugar del frontend donde se escribe la URL de la API. Ningun otro
 * archivo declara un host, un puerto ni un prefijo de ruta: todos componen
 * sobre API_BASE.
 *
 * El perfil https de launchSettings.json publica la API en 7203. El frontend
 * se sirve con Live Server en 5500, y los cuatro origenes ya estan en
 * CORS:AllowedOrigins de appsettings.Development.json.
 *
 * Apuntar al puerto http (5285) haria que UseHttpsRedirection respondiera 307
 * antes de llegar al controlador, que es justo lo que la comprobacion 12 de la
 * ronda vigila.
 */

const API_BASE = 'https://localhost:7203/api';
