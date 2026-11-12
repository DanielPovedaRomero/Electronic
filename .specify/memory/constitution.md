<!--
Sync Impact Report
==================
Cambio de versión: 1.0.0 → 2.0.0
(Incluye dos enmiendas del 2026-11-11. La segunda reemplaza las reglas de datos de la primera,
que nunca llegó a tener commit.)

Motivo (MAJOR): se redefine el Principio V.
  - Entity Framework y el SQL escrito en C# quedan prohibidos.
  - Todo acceso a datos es Dapper llamando procedimientos almacenados.
  - El prefijo de procedimientos cambia de SP_ a usp_, y la convención actual deja de cumplir.
  - Las obligaciones nuevas (RLS, rowversion, TVP, auditoría dentro del procedimiento, DbUp)
    hacen incumplir diseños que antes eran válidos.

Principios modificados:
  I. Arquitectura → se agrega: Dapper con procedimientos almacenados, exclusivamente.
  II. Aislamiento multinegocio → @NegocioId en todo procedimiento de negocio, más Row-Level
      Security con SESSION_CONTEXT.
  V. Integridad y eficiencia de datos → V. Acceso a datos con procedimientos almacenados e
     integridad
     - Eliminado: "si alguna vez se adopta EF Core, las lecturas usan AsNoTracking".
     - Agregado: las 11 reglas de la enmienda (repositorios Dapper, solo procedimientos
       almacenados con parámetros tipados, prefijo usp_, @NegocioId + RLS, plantilla de
       procedimiento, TVP, paginación @Pagina/@TamanoPagina, rowversion, auditoría en el
       procedimiento, fábrica de conexiones, scripts DbUp con CREATE OR ALTER).
     - Conservadas (no son de acceso a datos): DTOs fuera de la capa de aplicación, sin N+1,
       decimal(18,2), UTC, borrado lógico, columnas de creación/modificación y fotos en
       Archivos.

Secciones modificadas:
  Estructura y convenciones → stack (DbUp), patrón de repositorios, registro de dependencias
    (fábrica Scoped), contexto de negocio, scripts SQL y deuda heredada (prefijo SP_).
  Definición de terminado → el punto 5 exige el script numerado aplicado con DbUp.
Secciones añadidas / eliminadas: ninguna

Resuelto: TODO(MIGRACIONES), con DbUp (paquete autorizado: dbup-sqlserver).

Interpretaciones:
  - "Convención existente, si no usp_; nunca sp": la convención existente (SP_Product_*) usa el
    prefijo prohibido (SQL Server trata SP_ igual que sp_ con intercalación insensible a
    mayúsculas). Por eso aplica usp_ y SP_Product_* pasa a deuda heredada.
  - El autor de la auditoría y el negocio se toman de SESSION_CONTEXT, que establece la fábrica
    de conexiones.
  - Las operaciones de varios pasos son un solo procedimiento con transacción interna; no hay
    unidad de trabajo en C#.

Plantillas dependientes (leen la constitución en tiempo de ejecución; no modificadas):
  ✅ plan-template.md  ✅ spec-template.md  ✅ tasks-template.md

Ajuste adicional del 2026-11-11 (decisión del dueño del proyecto durante /speckit-plan):
  - I: solo se admiten paquetes NuGet gratuitos con licencia de código abierto permisiva.
    Prohibidos los paquetes de pago, con licencia comercial o con licencia dual restrictiva.
  - V: las fotos se guardan en su formato original (PNG o JPEG), sin conversión a WebP.

Diferido:
  - El módulo Product no cumple RLS, rowversion, NegocioId ni el prefijo usp_ (deuda heredada).
-->

# Electronic Constitution

## Core Principles

### I. Arquitectura por capas y stack fijo

- Todo módulo nuevo DEBE replicar la estructura y las convenciones descritas en
  "Estructura y convenciones de la solución". Ningún módulo inventa su propia organización.
- NO se agregan capas, patrones, frameworks, librerías ni paquetes NuGet nuevos sin enmendar
  antes esta constitución.
- Todo paquete NuGet DEBE ser gratuito y tener una licencia de código abierto permisiva (MIT,
  Apache-2.0 o BSD). Están prohibidos los paquetes de pago, con licencia comercial, con
  licencia dual o restringida por ingresos, o que requieran registro o clave.
- SOLID es obligatorio: una responsabilidad por clase; dependencias declaradas como interfaces
  y resueltas por inyección de dependencias en el constructor.
- Está prohibido instanciar servicios o repositorios con `new` fuera de los métodos de registro
  `DependencyInjection.cs` y de las pruebas. Está prohibido el estado estático mutable
  (clases `static` solo para constantes, extensiones de mapeo y validadores puros).
- Base de datos: SQL Server, exclusivamente.
- Acceso a datos: Dapper llamando procedimientos almacenados, exclusivamente.
  Entity Framework (cualquier versión), cualquier otro ORM y el SQL escrito en código C# están
  prohibidos (ver V).

