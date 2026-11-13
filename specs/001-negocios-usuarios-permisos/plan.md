# Implementation Plan: Negocios, usuarios, roles y permisos

**Branch**: `001-negocios-usuarios-permisos-plan` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-negocios-usuarios-permisos/spec.md`

## Summary

Es la base multinegocio de la plataforma. Incluye:
- Negocios administrados por el Super Administrador, con roles sugeridos copiados desde una
  plantilla.
- Usuarios por negocio con invitación por correo.
- Inicio de sesión con bloqueo por correo y segundo factor para el Super Administrador.
- Roles configurables con una matriz Módulo × Acción y la bandera "acceso total".
- Control de acceso por permiso en cada acción y menú.
- Auditoría de cambios y de eventos de seguridad.

**Enfoque técnico**:
- **Interfaz**: Razor renderizado en `Electronic.Web` (HTML, CSS y JS puros).
- **Servicios**: en `Electronic.Application`.
- **Datos**: repositorios Dapper que solo llaman procedimientos `usp_*`.
- **Aislamiento**: `@NegocioId` + Row-Level Security con `SESSION_CONTEXT`, establecido por
  una fábrica de conexiones Scoped.
- **Sesiones**: guardadas en el servidor y validadas en cada solicitud, para que los cambios
  de permisos y estados apliquen en la siguiente acción.
- **Esquema y semillas**: en scripts numerados aplicados con DbUp.

Las decisiones completas están en [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) con `Nullable` habilitado. `TreatWarningsAsErrors` queda para la funcionalidad de deuda técnica (R18)

**Primary Dependencies**:
- ASP.NET Core MVC: cookie auth, antiforgery, rate limiting, Data Protection y `IMemoryCache`.
- Dapper 2.1 y Microsoft.Data.SqlClient 7.1 (ya presentes).
- `PasswordHasher` vía `FrameworkReference Microsoft.AspNetCore.App`.
- `dbup-sqlserver` (MIT, autorizado por la constitución 2.0.0).

No hay librerías de imágenes ni paquetes de pago.

**Storage**: SQL Server 2019 o posterior. Procedimientos almacenados, RLS, TVP (`dbo.TipoListaIds`) y `VARBINARY(MAX)` para los logos en PNG o JPEG.

**Testing**:
- xUnit + Moq + coverlet (unitarias, en `Electronic.Application.Tests`).
- xUnit contra SQL Server real (integración, en el proyecto nuevo `Electronic.Infrastructure.Tests`). Se omiten si no existe `PRUEBAS_SQL`.

**Target Platform**: servidor Windows o Linux con ASP.NET Core; navegadores modernos (celular, tablet y escritorio)

**Project Type**: aplicación web renderizada en servidor sobre una solución por capas (Clean Architecture existente)

**Performance Goals**:
- Páginas de administración con p95 < 500 ms.
- La validación de sesión cuesta un solo procedimiento por solicitud.
- El inicio de sesión tarda < 1 s, incluido el hash.

**Constraints**:
- Aislamiento total entre negocios (SC-002).
- Los cambios de permisos y estados aplican en la siguiente acción (SC-006).
- Sin bibliotecas de frontend.
- Sin SQL en C#.
- Correos en español.

**Scale/Scope**:
- Al inicio, 1 negocio; diseñado para ~50 negocios × ~50 usuarios.
- Auditoría de decenas de miles de filas al año.
- ~25 pantallas.
- ~45 procedimientos.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluado contra la constitución **2.0.0**.

| Principio | Cumplimiento en el diseño | Estado |
|---|---|---|
| I. Arquitectura y stack fijo | Mismas capas y carpetas por módulo. Dapper solo con procedimientos. DI por constructor; la fábrica de conexiones pasa a Scoped. Único NuGet nuevo: `dbup-sqlserver` (MIT, autorizado). También FrameworkReference (no es NuGet) y un proyecto de pruebas de integración | ✅ (ver Complexity Tracking) |
| II. Aislamiento multinegocio | `@NegocioId` en todo procedimiento de negocio. RLS FILTER y BLOCK con `SESSION_CONTEXT` de solo lectura (R5). El NegocioId sale de la sesión del servidor, nunca del cliente | ✅ |
| III. Frontend sin dependencias | Razor + CSS con variables + JS mínimo. Ninguna vista nueva usa Bootstrap ni jQuery (R16). Accesible y responsive. Borrar `wwwroot/lib` queda para la funcionalidad de deuda técnica | ⚠️ Ver Complexity Tracking |
| IV. Calidad | El código nuevo compila sin advertencias. Nombres de dominio en español. Sin textos mágicos: catálogos y banderas (`EsConsulta`, `EsAdministracion`, `EsAccesoTotal`). `async` + `CancellationToken` en todo acceso a la base de datos y al correo. `TreatWarningsAsErrors` y `.editorconfig` quedan para la funcionalidad de deuda técnica (R18) | ⚠️ Ver Complexity Tracking |
| V. Datos con procedimientos | Plantilla `usp_` con `NOCOUNT`, `TRY/CATCH`, `XACT_ABORT` y transacción interna. TVP para listas. Paginación `@Pagina`/`@TamanoPagina`. `rowversion` en todo `UPDATE` (50001). Auditoría dentro del procedimiento. Scripts DbUp con `CREATE OR ALTER`. Logo en `Archivos` como PNG o JPEG original, validado por firma, nunca cargado en listados (R12) | ✅ |
| VI. Seguridad | `[RequierePermiso("Modulo.Accion")]` en cada acción. Antiforgery global. `PasswordHasher`. Secretos en user secrets y variables de entorno. Logo validado por firma y tamaño. Rate limiting en autenticación | ✅ |
| VII. Errores y registros | Manejador global + `ILogger<T>` estructurado. Los servicios atrapan solo las excepciones de Domain (R14). Sin datos sensibles en logs ni en auditoría | ✅ |
| VIII. Pruebas | Unitarias de reglas de negocio + integración de RLS, aislamiento, bloqueo, concurrencia y auditoría. Cobertura ≥ 80%. Las de integración corren en local con `PRUEBAS_SQL`; SQL Server en el CI queda para la funcionalidad de deuda técnica | ⚠️ Ver Complexity Tracking |
| IX. Catálogos y configuración | Permisos, plantillas de rol, plantillas de correo y configuración de seguridad en la base de datos, sembrados por scripts y editables por el Super Administrador. Caché con invalidación. Sin comparaciones contra textos | ✅ |

**Resultado previo a Phase 0**: PASS.
- Las imágenes se guardan en su formato original y no hay paquetes nuevos de imágenes. La
  constitución 2.0.0 se ajustó el 2026-11-11 con estas dos reglas.
- Las 3 desviaciones marcadas con ⚠️ son trabajo heredado que, por decisión del dueño, va en
  una funcionalidad de deuda técnica aparte. Están justificadas en Complexity Tracking.

**Re-check posterior a Phase 1** (data-model y contratos):
- Todos los procedimientos del contrato reciben `@NegocioId` o corren en un ámbito explícito.
- Ninguna ruta toma el NegocioId del cliente.
- No aparece SQL en C#.
- La matriz usa TVP.
- Todas las actualizaciones reciben `@RowVersion`.

**PASS**, con las mismas desviaciones justificadas.

## Project Structure

### Documentation (this feature)

```text
specs/001-negocios-usuarios-permisos/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── rutas-web.md
│   └── procedimientos-almacenados.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks (no lo crea /speckit-plan)
```

### Source Code (repository root)

```text
Electronic.Domain/
├── Entities/{Negocios,Usuarios,Roles,Permisos,Seguridad,Auditoria,Archivos,Plataforma}/*Model.cs
├── Repositories/{Negocios,Usuarios,Roles,Permisos,Autenticacion,Sesiones,Auditoria,Archivos,Plataforma}/I*Repository.cs
├── Common/ResultadoPaginado.cs
└── Excepciones/{ConflictoConcurrenciaException,ReglaNegocioException}.cs

Electronic.Application/
├── Common/
│   ├── Constants/{Messages,CodigosPermiso}.cs        # mensajes en español; códigos Modulo.Accion
│   ├── Contexto/IContextoUsuario.cs                  # NegocioId, cuenta, ámbito, permisos
│   └── Abstracciones/{IHasherContrasena,IGeneradorTokens,IServicioCorreo,IRelojUtc}.cs
├── DTO/{Negocios,Usuarios,Roles,Autenticacion,Auditoria,Plataforma}/*Dto.cs
├── Mapping/*Mapping.cs
├── Validators/*Validators.cs                          # incluye ImagenValidators (firma PNG/JPEG, ≤ 2 MB)
├── Services/{Negocios,Usuarios,Roles,Autenticacion,Auditoria,Plataforma}/I*Service.cs + *Service.cs
└── DependencyInjection.cs

Electronic.Infraestructure/                            # proyecto Electronic.Infrastructure
├── Data/{SqlConnectionFactory,AmbitoConexion,TraductorErroresSql}.cs
├── Repositories/<Modulo>/*Repository.cs
├── Seguridad/{HasherContrasena,GeneradorTokens}.cs
├── Correo/ServicioCorreoSmtp.cs
├── Cache/CatalogoCache.cs                             # permisos y configuración
├── Database/
│   ├── Migraciones.cs                                 # DbUp
│   └── Scripts/
│       ├── 0001_esquema_seguridad_y_tipos.sql
│       ├── 0002_tablas_plataforma.sql
│       ├── 0003_tablas_negocio.sql
│       ├── 0004_auditoria.sql
│       ├── 0005_rls.sql
│       ├── 0006_semilla_catalogo_permisos.sql
│       ├── 0007_semilla_configuracion_plantillas.sql
│       └── 0008…0014_usp_<modulo>.sql                 # CREATE OR ALTER por módulo
└── DependencyInjection.cs

Electronic.Web/
├── Controllers/{Cuenta,Plataforma,Soporte,Usuarios,Roles,Auditoria,Archivos}Controller.cs
├── Seguridad/{ContextoUsuarioWeb,EventosCookieSesion,RequierePermisoAttribute,FiltroAccesoDenegado}.cs
├── Models/<Modulo>/*ViewModel.cs
├── Views/{Cuenta,Plataforma,Usuarios,Roles,Auditoria,Shared}/*.cshtml
├── wwwroot/css/site.css · wwwroot/js/site.js         # las vistas nuevas no usan wwwroot/lib
└── Program.cs                                        # auth, antiforgery, rate limit, migraciones, manejador global

Electronic.Api/ · Electronic.Mcp/                     # solo registran un IContextoUsuario sin sesión (ámbito Anonimo)

Electronic.Application.Tests/Services/<Modulo>/*Tests.cs
Electronic.Infrastructure.Tests/                      # NUEVO: fixture DbUp + pruebas por procedimiento/RLS (omitidas sin PRUEBAS_SQL)
```

**Fuera de este módulo** (funcionalidad de deuda técnica, decisión del 2026-11-11):
- `.editorconfig` y `Directory.Build.props` con `TreatWarningsAsErrors`;
- corregir las advertencias de `ProductService`;
- borrar `wwwroot/lib`, `_ValidationScriptsPartial` y `Electronic.Tests`;
- agregar SQL Server al CI.

**Structure Decision**: se mantiene la solución por capas existente. El módulo agrega
subcarpetas por módulo en cada capa, como manda la sección "Estructura y convenciones" de la
constitución.

- **Electronic.Web** pasa a referenciar Application e Infrastructure, y es el único host con
  interfaz de este módulo.
- **Electronic.Infrastructure.Tests** es el único proyecto nuevo.
- **Las pantallas de plantillas** (roles sugeridos y correos) son la última historia del
  módulo (P3).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| `TreatWarningsAsErrors` y `.editorconfig` no se activan (IV) | Activarlos rompe la compilación por las advertencias heredadas de `ProductService`. El dueño decidió corregirlo en una funcionalidad de deuda técnica aparte | Activarlos aquí mezcla la corrección de Product con este módulo. Mientras tanto, la revisión del PR exige cero advertencias nuevas (R18) |
| `wwwroot/lib` (Bootstrap y jQuery) sigue en el repositorio (III) | Ninguna vista nueva lo usa. Borrarlo se agrupa con el resto de la deuda técnica | Borrarlo aquí amplía el PR con limpieza no relacionada |
| El catálogo de permisos se edita pero no se **crea** desde el sistema (IX, FR-038) | Un permiso solo tiene efecto si el código lo usa (`[RequierePermiso]`), así que cada módulo lo agrega por script al construirse. El Super Administrador edita nombres, descripciones y orden, y activa o desactiva | Permitir crear permisos desde la pantalla generaría permisos sin efecto, que confunden la matriz |
| `Electronic.Infrastructure.Tests` referencia `Electronic.Web` | La prueba por reflexión de SC-003 necesita los controladores Web, y corre sin SQL en el CI | Un tercer proyecto de pruebas solo para una prueba |
| Pruebas de integración fuera del CI (VIII) | Agregar SQL Server al CI va en la funcionalidad de deuda técnica. Mientras tanto, las pruebas se omiten sin `PRUEBAS_SQL` y se corren en local antes de cada PR | Hacerlas obligatorias hoy rompería el CI, que no tiene SQL Server. Mitigación: cada PR adjunta la salida de esas pruebas con `PRUEBAS_SQL` |
| Proyecto nuevo `Electronic.Infrastructure.Tests` | El Principio VIII exige pruebas de integración contra SQL Server y de aislamiento | Mezclarlas con las unitarias obligaría a tener SQL Server para cualquier `dotnet test` |
| `FrameworkReference Microsoft.AspNetCore.App` en Infrastructure | `PasswordHasher` e `IMemoryCache` están en el framework compartido. No es NuGet | Implementar PBKDF2 a mano crea un formato propio y es fácil equivocarse |
| Los controladores Web no devuelven `StatusCode(response.Code, response)` | Son MVC: traducen `ApiResponse<T>` a vista + `ModelState`. La convención es de los controladores del API | Devolver JSON desde Web obligaría a llamar desde JavaScript, lo que prohíbe el Principio III |
| Las filas de relación (`RolPermisos`, `UsuarioRoles`) se reemplazan con borrado físico | Son asignaciones, no registros de negocio. El historial completo queda en `Auditoria` | El borrado lógico en tablas de relación complica la unicidad y los permisos efectivos sin aportar historial |
| `Electronic.Api` y `Electronic.Mcp` siguen sin autorización por permiso (deuda heredada de Product) | Este módulo no toca sus controladores ni herramientas. Solo les registra un contexto anónimo para que compile la fábrica Scoped | Asegurar el API es un módulo propio (migrar Product) |
