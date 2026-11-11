<!--
Sync Impact Report
==================
Cambio de versión: (plantilla sin ratificar) → 1.0.0
Motivo: primera adopción de la constitución; todos los marcadores reemplazados.

Principios definidos (la plantilla tenía 5 marcadores genéricos; se definen 9):
  [PRINCIPLE_1_NAME] → I. Arquitectura por capas y stack fijo
  [PRINCIPLE_2_NAME] → II. Aislamiento multinegocio
  [PRINCIPLE_3_NAME] → III. Frontend renderizado en servidor, sin dependencias
  [PRINCIPLE_4_NAME] → IV. Calidad de código sin advertencias
  [PRINCIPLE_5_NAME] → V. Integridad y eficiencia de datos
  (nuevo)            → VI. Seguridad por permisos
  (nuevo)            → VII. Errores y registros observables
  (nuevo)            → VIII. Pruebas obligatorias
  (nuevo)            → IX. Catálogos y configuración desde base de datos

Secciones añadidas:
  [SECTION_2_NAME] → Estructura y convenciones de la solución
  [SECTION_3_NAME] → Definición de terminado y flujo de trabajo
  Governance       → reglas de enmienda, versionado y cumplimiento
Secciones eliminadas: ninguna

Plantillas dependientes (leen la constitución en tiempo de ejecución; no modificadas aquí):
  ✅ .specify/templates/plan-template.md   (su "Constitution Check" usa estos principios)
  ✅ .specify/templates/spec-template.md   (sin cambios necesarios)
  ✅ .specify/templates/tasks-template.md  (sin cambios necesarios)

Interpretaciones y TODO diferidos:
  - El stack actual es Dapper + procedimientos almacenados (no EF Core). Las reglas de
    "AsNoTracking" y "migraciones" se tradujeron a su equivalente con Dapper (V).
    TODO(MIGRACIONES): elegir la herramienta de migraciones SQL versionadas (requiere enmienda).
  - El código existente (módulo Product, Electronic.Web) incumple varias reglas; ver
    "Deuda heredada" en la sección de estructura.
-->

# Electronic Constitution

## Core Principles

### I. Arquitectura por capas y stack fijo

- Todo módulo nuevo DEBE replicar la estructura y las convenciones descritas en
  "Estructura y convenciones de la solución". Ningún módulo inventa su propia organización.
- NO se agregan capas, patrones, frameworks, librerías ni paquetes NuGet nuevos sin enmendar
  antes esta constitución.
- SOLID es obligatorio: una responsabilidad por clase; dependencias declaradas como interfaces
  y resueltas por inyección de dependencias en el constructor.
- Está prohibido instanciar servicios o repositorios con `new` fuera de los métodos de registro
  `DependencyInjection.cs` y de las pruebas. Está prohibido el estado estático mutable
  (clases `static` solo para constantes, extensiones de mapeo y validadores puros).
- Base de datos: SQL Server, exclusivamente.

**Razón**: una sola forma de construir módulos mantiene el código predecible para personas y
agentes, y evita que cada funcionalidad arrastre dependencias propias.

### II. Aislamiento multinegocio

- Toda tabla de negocio DEBE tener la columna `NegocioId`.
- Toda consulta, inserción, actualización y borrado DEBE filtrar por el `NegocioId` del usuario
  actual. El `NegocioId` se obtiene del contexto del usuario autenticado en el servidor, nunca
  de un parámetro enviado por el cliente.
- Un usuario NUNCA puede ver ni modificar datos de otro negocio, ni un cliente los de otro
  cliente.

**Razón**: la fuga de datos entre negocios es el fallo más grave posible en un sistema
multinegocio; el filtro debe ser estructural, no opcional.

### III. Frontend renderizado en servidor, sin dependencias

- HTML, CSS y JavaScript puros. Sin frameworks ni librerías de terceros (incluidos Bootstrap
  y jQuery).
- Las páginas se renderizan en el servidor (ASP.NET Core MVC con vistas Razor). Los
  controladores web consumen los servicios de la capa de aplicación por inyección de
  dependencias, igual que los demás hosts.
- El JavaScript NO llama al API ni contiene lógica de negocio. Solo maneja interacción de
  interfaz: mostrar/ocultar, validación visual y vista previa de fotos.
- El CSS usa variables (custom properties) para colores y espaciados. No hay estilos en línea.
- El diseño DEBE ser responsive (celular, tablet y escritorio) y accesible: etiqueta en todos
  los campos, texto alternativo en todas las imágenes y navegación completa con teclado.