**Razón**: una sola forma de construir módulos mantiene el código predecible para personas y
agentes, y evita que cada funcionalidad arrastre dependencias propias.

### II. Aislamiento multinegocio

- Toda tabla de negocio DEBE tener la columna `NegocioId`.
- Todo procedimiento almacenado de negocio DEBE recibir el parámetro `@NegocioId` del usuario
  actual y filtrar por él en cada consulta, inserción, actualización y borrado. El `NegocioId` se obtiene del contexto del usuario
  autenticado en el servidor, nunca de un parámetro enviado por el cliente.
- Además del filtro explícito, toda tabla de negocio DEBE estar protegida con Row-Level
  Security de SQL Server.
  - Su predicado compara `NegocioId` con `SESSION_CONTEXT(N'NegocioId')`.
  - La fábrica de conexiones establece ese valor (y el `UsuarioId` del autor), de solo
    lectura, al abrir cada conexión.
  - Una conexión sin `NegocioId` en sesión no ve filas de negocio.
  - Las operaciones de plataforma del Super Administrador usan una clave de sesión de
    plataforma explícita, reconocida por las políticas, y siempre quedan auditadas.
- Un usuario NUNCA puede ver ni modificar datos de otro negocio, ni un cliente los de otro
  cliente.

**Razón**: la fuga de datos entre negocios es el fallo más grave posible en un sistema
multinegocio. El filtro debe ser estructural, no opcional, y RLS es la segunda barrera si una
consulta olvida el filtro.

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

### V. Acceso a datos con procedimientos almacenados e integridad

**Acceso desde C#**

- Todo acceso a datos se hace con Dapper en la capa de infraestructura, mediante repositorios
  que implementan interfaces. Ninguna otra capa conoce la base de datos: ni tablas, ni
  procedimientos, ni tipos de SQL Server.
- Entity Framework, cualquier otro ORM y el SQL escrito en C# están prohibidos.
- Toda operación de datos es un procedimiento almacenado invocado con
  `CommandType.StoredProcedure` y parámetros tipados (`DynamicParameters` con `DbType`
  explícito).
- Las listas de valores (por ejemplo, los permisos de un rol) se envían como parámetros con
  valor de tabla (TVP), basados en tipos de tabla definidos en los scripts. Nunca como textos
  separados por comas.
- Las conexiones se obtienen siempre de la fábrica de conexiones inyectada
  (`IDbConnectionFactory`) y se cierran con `using`. Nunca hay conexiones estáticas ni
  compartidas entre solicitudes.

**Procedimientos almacenados**

- **Nombres**: `usp_<Entidad>_<Accion>` en español (por ejemplo, `usp_Usuarios_Crear`,
  `usp_Roles_Listar`). El prefijo `sp` (en cualquier combinación de mayúsculas y minúsculas)
  está prohibido.
- **Estructura**: todo procedimiento empieza con `SET NOCOUNT ON`, envuelve su cuerpo en
  `BEGIN TRY ... END TRY BEGIN CATCH ... THROW; END CATCH` y nunca oculta un error.
- **Transacción**: si modifica más de una tabla, el procedimiento abre su propia transacción
  (con `SET XACT_ABORT ON`), y la confirma o revierte completa. Una operación de varios pasos
  es un solo procedimiento, no una secuencia de llamadas desde C#.
- **SQL dinámico**: prohibido el SQL dinámico concatenado dentro de los procedimientos.
- **Columnas**: las consultas devuelven solo las columnas necesarias. `SELECT *` está prohibido.
- **Paginación**: todo listado recibe `@Pagina` y `@TamanoPagina` y pagina con
  `OFFSET ... FETCH NEXT ...` y un `ORDER BY` determinista. El procedimiento devuelve también
  el total de registros.
- **N+1**: prohibidas las consultas N+1. Los datos relacionados se obtienen en el mismo
  procedimiento.

**Concurrencia y auditoría**

- **Concurrencia**: toda tabla tiene una columna `rowversion`.
  - Los procedimientos de actualización reciben la versión leída y la validan en su `WHERE`.
  - Si la versión cambió, fallan con `THROW` usando un número de error reservado para
    conflictos de concurrencia. El repositorio lo traduce a un conflicto que se informa al
    usuario; nunca se sobrescribe en silencio.
- **Auditoría**: el mismo procedimiento que modifica los datos registra la auditoría, dentro de
  la misma transacción.
  - Registra autor (tomado de `SESSION_CONTEXT`), fecha UTC, entidad, acción, valor anterior y
    valor nuevo.
  - Si la auditoría falla, el cambio se revierte.
- Toda tabla guarda además quién creó y modificó cada registro, y cuándo.

**Scripts y migraciones**

- Tablas, índices, procedimientos, tipos de tabla, políticas RLS y datos semilla viven en
  scripts SQL versionados y numerados dentro del repositorio, y se aplican con DbUp.
