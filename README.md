# Practikap

Plataforma web para la gestión de prácticas productivas del SENA.

Practikap nace como alternativa especializada a Sofía Plus para un proceso concreto:
el seguimiento de la etapa productiva. Tres roles —Administrador, Instructor y
Aprendiz— operan sobre prácticas, seguimientos, observaciones, calificación
bidireccional y mensajería interna. Su componente diferenciador es un **Motor de
Reglas Dinámicas** que permite modificar el comportamiento del sistema sin
redespliegue.

**Samuel Villarreal Duque** | SENA | Programación de Software |

---

## Stack

| Ámbito | Tecnología |
|---|---|
| Runtime | .NET 10 LTS · C# 14 |
| API | ASP.NET Core 10 (controladores) |
| ORM | EF Core 9 · Pomelo 9.0.0 |
| Base de datos | MySQL 9.7.2 · InnoDB · utf8mb4 |
| Seguridad | JWT HS256 · BCrypt |
| Apoyo | AutoMapper · FluentValidation · Swagger/OpenAPI |
| Frontend | HTML5 · CSS3 · JavaScript · Bootstrap 5 |

> **Nota sobre versiones.** EF Core está fijado en la línea **9.x**.
> Pomelo 9.0.0 es el proveedor MySQL soportado y depende de EF Core 9.

---

## Arquitectura

Clean Architecture en cuatro capas. Las dependencias apuntan siempre hacia el núcleo.

```
Practikap.Web  ──HTTP/fetch──▶  Practikap.API
                                     │
                       ┌─────────────┴─────────────┐
                       ▼                           ▼
             Practikap.Application ◀──── Practikap.Infrastructure
                       │                           │
                       └──────────┬────────────────┘
                                  ▼
                          Practikap.Domain
                        (no referencia a nadie)
```

`Practikap.Web` **no forma parte de `Practikap.sln`**: es una carpeta de archivos
estáticos que se comunica con la API únicamente por HTTP, nunca por referencia de
proyecto. El patrón MVC se cumple dentro de `Practikap.API`.

Detalle completo en `Info/03_Diseno/Arquitectura/`.

---

## Estructura del repositorio

```
Practikap/
├── Practikap.sln
└── App/
    ├── Practikap.Domain/          Entidades, interfaces, Motor de Reglas
    ├── Practikap.Application/     Casos de uso, DTOs, validadores, mapeos
    ├── Practikap.Infrastructure/  EF Core, repositorios, seguridad, migraciones
    ├── Practikap.API/             Controladores, middlewares, Program.cs
    └── Practikap.Web/             Frontend estático (fuera de la solución)
```

> **Documentación en repositorio aparte.** La carpeta `Info/` —requerimientos,
> matrices, diagramas, actas y demás documentación de gestión— se versiona en un
> repositorio privado separado, propiedad de CodeLabG2. No forma parte de este
> repositorio y no se referencia por ruta relativa desde aquí.

---

## Puesta en marcha

**Requisitos:** .NET SDK 10.0.201 o superior · MySQL 9.7.2 · `dotnet-ef`

```bash
# 1. Herramienta de migraciones (una sola vez por máquina)
dotnet tool install --global dotnet-ef

# 2. Restaurar y compilar
dotnet restore
dotnet build Practikap.sln

# 3. Variables de entorno (ver Doc_Tecnico §6.2)
export ConnectionStrings__MySQL="Server=localhost;Database=practikap;User=app_user;Password=***;"
export Jwt__SecretKey="<clave-aleatoria-minimo-256-bits>"
export Jwt__Issuer="https://practikap.app"
export Jwt__ExpirationMinutes="60"
export CORS__AllowedOrigins="http://localhost:5500"

# 4. Aplicar la migración inicial
dotnet ef database update \
  --project        App/Practikap.Infrastructure \
  --startup-project App/Practikap.API

# 5. Levantar la API
dotnet run --project App/Practikap.API
```

Swagger queda disponible en `/swagger` mientras el entorno sea `Development`.

**Ninguna credencial se versiona.** `appsettings.Production.json` está en
`.gitignore` y todos los valores sensibles se inyectan como variables de entorno
(RNF-06).

---

## Flujo de trabajo

GitFlow. `main` está protegida y solo recibe merges por pull request revisado.

| Rama | Propósito |
|---|---|
| `main` | Producción. Desplegable en todo momento. |
| `develop` | Integración continua. Base del desarrollo activo. |
| `feature/*` | Un paso del plan de desarrollo. Nace de `develop`. |
| `release/*` | Preparación de versión. |
| `hotfix/*` | Corrección urgente sobre producción. |

Las ramas de funcionalidad siguen el patrón `feature/[paso]-[descripcion-corta]`,
por ejemplo `feature/3.1-scaffolding`.

---

## Documentación

La documentación de gestión y diseño —requerimientos, matrices, diagramas,
seguimiento— vive en el repositorio privado `Info` de SamuVD, separado de este.
Entre sus artefactos: `Doc_Base`, `Doc_Negocio`, `Matriz_de_Requerimientos`
(fuente canónica de RF, RNF, RN, CU, HU y EP), `Matriz_de_Roles`, `Doc_Casos_de_Uso`,
`Script_DDL.sql` (referencia del esquema físico), `Doc_Arquitectura`, `Doc_Tecnico`,
`Doc_Stack_Tecnologico`, `Doc_Wireframes` y `Doc_Seguimiento`.

Acceso restringido.

---

## Estado

Fases 0 a 2 completadas. **Fase 3 en progreso** — paso 3.1, esqueleto del proyecto.