- Toda la interfaz y todos los textos visibles están en español.

**Razón**: sin dependencias de terceros no hay actualizaciones forzadas ni superficie de
ataque añadida, y la lógica queda en un solo lugar: el servidor.

### IV. Calidad de código sin advertencias

- Sin números ni textos mágicos. Los valores de negocio vienen de catálogos en base de datos
  (ver IX). Las constantes y los enums solo se permiten para valores técnicos internos que el
  usuario nunca configura (nombres de procedimientos, nombres de políticas, claves de
  configuración, mensajes del sistema).
- Sin código muerto: ningún método, clase, variable, `using`, referencia de proyecto ni
  paquete NuGet sin usar.
- Todo proyecto DEBE tener `<Nullable>enable</Nullable>` y
  `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- La solución DEBE tener un `.editorconfig` con los analizadores de .NET activos; el código
  compila sin advertencias.
- Métodos cortos y con un solo propósito.
- Los conceptos del dominio se nombran en español (`Orden`, `Negocio`, `MetodoPago`).
  Los sufijos técnicos siguen la convención existente (`Service`, `Repository`, `Dto`, `Model`).
- Sin comentarios que expliquen lo que el código ya dice.
- Todo acceso a base de datos, archivos o correo es `async`/`await` y recibe un
  `CancellationToken` que se propaga hasta la llamada final.

**Razón**: tratar las advertencias como errores convierte la calidad en una condición de
compilación en vez de una revisión manual.

### V. Integridad y eficiencia de datos

- Las entidades del dominio nunca salen de la capa de aplicación: los hosts (API, Web, MCP)
  reciben y devuelven siempre DTOs o ViewModels.
- Las consultas de lectura devuelven solo las columnas necesarias. Con Dapper no hay
  seguimiento de entidades; si alguna vez se adopta EF Core, las lecturas usan `AsNoTracking`.
- Todo listado DEBE estar paginado en la base de datos. Prohibidas las consultas N+1: los
  datos relacionados se obtienen en la misma consulta o en un solo lote.
- Montos en `decimal(18,2)`, tanto en SQL como en C# (`decimal`). Fechas guardadas en UTC.
- Los cambios de esquema (tablas, índices, procedimientos almacenados) se hacen solo mediante
  scripts de migración versionados en el repositorio. Nunca se aplican cambios manuales.
- Las operaciones de varios pasos se ejecutan dentro de una transacción.
- Las órdenes, el inventario y los pedidos tienen control de concurrencia optimista con
  `rowversion`. Un conflicto se informa al usuario; nunca se sobrescribe en silencio.
- Borrado lógico en lugar de borrado físico.
- Todo registro guarda la auditoría de quién lo creó y modificó, y cuándo.
- Las fotos se guardan en la tabla `Archivos` (`VARBINARY(MAX)`), comprimidas a WebP. Ninguna
  consulta de listado carga la columna de contenido.

**Razón**: estas reglas evitan los fallos de datos más costosos: pérdidas de actualizaciones,
consultas lentas, borrados irrecuperables y fugas del modelo interno.

### VI. Seguridad por permisos

- Toda acción de controlador exige autorización por un permiso con formato `Módulo.Acción`.
  Nada es público salvo la tienda, y lo público se marca de forma explícita.
- Toda entrada se valida en el servidor, aunque también se valide en el navegador.
- Los formularios usan protección anti-forgery.
- Las contraseñas se guardan con un hash seguro y salado (por ejemplo, `PasswordHasher` de
  ASP.NET Core Identity). Nunca en texto plano ni con hash reversible.
- Los secretos y las cadenas de conexión están fuera del código y de los `appsettings.json`
  versionados: se usan user secrets en desarrollo y variables de entorno en los demás entornos.
- Los archivos subidos se validan por tipo real y por tamaño, y se respeta el límite de fotos
  por etapa definido en su catálogo.
- El limitador de solicitudes por IP vigente en el API se mantiene en todos los controladores.

**Razón**: en un sistema multinegocio, cada acción sin permiso es una posible fuga de datos.

### VII. Errores y registros observables

- Hay un manejo global de excepciones en cada host. Ninguna excepción se oculta con un `catch`
  vacío ni con un `catch` que la descarte sin registrarla.
- El usuario recibe mensajes amigables en español. El detalle técnico va solo al log.
- Se usa logging estructurado (`ILogger<T>` con plantillas de mensaje y parámetros con nombre)
  para los errores y las acciones importantes.
- Nunca se registran datos sensibles: contraseñas, tokens, cadenas de conexión ni contenido
  de archivos.
- En el host MCP, los logs van solo a stderr; stdout está reservado para el protocolo.

**Razón**: un error que no se registra no se puede diagnosticar.

### VIII. Pruebas obligatorias

- Toda regla de negocio lleva pruebas unitarias con xUnit y Moq, el stack que ya usa el
  proyecto, ubicadas en el proyecto de pruebas de su capa.
- El acceso a datos y el aislamiento entre negocios llevan pruebas de integración contra
  SQL Server. Toda consulta nueva DEBE tener una prueba que demuestre que no devuelve datos
  de otro `NegocioId`.
- Cobertura mínima del 80% en las capas de dominio y aplicación, medida con coverlet.

**Razón**: las reglas de negocio y el aislamiento son lo más caro de romper; las pruebas son
la única prueba objetiva de que se cumplen.

### IX. Catálogos y configuración desde base de datos

- Todo catálogo y toda configuración de negocio se cargan desde la base de datos, nunca desde
  código, clases, enums ni archivos de configuración. Ejemplos: estados de orden, roles,
  permisos, categorías, métodos de pago, tipos de equipo, condiciones de producto, etapas y
  límites de fotos, plantillas y textos de correo, datos del negocio.
- Ningún rol ni estado está fijo en el código: todo es configurable por negocio.
- Cada catálogo es administrable desde el sistema (crear, editar, activar/desactivar) por
  usuarios con permiso.
- Cada catálogo pertenece a un `NegocioId`, salvo los catálogos globales de la plataforma.
- Los valores iniciales se cargan como datos semilla en las migraciones, no en la lógica.
- El código nunca compara contra textos ni IDs fijos de un catálogo. Cuando una regla necesita
  identificar un valor (por ejemplo, el estado "Entregado"), el catálogo tiene un campo
  `Codigo` o una bandera de comportamiento (`EsFinal`, `RequiereAprobacion`, `DescuentaStock`),
  y la lógica usa esa bandera.
- Los catálogos de uso frecuente se guardan en caché en memoria (`IMemoryCache`), y la entrada
  se invalida cuando el catálogo cambia.

**Razón**: cada negocio opera distinto; fijar valores en el código obliga a desplegar para
cambiar una regla que el negocio debería poder ajustar solo.

## Estructura y convenciones de la solución

Esta sección documenta la estructura vigente. Es la referencia que todo módulo nuevo replica
(Principio I).

**Stack**: .NET 10 (`net10.0`), C# con `ImplicitUsings` y `Nullable` habilitados, SQL Server,
Dapper con `Microsoft.Data.SqlClient`, xUnit + Moq + coverlet. El API usa OpenAPI +
Scalar en desarrollo. El MCP usa el paquete `ModelContextProtocol` sobre stdio. Solución en
formato `Electronic.slnx`.

**Proyectos y dependencias** (siempre hacia adentro):

| Proyecto | Responsabilidad | Depende de |
|---|---|---|
| `Electronic.Domain` | Entidades (`<Nombre>Model`) e interfaces de repositorio (`I<Nombre>Repository`) | — |
| `Electronic.Application` | Servicios, DTOs, mapeos, validadores, `ApiResponse<T>`, mensajes | Domain |
| `Electronic.Infrastructure` (carpeta `Electronic.Infraestructure/`) | Repositorios Dapper, `IDbConnectionFactory` | Application |
| `Electronic.Api` | Controladores REST, rate limiting | Application, Infrastructure |
| `Electronic.Mcp` | Herramientas MCP sobre los servicios | Application, Infrastructure |
| `Electronic.Web` | Vistas Razor (Principio III) | Application, Infrastructure |
| `Electronic.Application.Tests` | Pruebas unitarias de servicios | Application, Domain |

**Organización por módulo**: cada módulo crea una subcarpeta con su nombre en cada capa y en
cada tipo de componente:

- `Domain/Entities/<Modulo>/<Nombre>Model.cs`
- `Domain/Repositories/<Modulo>/I<Nombre>Repository.cs`
- `Application/Services/<Modulo>/I<Nombre>Service.cs` y `<Nombre>Service.cs`
- `Application/DTO/<Nombre>Dto.cs`
- `Application/Mapping/<Nombre>Mapping.cs`: métodos de extensión `ToDto`, `ToModel` y `ToDtoList`
- `Application/Validators/<Nombre>Validators.cs`: `static string? Validate(dto)` que devuelve
  el primer mensaje de error, o `null` si el DTO es válido
- `Infrastructure/Repositories/<Modulo>/<Nombre>Repository.cs`

**Patrones obligatorios**:

- **Registro de dependencias**: cada capa expone un método de extensión en su
  `DependencyInjection.cs` (`AddApplication()`, `AddInfrastructure(IConfiguration)`). Los
  servicios y repositorios se registran como `Scoped`, y la fábrica de conexiones como
  `Singleton`. Todos los hosts llaman a esos métodos.
- **Repositorios**: usan constructores primarios con `IDbConnectionFactory`. Cada método abre
  su conexión con `using`, construye `DynamicParameters` con `DbType` explícito y ejecuta un
  procedimiento almacenado `SP_<Entidad>_<Accion>` mediante `CommandDefinition`, con
  `CommandType.StoredProcedure` y el `CancellationToken`. No hay SQL en línea.
- **Servicios**: devuelven siempre `ApiResponse<T>` (`Code`, `Message`, `Result`), creado con
  `ApiResponse<T>.Success(...)` o `ApiResponse<T>.Fail(HttpStatusCode, mensaje)`.
  - Los casos previstos se convierten en respuestas: validación y duplicados dan 400; un
    recurso inexistente al consultarlo da 404.
  - Las excepciones inesperadas NO se ocultan (Principio VII).
- **Controladores**: son delgados. Llaman al servicio y devuelven
  `StatusCode(response.Code, response)`. No contienen lógica.
- **Mensajes**: son constantes de formato en `Common/Constants/Messages.cs` de su capa, y se
  usan con `string.Format`. Los mensajes visibles al usuario están en español.
- **Configuración técnica**: los nombres de claves se definen en `Constants/Config.cs`.
- **Herramientas MCP**: son clases `[McpServerToolType]` con métodos
  `[McpServerTool, Description(...)]` que delegan en los servicios de aplicación.

**Deuda heredada** (código previo a esta constitución; se corrige al tocarlo y no sirve de
precedente):

- El módulo Product tiene nombres en inglés, textos en inglés y `catch` que descartan la
  excepción. Además no tiene `NegocioId`, paginación, borrado lógico, auditoría ni
  autorización.
- `Electronic.Web` usa Bootstrap y jQuery y no referencia otras capas.
- `Electronic.Mcp/appsettings.json` contiene una cadena de conexión.
- No existen todavía `.editorconfig` ni `TreatWarningsAsErrors`.
- `Electronic.Tests`, `WeatherForecast.cs` y `Electronic.Api.http` son código muerto.

## Definición de terminado y flujo de trabajo

Una tarea está terminada solo si cumple todas estas condiciones:

1. `dotnet build` compila sin advertencias.
2. `dotnet test` pasa todas las pruebas, y la cobertura de dominio y aplicación es ≥ 80%.
3. Respeta cada principio de esta constitución.
4. No deja código, archivos, referencias ni paquetes sin usar.
5. Los cambios de esquema van acompañados de su script de migración versionado.

**Flujo de trabajo**:

- El trabajo se hace en ramas `feature/<descripcion>` o `feat/<descripcion>` y se integra a
  `main` por pull request.
- El CI (`.github/workflows/CI.yml`) compila y prueba cada PR. Cada push a `main` publica el
  API y genera un release.
- Las funcionalidades se especifican con Spec Kit (`/speckit-specify`, `/speckit-plan`,
  `/speckit-tasks`). El plan DEBE pasar el "Constitution Check" antes de implementarse.

## Governance

- Esta constitución prevalece sobre cualquier otra práctica, guía o costumbre del proyecto.
  `CLAUDE.md` es la guía operativa en tiempo de ejecución y no puede contradecirla.
- **Enmiendas**: solo mediante pull request que modifique este archivo. El PR explica el motivo,
  el impacto en el código existente y el plan de migración si alguna regla deja de cumplirse.
  Agregar un patrón, capa, framework, librería o paquete NuGet requiere enmienda previa.
- **Versionado** (semántico):
  - MAJOR: se elimina un principio o se redefine de forma incompatible.
  - MINOR: se agrega un principio o una sección, o una regla se amplía de forma sustancial.
  - PATCH: aclaraciones y redacción.
- **Cumplimiento**: toda revisión de PR verifica estos principios. Toda excepción se justifica
  por escrito en la sección "Complexity Tracking" del plan de la funcionalidad. Una excepción
  no justificada bloquea la integración.

**Version**: 1.0.0 | **Ratified**: 2026-11-10 | **Last Amended**: 2026-11-10