- Nunca se aplican cambios manuales a la base de datos.
- Los procedimientos se escriben con `CREATE OR ALTER PROCEDURE`.
- Un script ya aplicado no se modifica: cualquier cambio, incluido el de un procedimiento, va
  en un script nuevo.
- Borrado lógico en lugar de borrado físico.
- Montos en `decimal(18,2)`, tanto en SQL como en C# (`decimal`). Fechas guardadas en UTC.
- Las entidades del dominio nunca salen de la capa de aplicación: los hosts (API, Web, MCP)
  reciben y devuelven siempre DTOs o ViewModels.
- Las fotos se guardan en la tabla `Archivos` (`VARBINARY(MAX)`) en su formato original: PNG o
  JPEG, sin conversión. Su tipo real se valida por la firma de bytes. Ninguna consulta de
  listado carga la columna de contenido.

**Razón**: con la lógica de datos en procedimientos almacenados, la seguridad y el rendimiento
dependen de que todos sigan la misma plantilla. Estas reglas evitan inyección SQL, fugas de
conexiones, transacciones a medias, pérdidas de actualizaciones, cambios sin auditar y
esquemas que divergen entre entornos.

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
Dapper con `Microsoft.Data.SqlClient`, DbUp (`dbup-sqlserver`) para scripts de esquema,
xUnit + Moq + coverlet. El API usa OpenAPI +
Scalar en desarrollo. El MCP usa el paquete `ModelContextProtocol` sobre stdio. Solución en
formato `Electronic.slnx`.

**Proyectos y dependencias** (siempre hacia adentro):

| Proyecto | Responsabilidad | Depende de |
|---|---|---|
| `Electronic.Domain` | Entidades (`<Nombre>Model`) e interfaces de repositorio (`I<Nombre>Repository`) | — |
| `Electronic.Application` | Servicios, DTOs, mapeos, validadores, `ApiResponse<T>`, mensajes, contexto del negocio actual | Domain |
| `Electronic.Infrastructure` (carpeta `Electronic.Infraestructure/`) | Repositorios Dapper, `IDbConnectionFactory`, scripts SQL (tablas, procedimientos, tipos de tabla, RLS, semillas) y DbUp | Application |
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
- `Infrastructure/Database/Scripts/NNNN_<descripcion>.sql`: scripts numerados con 4 dígitos,
  incluidos como recursos embebidos para DbUp

**Patrones obligatorios**:

- **Registro de dependencias**: cada capa expone un método de extensión en su
  `DependencyInjection.cs` (`AddApplication()`, `AddInfrastructure(IConfiguration)`). Todos
  los hosts llaman a esos métodos. Se registran como `Scoped` los servicios, los repositorios y
  la fábrica de conexiones (esta última porque depende del negocio del usuario de la
  solicitud).
- **Contexto del negocio actual**: una interfaz de la capa de aplicación expone el `NegocioId` y
  el usuario autenticado de la solicitud. Cada host la implementa a partir de su autenticación,
  y la fábrica de conexiones la usa para establecer `SESSION_CONTEXT` (Principio II).
- **Repositorios**: usan constructores primarios con `IDbConnectionFactory`.
  - Cada método abre su conexión con `using`.
  - Construye `DynamicParameters` con `DbType` explícito, incluidos `@NegocioId`, `@Pagina` y
    `@TamanoPagina` en listados, `@RowVersion` en actualizaciones, y los TVP necesarios.
  - Ejecuta un procedimiento `usp_<Entidad>_<Accion>` mediante `CommandDefinition`, con
    `CommandType.StoredProcedure` y el `CancellationToken`.
  - Traduce el número de error de concurrencia a un conflicto.
- **Scripts SQL**: tablas, procedimientos, tipos de tabla, políticas RLS y datos semilla de cada
  módulo van en scripts numerados que se aplican con DbUp.
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
  excepción. Además no tiene `NegocioId`, RLS, `rowversion`, paginación, borrado lógico,
  auditoría ni autorización.
- Sus procedimientos `SP_Product_*` usan el prefijo prohibido y no tienen scripts en el
  repositorio.
- La fábrica de conexiones está registrada como `Singleton` y no establece `SESSION_CONTEXT`.
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
5. Los cambios de esquema y de datos semilla van en su script SQL numerado, y DbUp lo aplica
   sin errores sobre una base de datos vacía.

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
  Agregar un patrón, capa, framework, librería o paquete NuGet requiere enmienda previa, y el
  paquete debe cumplir la regla de licencias del Principio I.
- **Versionado** (semántico):
  - MAJOR: se elimina un principio o se redefine de forma incompatible.
  - MINOR: se agrega un principio o una sección, o una regla se amplía de forma sustancial.
  - PATCH: aclaraciones y redacción.
- **Cumplimiento**: toda revisión de PR verifica estos principios. Toda excepción se justifica
  por escrito en la sección "Complexity Tracking" del plan de la funcionalidad. Una excepción
  no justificada bloquea la integración.

**Version**: 2.0.0 | **Ratified**: 2026-11-10 | **Last Amended**: 2026-11-11
