---

description: "Task list for 001-negocios-usuarios-permisos"
---

# Tasks: Negocios, usuarios, roles y permisos

**Input**: Design documents from `/specs/001-negocios-usuarios-permisos/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: se incluyen porque la constitución los exige (Principio VIII):
- **Unitarias**: xUnit + Moq en `Electronic.Application.Tests`.
- **Integración**: SQL Server en `Electronic.Infrastructure.Tests`. Se omiten si no existe
  `PRUEBAS_SQL`.
- **Orden**: escribirlas primero y comprobar que fallan antes de implementar.

**Organization**: tareas agrupadas por historia de usuario.
- **Orden de fases**: US2 (autenticación) va antes que US1 (negocios), porque todas las
  pantallas requieren sesión. Ambas son P1 y forman juntas el MVP.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede ejecutar en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia a la que pertenece (US1…US7)

## Path Conventions

| Capa | Carpeta |
|---|---|
| Domain | `Electronic.Domain/` |
| Application | `Electronic.Application/` |
| Infrastructure | `Electronic.Infraestructure/` (proyecto `Electronic.Infrastructure`) |
| Scripts SQL | `Electronic.Infraestructure/Database/Scripts/` |
| Web | `Electronic.Web/` |
| Pruebas | `Electronic.Application.Tests/` y `Electronic.Infrastructure.Tests/` |

## Reglas que aplican a TODAS las tareas (constitución 2.0.0)

- **Procedimientos**:
  - Nombre `usp_<Entidad>_<Accion>`, escrito con `CREATE OR ALTER`.
  - Plantilla: `SET NOCOUNT ON`, `TRY/CATCH` + `THROW`, y `SET XACT_ABORT ON` + transacción
    cuando modifica varias tablas.
  - Sin `SELECT *` y sin SQL dinámico concatenado.
  - Los de negocio reciben `@NegocioId` y filtran por él.
  - Los de actualización reciben `@RowVersion` y lanzan `THROW 50001` si cambió. Las
    actualizaciones de sistema (sesiones, intentos, tokens) leen la versión con
    `UPDLOCK, HOLDLOCK` y la validan.
  - **Todas las tablas** llevan `RowVersion` y las columnas de creación y modificación, y todo
    `INSERT` o `UPDATE` las llena desde `SESSION_CONTEXT`.
  - Todo cambio llama a `usp_Auditoria_Registrar` dentro de la transacción.
  - Los listados reciben `@Pagina` y `@TamanoPagina`, paginan con `OFFSET/FETCH` y devuelven
    `TotalRegistros` con `COUNT(*) OVER()`.
  - Números de error: [contracts/procedimientos-almacenados.md](contracts/procedimientos-almacenados.md#errores).
- **Repositorios**:
  - Constructor primario con `IDbConnectionFactory`, conexión con `using`.
  - `DynamicParameters` con `DbType` explícito; las listas viajan como TVP `dbo.TipoListaIds`.
  - `CommandDefinition` con `CommandType.StoredProcedure` y `CancellationToken`.
  - Las `SqlException` se traducen con `TraductorErroresSql`.
- **Servicios**:
  - Devuelven `ApiResponse<T>` con mensajes en español de `Common/Constants/Messages.cs`.
  - Solo atrapan `ConflictoConcurrenciaException` (409) y `ReglaNegocioException` (400/404).
    No hay `catch (Exception)`.
- **Web**:
  - Controladores MVC con `[RequierePermiso("Modulo.Accion")]` o `[SoloPlataforma]`.
  - Formularios con antiforgery.
  - Vistas sin Bootstrap ni jQuery, con etiquetas en todos los campos y `alt` en las imágenes.
- **Nombres**: dominio en español; sufijos `Model`, `Dto`, `Service`, `Repository`.
- **Async**: todo es `async` con `CancellationToken`.
- **Advertencias**: ninguna advertencia nueva en `dotnet build`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: dependencias, referencias y proyecto de pruebas de integración

- [ ] T001 En `Electronic.Infraestructure/Electronic.Infrastructure.csproj`:
  - agregar `PackageReference dbup-sqlserver` (última estable, licencia MIT);
  - agregar `<FrameworkReference Include="Microsoft.AspNetCore.App" />`;
  - agregar `<EmbeddedResource Include="Database\Scripts\*.sql" />`;
  - quitar `System.Data.SqlClient`, que no se usa (código muerto).
- [ ] T002 En `Electronic.Web/Electronic.Web.csproj`: agregar `ProjectReference` a
  `..\Electronic.Application\Electronic.Application.csproj` y a
  `..\Electronic.Infraestructure\Electronic.Infrastructure.csproj`, y un `UserSecretsId`.
- [ ] T003 Crear el proyecto xUnit `Electronic.Infrastructure.Tests/Electronic.Infrastructure.Tests.csproj`
  con `net10.0`, `Nullable` y los mismos paquetes de prueba que `Electronic.Application.Tests`
  (sin Moq). Debe referenciar Domain, Application e Infrastructure. Agregarlo a `Electronic.slnx`
  dentro de `/Tests/`.
- [ ] T004 [P] Crear `Electronic.Infrastructure.Tests/Infraestructura/RequiereSqlFactAttribute.cs`:
  un atributo que hereda de `FactAttribute` y asigna `Skip = "Defina PRUEBAS_SQL para ejecutar
  pruebas de integración"` cuando la variable de entorno no existe.
- [ ] T005 [P] Crear `Electronic.Infrastructure.Tests/Infraestructura/BaseDatosFixture.cs`. Debe:
  - crear una base de datos temporal (`ElectronicPruebas_<guid>`) desde `PRUEBAS_SQL`;
  - aplicar `Migraciones.AplicarMigraciones`;
  - exponer helpers para abrir conexiones con el ámbito, el NegocioId y el UsuarioId indicados;
  - borrar la base en `DisposeAsync`.
- [ ] T006 [P] Agregar a `Electronic.Web/appsettings.json` las claves vacías
  `ConnectionStrings:DBConnection`, `ConnectionStrings:DBMigraciones`,
  `Migraciones:AplicarAlIniciar` (false), `Instalacion:CorreoSuperAdministrador` y
  `Smtp:{Host,Puerto,Usuario,Contrasena,Remitente,UsarSsl}`. Ningún secreto con valor.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: esquema, RLS, auditoría, fábrica de conexiones con `SESSION_CONTEXT`, sesiones
validadas en cada solicitud, autorización por permiso y layout base.

**⚠️ CRITICAL**: ninguna historia puede empezar hasta terminar esta fase.

### Base de datos

- [ ] T007 Crear `Database/Scripts/0001_esquema_seguridad_y_tipos.sql` con:
  - `CREATE SCHEMA seguridad`;
  - `CREATE TYPE dbo.TipoListaIds AS TABLE (Id INT NOT NULL PRIMARY KEY)`.
- [ ] T008 Crear `Database/Scripts/0002_tablas_plataforma.sql` según
  [data-model.md](data-model.md#entidades-de-plataforma-sin-negocioid).
  - **Columnas comunes**: **todas las tablas** de este script, incluidas `ControlIntentos`,
    `Sesiones`, `TokensSeguridad`, los catálogos, las plantillas y `PlantillaRolPermisos`,
    llevan `RowVersion ROWVERSION` y `CreadoPorId`, `CreadoPorTipo CHAR(1)`, `CreadoEnUtc`,
    `ModificadoPorId NULL`, `ModificadoPorTipo NULL` y `ModificadoEnUtc NULL` (constitución V).
  - **`Negocios`**:
    - `Nombre NVARCHAR(150)` obligatorio, `Nit NVARCHAR(30)` "único en la plataforma",
      `CorreoContacto NVARCHAR(256)` obligatorio;
    - `ColorPrimario` y `ColorSecundario CHAR(7)`;
    - `TextoAgradecimiento NVARCHAR(1000)`, `TextoCondiciones NVARCHAR(4000)`;
    - `DiasResguardo INT CHECK (>= 0)`, `LogoArchivoId INT NULL`, `Activo BIT`.
  - **`SuperAdministradores`**: `Nombre`, `Correo`, `CorreoNormalizado` (único),
    `HashContrasena NVARCHAR(500) NULL` ("NULL = pendiente"), `Activo`.
  - **Catálogo de permisos**:
    - `Modulos` (Codigo único, `Nombre NVARCHAR(100)`, `Descripcion NVARCHAR(500)`, Orden,
      `Activo`);
    - `Acciones` (Codigo único, `Nombre NVARCHAR(100)`, Orden, `EsConsulta BIT`, `Activo`);
    - `Permisos` (ModuloId, AccionId, Codigo único, `Descripcion NVARCHAR(500)`,
      `EsAdministracion BIT`, `Activo`, UNIQUE(ModuloId, AccionId)).
  - **Plantillas**: `PlantillasRol` (Nombre, Descripcion, EsAccesoTotal, Orden, Activo),
    `PlantillaRolPermisos` y `PlantillasCorreo` (Codigo único, `Asunto NVARCHAR(200)`,
    `CuerpoHtml NVARCHAR(MAX)`).
  - **`ConfiguracionSeguridad`**: una sola fila, con CHECK de rangos:
    - MaxIntentosFallidos 1–20, MinutosBloqueo 1–1440;
    - MinutosEnlaceRecuperacion 5–1440, HorasEnlaceInvitacion 1–720;
    - MinutosCodigoSegundoFactor 1–60, MinutosInactividadSesion 5–720;
    - LongitudMinimaContrasena 8–128.
  - **Seguridad de sesión**:
    - `ControlIntentos` (`CorreoNormalizado` PK, IntentosFallidos, `BloqueadoHastaUtc NULL`);
    - `Sesiones` (Id BIGINT, `HashIdentificador BINARY(32)` único, CuentaId, `CuentaTipo CHAR(1)`,
      `NegocioId NULL`, CreadaEnUtc, UltimaActividadUtc, `RevocadaEnUtc NULL`);
    - `TokensSeguridad` (Tipo, CuentaId, CuentaTipo, `HashToken BINARY(32)`, ExpiraEnUtc,
      `UsadoEnUtc NULL`).
- [ ] T009 Crear `Database/Scripts/0003_tablas_negocio.sql`. Todas las tablas, incluidas las
  relaciones `RolPermisos` y `UsuarioRoles`, llevan `NegocioId`, `RowVersion` y las columnas de
  creación y modificación.
  - **`Usuarios`**: `Nombre NVARCHAR(150)`, `Correo` y `CorreoNormalizado NVARCHAR(256)` con
    UNIQUE(NegocioId, CorreoNormalizado), `Telefono NVARCHAR(30)`,
    `HashContrasena NVARCHAR(500) NULL`, `Activo`.
  - **`Roles`**: `Nombre NVARCHAR(100)` con UNIQUE(NegocioId, Nombre), `Descripcion NVARCHAR(500)`,
    `EsAccesoTotal BIT`, `Activo`.
  - **Relaciones**: `RolPermisos` (NegocioId, RolId, PermisoId, PK(RolId, PermisoId)) y
    `UsuarioRoles` (NegocioId, UsuarioId, RolId, PK(UsuarioId, RolId)).
  - **`Archivos`**: `NombreOriginal NVARCHAR(255)`, `TipoContenido` ("`image/png` o
    `image/jpeg`"), `TamanoBytes`, `Contenido VARBINARY(MAX)`.
  - **FK**: `Negocios.LogoArchivoId → Archivos`.
- [ ] T010 Crear `Database/Scripts/0004_auditoria.sql` con:
  - la tabla `Auditoria`: Id BIGINT, `NegocioId NULL`, FechaUtc, AutorId, AutorTipo, AutorNombre,
    Entidad, EntidadId, Accion, `Cambios NVARCHAR(MAX)` JSON, más `RowVersion` y las columnas
    de creación y modificación (las de modificación siempre NULL, porque es de solo inserción);
  - los índices (NegocioId, FechaUtc DESC), (NegocioId, Entidad, EntidadId) y (NegocioId, AutorId);
  - `usp_Auditoria_Registrar`, que lee el autor de `SESSION_CONTEXT(N'UsuarioId')` y
    `(N'AutorTipo')`, resuelve AutorNombre y usa `A` para anónimo;
  - `usp_Auditoria_RegistrarEvento`.
- [ ] T011 Crear `Database/Scripts/0005_rls.sql` con:
  - `seguridad.fn_PredicadoNegocio(@NegocioId)`. Permite la fila si `Ambito = 'Negocio'` y el
    NegocioId coincide con `SESSION_CONTEXT(N'NegocioId')`, o si
    `Ambito IN ('Plataforma','Autenticacion')`;
  - políticas FILTER + BLOCK AFTER INSERT/AFTER UPDATE en `Usuarios`, `Roles`, `RolPermisos`,
    `UsuarioRoles`, `Archivos` y `Auditoria` (con `NegocioId NULL`: solo Plataforma), y en
    `Negocios` sobre `Id` ([research R5](research.md#r5-row-level-security-y-contexto-de-sesión));
  - un rol de base de datos `rol_aplicacion` con solo `GRANT EXECUTE`, y `DENY UPDATE, DELETE`
    sobre `Auditoria`.
- [ ] T012 [P] Crear `Database/Scripts/0006_semilla_catalogo_permisos.sql` con:
  - los módulos Usuarios, Roles y Auditoria;
  - las acciones Ver (`EsConsulta = 1`), Crear, Editar y CambiarEstado;
  - los permisos y las banderas `EsAdministracion` de [research R9](research.md#r9-catálogo-de-permisos-acción-de-consulta-y-acceso-total).
- [ ] T013 [P] Crear `Database/Scripts/0007_semilla_configuracion_plantillas.sql` con:
  - la fila de `ConfiguracionSeguridad` con los valores por defecto (5, 15, 60, 72, 10, 30, 8);
  - `PlantillasRol` Administrador (`EsAccesoTotal = 1`), y Recepción, Técnico y Bodega **sin
    filas en `PlantillaRolPermisos`** (sin permisos; los módulos futuros se los agregan);
  - `PlantillasCorreo` `Invitacion`, `Recuperacion` y `SegundoFactor`, en español, con los
    marcadores `{{Nombre}}`, `{{Negocio}}`, `{{Enlace}}`, `{{Codigo}}` y `{{Vence}}`.
- [ ] T014 Crear `Database/Scripts/0008_usp_sesiones_catalogo.sql` con:
  - `usp_Sesiones_Crear`, `usp_Sesiones_Revocar` y `usp_Sesiones_RevocarOtras`;
  - `usp_Sesiones_Validar`. Comprueba que la sesión no esté revocada, que no supere
    `MinutosInactividadSesion` y que el usuario y el negocio estén activos. Devuelve la cuenta,
    el NegocioId, el nombre, los colores y el LogoArchivoId del negocio y los permisos efectivos:
    - todos los permisos **activos** de módulos activos si hay un rol activo con `EsAccesoTotal`
      o si es un Super Administrador;
    - si no, la unión de los roles activos, filtrada a permisos y módulos activos.

    Actualiza `UltimaActividadUtc` si pasó más de 1 minuto. Primero lee la `RowVersion` con
    `WITH (UPDLOCK, HOLDLOCK)` y la valida en el `UPDATE`;
  - `usp_Permisos_ObtenerCatalogo`, `usp_Configuracion_Obtener` y `usp_PlantillasCorreo_Obtener`.
- [ ] T015 Crear `Electronic.Infraestructure/Database/Migraciones.cs` con
  `static void AplicarMigraciones(string cadenaConexion)`, usando DbUp:
  `DeployChanges.To.SqlDatabase`, `EnsureDatabase`, scripts embebidos ordenados y journal en
  `dbo.SchemaVersions`. Si falla, lanza una excepción con el detalle.

### Domain y Application (contratos compartidos)

- [ ] T016 [P] Crear en Domain `Excepciones/ConflictoConcurrenciaException.cs`,
  `Excepciones/ReglaNegocioException.cs` (con `int Codigo`) y `Common/ResultadoPaginado.cs`
  (`IReadOnlyList<T> Elementos`, `int TotalRegistros`, `int Pagina`, `int TamanoPagina`).
- [ ] T017 [P] Crear en Domain:
  - `Entities/Seguridad/SesionValidadaModel.cs` y `Entities/Seguridad/ConfiguracionSeguridadModel.cs`;
  - `Entities/Permisos/{ModuloModel,AccionModel,PermisoModel}.cs`;
  - las interfaces `Repositories/Sesiones/ISesionRepository.cs`,
    `Repositories/Permisos/IPermisoRepository.cs` y
    `Repositories/Plataforma/IConfiguracionRepository.cs`.
- [ ] T018 [P] Crear en Application:
  - `Common/Contexto/IContextoUsuario.cs`: `Ambito`, `NegocioId?`, `CuentaId?`, `CuentaTipo`,
    `EsSuperAdministrador`, `EnSoporte`, `IReadOnlySet<string> Permisos` y
    `bool TienePermiso(string)`;
  - `Common/Contexto/AmbitoConexion.cs`: enum técnico `Anonimo`, `Autenticacion`, `Negocio` y
    `Plataforma`.
- [ ] T019 [P] Crear en Application las interfaces `Common/Abstracciones/IHasherContrasena.cs`,
  `IGeneradorTokens.cs` (token de enlace, código de 6 dígitos y hash SHA-256),
  `IServicioCorreo.cs` (`EnviarAsync(codigoPlantilla, destinatario, valores, ct)`) e
  `IRelojUtc.cs`.
- [ ] T020 [P] Crear en Application:
  - `Common/Constants/CodigosPermiso.cs`, con constantes `"Usuarios.Ver"` … `"Auditoria.Ver"`;
  - las constantes de mensajes en español del módulo, agregadas en
    `Common/Constants/Messages.cs`. Los mensajes de Product no se tocan.
- [ ] T021 Crear `Application/Services/Sesiones/{ISesionService,SesionService}.cs` (`Validar`,
  `Crear`, `Revocar`, `RevocarOtras`). Calcula el hash SHA-256 del identificador y nunca guarda
  ni registra el valor en claro.

### Infrastructure

- [ ] T022 Reescribir `Electronic.Infraestructure/Data/IDbConnectionFactory.cs` y
  `SqlConnectionFactory.cs` como Scoped, con dependencias `IContextoUsuario` e
  `IConfiguration`.
  - `CreateConnection` abre la conexión y ejecuta `sp_set_session_context` con `@read_only = 1`
    para `Ambito`, `NegocioId`, `UsuarioId` y `AutorTipo`.
  - `CrearConexionAutenticacion` hace lo mismo con el ámbito `Autenticacion`.
  - El repositorio Product existente sigue compilando sin cambios.
- [ ] T023 [P] Crear `Electronic.Infraestructure/Data/TraductorErroresSql.cs`. Traduce:
  - 50001 → `ConflictoConcurrenciaException`;
  - 50002–50007 y 2601/2627 (estos como 50003) → `ReglaNegocioException(codigo)`;
  - cualquier otro número: relanza la excepción.
- [ ] T024 [P] Crear `Electronic.Infraestructure/Seguridad/HasherContrasena.cs` (sobre
  `PasswordHasher<object>`, que expone si hace falta rehash), `GeneradorTokens.cs` (con
  `RandomNumberGenerator`, Base64Url de 32 bytes y código de 6 dígitos) y `RelojUtc.cs`.
- [ ] T025 [P] Crear `Electronic.Infraestructure/Correo/ServicioCorreoSmtp.cs`:
  - usa `SmtpClient` con la configuración `Smtp:*`;
  - obtiene la plantilla con `usp_PlantillasCorreo_Obtener` y reemplaza los marcadores, con los
    valores codificados en HTML;
  - registra los fallos con `ILogger` sin incluir el contenido del correo y devuelve
    `bool enviado`.
- [ ] T026 Crear `Electronic.Infraestructure/Repositories/Sesiones/SesionRepository.cs`,
  `Repositories/Permisos/PermisoRepository.cs` y
  `Repositories/Plataforma/ConfiguracionRepository.cs`. Agregar
  `Cache/CatalogoCache.cs` (`IMemoryCache`) para el catálogo de permisos y la configuración,
  con `Invalidar*`.
- [ ] T027 Actualizar `Electronic.Infraestructure/DependencyInjection.cs`. Registrar como
  Scoped la fábrica, los repositorios y los servicios técnicos, y `AddMemoryCache`.
- [ ] T028 Registrar en `Electronic.Api/Program.cs` y `Electronic.Mcp/Program.cs` un
  `IContextoUsuario` de ámbito `Anonimo` (`Electronic.Infraestructure/Data/ContextoSinSesion.cs`),
  para que compilen con la fábrica Scoped. No cambia su comportamiento.

### Web

- [ ] T029 Crear `Electronic.Web/Seguridad/ContextoUsuarioWeb.cs`, que implementa
  `IContextoUsuario` a partir de `HttpContext.Items["SesionValidada"]`.
- [ ] T030 Crear `Electronic.Web/Seguridad/EventosCookieSesion.cs` (`CookieAuthenticationEvents`).
  `ValidatePrincipal` llama a `ISesionService.Validar`. Si la sesión no es válida, ejecuta
  `RejectPrincipal` + `SignOutAsync`; si es válida, guarda el `SesionValidadaModel` en
  `HttpContext.Items`.
- [ ] T031 Crear `Electronic.Web/Seguridad/RequierePermisoAttribute.cs` y
  `SoloPlataformaAttribute.cs`, con sus `IAuthorizationHandler` y `IAuthorizationPolicyProvider`.
  Sin permiso, devuelven 403.
- [ ] T032 Crear `Electronic.Web/Controllers/ControladorBase.cs`, con el método
  `ResultadoDesde<T>(ApiResponse<T>)`:
  - 404 → `NotFound`;
  - 409 → `ModelState` + aviso de conflicto;
  - 400 → `ModelState` con el mensaje.
- [ ] T033 Reescribir `Electronic.Web/Program.cs` con:
  - `AddApplication`, `AddInfrastructure`, `AddControllersWithViews` y el filtro global
    `AutoValidateAntiforgeryTokenAttribute`;
  - cookie auth: HttpOnly, Secure, SameSite=Lax, `LoginPath=/cuenta/iniciar-sesion` y los
    eventos de T030;
  - `AddRateLimiter` con la política `Autenticacion`: ventana fija de 10 por minuto por IP y
    rechazo con 429;
  - `UseExceptionHandler("/Home/Error")` con logging estructurado;
  - `Migraciones.AplicarMigraciones` con `DBMigraciones` cuando `Migraciones:AplicarAlIniciar`
    sea true.
- [ ] T034 Reescribir `Electronic.Web/Views/Shared/_Layout.cshtml`. HTML semántico, español,
  sin `lib/bootstrap` ni `lib/jquery`, con un enlace "saltar al contenido" y `<main>`. Los
  colores del negocio de la sesión se inyectan como `--color-primario` y `--color-secundario`
  en un `<style>` del servidor. El menú y el aviso de soporte se dejan como secciones parciales.
- [ ] T035 [P] Reescribir `Electronic.Web/wwwroot/css/site.css`:
  - variables en `:root` para colores, espaciados y tipografía;
  - diseño responsive, mobile-first, con cortes para tablet y escritorio;
  - estilos de formularios, tablas, mensajes, foco visible y `.oculto`;
  - sin estilos en línea.
- [ ] T036 [P] Reescribir `Electronic.Web/wwwroot/js/site.js` (JavaScript puro, sin `fetch`):
  solo funciones para mostrar/ocultar y la confirmación de acciones destructivas.
- [ ] T037 [P] Traducir al español `Electronic.Web/Views/Shared/Error.cshtml`, que muestra un
  mensaje amigable sin detalle técnico, y quitar las referencias a
  `_ValidationScriptsPartial` de las vistas nuevas.

### Pruebas de la base

- [ ] T038 [P] Crear `Electronic.Infrastructure.Tests/Migraciones/MigracionesTests.cs`
  (`[RequiereSqlFact]`):
  - todos los scripts se aplican sobre una base vacía;
  - una segunda ejecución no aplica nada;
  - existen `dbo.TipoListaIds` y las políticas RLS.
- [ ] T039 [P] Crear `Electronic.Infrastructure.Tests/Seguridad/RlsTests.cs`. Con ámbito
  `Anonimo`, no se ve ninguna fila de `Usuarios`, `Roles` ni `Negocios`. Con ámbito `Negocio` de
  A, solo se ven filas de A, y un INSERT con el NegocioId de B lo bloquea la política.
- [ ] T040 [P] Crear `Electronic.Application.Tests/Services/Sesiones/SesionServiceTests.cs`.
  Comprueba que el hash se calcula antes de llamar al repositorio y que la sesión inválida se
  devuelve como inválida.
- [ ] T041 Crear `Electronic.Infrastructure.Tests/Sesiones/SesionesProcedimientosTests.cs`:
  - una sesión revocada o inactiva por más de `MinutosInactividadSesion` es inválida;
  - con un rol `EsAccesoTotal`, los permisos son todo el catálogo, incluido un permiso insertado
    después (FR-019a);
  - un rol inactivo no suma permisos (FR-021);
  - un permiso o módulo inactivo no suma, ni siquiera con `EsAccesoTotal` (FR-038);
  - dos validaciones simultáneas de la misma sesión no producen error de concurrencia.

**Checkpoint**: la migración crea toda la base, RLS aísla y Web arranca con el layout nuevo.

---

## Phase 3: User Story 2 - Inicio de sesión, cierre y contraseñas (Priority: P1) 🎯 MVP (parte 1)

**Goal**: iniciar y cerrar sesión, elegir negocio, usar el segundo factor del Super
Administrador, cambiar y recuperar la contraseña, aplicar el bloqueo por correo y crear el
primer Super Administrador por invitación.

**Independent Test**: con un usuario sembrado en las pruebas, se comprueban el inicio y el
cierre de sesión, el cambio de contraseña, la recuperación, el bloqueo tras 5 fallos y la
selección de negocio si el correo existe en 2 negocios (quickstart escenarios 1, 2 y 9).

### Tests for User Story 2

- [ ] T042 [P] [US2] Crear `Electronic.Application.Tests/Services/Autenticacion/AutenticacionServiceTests.cs`
  con estos casos:
  - 0 coincidencias → `RegistrarFallo` y mensaje genérico;
  - 1 coincidencia → crea la sesión;
  - 2 coincidencias → pide elegir el negocio;
  - cuenta de Super Administrador → envía el código y no crea la sesión;
  - cuenta o negocio inactivo con contraseña correcta → rechazo;
  - correo bloqueado → rechazo aunque la contraseña sea correcta;
  - `SuccessRehashNeeded` → `ActualizarHash`;
  - código del segundo factor incorrecto → `RegistrarFallo`;
  - cambio de contraseña → `RevocarOtras`.
- [ ] T043 [P] [US2] Crear `Electronic.Application.Tests/Validators/ContrasenaValidatorsTests.cs`
  (FR-015: "al menos 8 caracteres e incluir letras y números" y la longitud mínima
  configurable).
- [ ] T044 [P] [US2] Crear `Electronic.Infrastructure.Tests/Autenticacion/AutenticacionProcedimientosTests.cs`:
  - 5 fallos con un correo presente en A y en B bloquean ambas cuentas (FR-014);
  - el éxito reinicia el contador;
  - un token se usa una sola vez y vence;
  - un token nuevo invalida el anterior;
  - la auditoría registra `InicioSesionFallido` y `Bloqueo` sin datos sensibles.

### Implementation for User Story 2

- [ ] T045 [US2] Crear `Database/Scripts/0009_usp_autenticacion.sql` con:
  - `usp_Autenticacion_ObtenerCuentas`, `_RegistrarFallo` (bloquea al llegar a
    `MaxIntentosFallidos` durante `MinutosBloqueo`), `_RegistrarExito` y `_ActualizarHash`;
  - `usp_Tokens_Crear` (invalida los vigentes del mismo tipo y cuenta), `usp_Tokens_Consumir`
    (50004 si es inválido, vencido o usado; si trae hash, define la contraseña) y
    `usp_Tokens_ObtenerCuentasRecuperacion`;
  - `usp_SuperAdministradores_CrearInicial` (solo si no existe ninguno).

  Los procedimientos que actualizan `ControlIntentos` y `TokensSeguridad` leen la `RowVersion`
  con `WITH (UPDLOCK, HOLDLOCK)` y la validan en el `UPDATE`. Todos llenan las columnas de
  creación y modificación.
- [ ] T046 [P] [US2] Crear en Domain `Entities/Autenticacion/{CuentaAutenticacionModel,EstadoCorreoModel}.cs`
  y `Repositories/Autenticacion/IAutenticacionRepository.cs`.
- [ ] T047 [US2] Crear `Electronic.Infraestructure/Repositories/Autenticacion/AutenticacionRepository.cs`.
  Usa siempre `CrearConexionAutenticacion`.
- [ ] T048 [P] [US2] Crear en Application:
  - `DTO/Autenticacion/{InicioSesionDto,ResultadoInicioSesionDto,CuentaOpcionDto,SegundoFactorDto,DefinirContrasenaDto,CambioContrasenaDto,RecuperacionDto}.cs`;
  - `Validators/ContrasenaValidators.cs`;
  - `Mapping/AutenticacionMapping.cs`.
- [ ] T049 [US2] Crear `Application/Services/Autenticacion/{IAutenticacionService,AutenticacionService}.cs`
  con:
  - `IniciarSesion` y `SeleccionarCuenta`;
  - `EnviarSegundoFactor` y `VerificarSegundoFactor` (FR-027a);
  - `CerrarSesion` y `CambiarContrasena` (FR-012);
  - `SolicitarRecuperacion` (FR-013): siempre responde la misma confirmación y envía un enlace
    por cuenta, con el nombre del negocio;
  - `DefinirContrasena` (invitación o recuperación).

  Registrarlo en `DependencyInjection.cs`.
- [ ] T050 [US2] Crear `Electronic.Web/Seguridad/CookiePreautenticacion.cs`. Protege con
  `IDataProtector` los Id de las cuentas que coincidieron y el paso pendiente (seleccionar o
  segundo factor). Vence a los 5 minutos.
- [ ] T051 [US2] Crear `Electronic.Web/Controllers/CuentaController.cs` con las rutas de
  [contracts/rutas-web.md § Cuenta](contracts/rutas-web.md#cuenta-autenticación):
  - `[EnableRateLimiting("Autenticacion")]` en iniciar-sesión, segundo-factor y recuperar;
  - cierre de sesión por POST.
- [ ] T052 [P] [US2] Crear `Electronic.Web/Models/Cuenta/*ViewModel.cs` y las vistas
  `Electronic.Web/Views/Cuenta/{IniciarSesion,SeleccionarNegocio,SegundoFactor,Recuperar,RecuperarConfirmacion,DefinirContrasena,CambiarContrasena,Perfil}.cshtml`:
  - con `<label>` en cada campo y `autocomplete` correcto (`username`, `current-password`,
    `new-password`, `one-time-code`);
  - con los mensajes de error en una región `role="alert"`.
- [ ] T053 [US2] Crear `Electronic.Web/Seguridad/InicializadorPlataforma.cs`, que se ejecuta al
  iniciar, después de las migraciones. Si no existe ningún Super Administrador y
  `Instalacion:CorreoSuperAdministrador` tiene valor, lo crea como pendiente y envía la
  invitación (FR-027). Registrarlo en `Program.cs`.

**Checkpoint**: el Super Administrador inicial se activa por correo, entra con segundo factor y
el bloqueo funciona.

---

## Phase 4: User Story 1 - Alta de negocio lista para operar (Priority: P1) 🎯 MVP (parte 2)

**Goal**: el Super Administrador crea, edita, lista, activa y desactiva negocios con logo
PNG/JPEG. Los negocios nuevos reciben los roles sugeridos. El Super Administrador invita al
administrador inicial y puede entrar a un negocio en modo soporte.

**Independent Test**: quickstart escenarios 3, 3b, 4, 13 y 14.

### Tests for User Story 1

- [ ] T054 [P] [US1] Crear `Electronic.Application.Tests/Validators/NegocioValidatorsTests.cs`:
  - Nombre, Nit y CorreoContacto son obligatorios;
  - el correo debe ser válido;
  - los colores deben tener formato `#RRGGBB`;
  - "DiasResguardo ≥ 0";
  - las longitudes máximas son las de T008.
- [ ] T055 [P] [US1] Crear `Electronic.Application.Tests/Validators/ImagenValidatorsTests.cs`
  (FR-037):
  - se aceptan la firma PNG `89 50 4E 47 0D 0A 1A 0A` y la JPEG `FF D8 FF`;
  - se rechaza un GIF renombrado a `.png`;
  - se rechazan más de 2 MB (2 097 152 bytes);
  - el `TipoContenido` se deduce de la firma.
- [ ] T056 [P] [US1] Crear `Electronic.Application.Tests/Services/Negocios/NegocioServiceTests.cs`:
  - creación válida;
  - NIT duplicado (50003) → 400;
  - conflicto (50001) → 409;
  - administrador inicial: invitación enviada, o advertencia si el correo falla;
  - entrar y salir de soporte.
- [ ] T057 [P] [US1] Crear `Electronic.Infrastructure.Tests/Negocios/NegociosProcedimientosTests.cs`:
  - `usp_Negocios_Crear` copia las 4 plantillas de rol con sus permisos, y la bandera
    `EsAccesoTotal` en Administrador;
  - el NIT duplicado da 50003;
  - al desactivar, `usp_Sesiones_Validar` invalida las sesiones del negocio (FR-005);
  - la entrada y la salida de soporte quedan auditadas con `NegocioId` y `AutorTipo = 'S'`.

### Implementation for User Story 1

- [ ] T058 [US1] Crear `Database/Scripts/0010_usp_negocios.sql` con:
  - `usp_Negocios_Listar`, que busca por nombre o NIT y no devuelve `Contenido`;
  - `_Obtener`;
  - `_Crear`, que crea el archivo del logo con `@LogoTipoContenido` y copia `PlantillasRol` y
    `PlantillaRolPermisos`;
  - `_Actualizar` y `_CambiarEstado`;
  - `_CrearAdministradorInicial`, que crea el usuario pendiente con el rol `EsAccesoTotal` del
    negocio;
  - `usp_Sesiones_EntrarSoporte`, `usp_Sesiones_SalirSoporte` y `usp_Archivos_Obtener`.
- [ ] T059 [P] [US1] Crear en Domain `Entities/Negocios/NegocioModel.cs`,
  `Entities/Archivos/ArchivoModel.cs`, `Repositories/Negocios/INegocioRepository.cs` y
  `Repositories/Archivos/IArchivoRepository.cs`.
- [ ] T060 [US1] Crear `Electronic.Infraestructure/Repositories/Negocios/NegocioRepository.cs`
  y `Repositories/Archivos/ArchivoRepository.cs`. El logo viaja como `DbType.Binary`.
- [ ] T061 [P] [US1] Crear en Application:
  - `DTO/Negocios/{NegocioDto,NegocioListadoDto,LogoDto,AdministradorInicialDto}.cs`;
  - `Validators/NegocioValidators.cs` y `Validators/ImagenValidators.cs` (FR-037);
  - `Mapping/NegocioMapping.cs`.
- [ ] T062 [US1] Crear `Application/Services/Negocios/{INegocioService,NegocioService}.cs`
  (FR-001–005, FR-026) y `Application/Services/Soporte/{ISoporteService,SoporteService}.cs`
  (FR-026a/b). Registrarlos en `DependencyInjection.cs`.
- [ ] T063 [US1] Crear `Electronic.Web/Controllers/PlataformaController.cs` con las rutas
  `/plataforma/negocios*`, `[SoloPlataforma]`. El logo se recibe con `IFormFile`, con un
  límite de solicitud de 2 MB.
- [ ] T064 [P] [US1] Crear `Electronic.Web/Controllers/SoporteController.cs` (`POST /soporte/salir`)
  y `Electronic.Web/Controllers/ArchivosController.cs` (`GET /archivos/{id}`, que responde con
  el `TipoContenido` guardado y `Cache-Control: private`).
- [ ] T065 [P] [US1] Crear `Electronic.Web/Models/Negocios/*ViewModel.cs` y las vistas
  `Electronic.Web/Views/Plataforma/{Negocios,CrearNegocio,EditarNegocio,AdministradorInicial}.cshtml`.
  Los colores usan `<input type="color">` y el logo `<input type="file" accept="image/png,image/jpeg">`.
- [ ] T066 [US1] Agregar a `Electronic.Web/Views/Shared/_AvisoSoporte.cshtml` el aviso fijo
  "Soporte en {Negocio}" con el botón Salir, e incluirlo en `_Layout.cshtml` cuando
  `IContextoUsuario.EnSoporte`.
- [ ] T067 [P] [US1] Agregar a `Electronic.Web/wwwroot/js/site.js` la vista previa del logo
  elegido (`URL.createObjectURL`), sin validarlo en el cliente.

**Checkpoint**: MVP (US2 + US1). La plataforma crea un negocio operativo con su administrador
invitado.

---

## Phase 5: User Story 3 - Roles y matriz de permisos (Priority: P2)

**Goal**: CRUD de roles del negocio y matriz Módulo × Acción con las reglas de consulta,
escalada y acceso total.

**Independent Test**: crear el rol "Cajero" con `Usuarios.Crear` y verificar que `Usuarios.Ver`
se agrega solo. Un usuario con ese rol obtiene exactamente esos 2 permisos (quickstart
escenarios 6 y 12).

### Tests for User Story 3

- [ ] T068 [P] [US3] Crear `Electronic.Application.Tests/Validators/MatrizPermisosValidatorsTests.cs`
  (FR-020a): una acción sin la consulta de su módulo se rechaza, usando la bandera
  `EsConsulta` y no el texto "Ver".
- [ ] T069 [P] [US3] Crear `Electronic.Application.Tests/Services/Roles/RolServiceTests.cs`:
  - marcar un permiso que el autor no tiene → 400 + evento `AccesoDenegado` (FR-020b);
  - editar un rol propio → rechazo;
  - un Super Administrador puede todo;
  - cambiar `EsAccesoTotal` siendo usuario de negocio → rechazo (FR-019a);
  - nombre duplicado → 400.
- [ ] T070 [P] [US3] Crear `Electronic.Infrastructure.Tests/Roles/RolesProcedimientosTests.cs`:
  - 50005 si la matriz es inválida;
  - 50002 al desactivar el único rol con todos los permisos `EsAdministracion`;
  - 50001 si el `RowVersion` cambió;
  - un nombre único por negocio puede repetirse en otro negocio;
  - el TVP reemplaza el conjunto y queda auditado con el antes y el después.

### Implementation for User Story 3

- [ ] T071 [US3] Crear `Database/Scripts/0011_usp_roles.sql` con:
  - `usp_Roles_Listar`, `_Obtener` (devuelve los PermisoIds), `_Crear` y `_CambiarEstado`;
  - `_Actualizar`, que recibe `@Permisos dbo.TipoListaIds` y valida 50005 y 50002;
  - `_CambiarAccesoTotal`, solo en ámbito Plataforma o soporte.
- [ ] T072 [P] [US3] Crear en Domain `Entities/Roles/RolModel.cs` y
  `Repositories/Roles/IRolRepository.cs`.
- [ ] T073 [US3] Crear `Electronic.Infraestructure/Repositories/Roles/RolRepository.cs`. El TVP
  se arma con un `DataTable` (`Id INT`) y `.AsTableValuedParameter("dbo.TipoListaIds")`.
- [ ] T074 [P] [US3] Crear en Application `DTO/Roles/{RolDto,RolListadoDto,MatrizPermisosDto,CeldaMatrizDto}.cs`,
  `Validators/{RolValidators,MatrizPermisosValidators}.cs` ("Nombre obligatorio (≤ 100)") y
  `Mapping/RolMapping.cs`.
- [ ] T075 [US3] Crear `Application/Services/Roles/{IRolService,RolService}.cs`:
  - construye la matriz desde el catálogo en caché;
  - marca deshabilitadas las celdas que el autor no tiene, o todas si el rol es suyo;
  - aplica FR-020a/b y FR-019a.

  Registrarlo en `DependencyInjection.cs`.
- [ ] T076 [US3] Crear `Electronic.Web/Controllers/RolesController.cs` con `/roles*` y
  `[RequierePermiso]` según el contrato. Agregar `POST /plataforma/roles/{id}/acceso-total` en
  `PlataformaController.cs`.
- [ ] T077 [P] [US3] Crear `Electronic.Web/Models/Roles/*ViewModel.cs` y las vistas
  `Electronic.Web/Views/Roles/{Index,Crear,Editar}.cshtml`. La matriz es una `<table>` con
  `<caption>`, `scope` en los encabezados y una casilla con `aria-label` "Módulo – Acción" en
  cada celda.
- [ ] T078 [P] [US3] Agregar a `Electronic.Web/wwwroot/js/site.js` el comportamiento de la
  matriz: marcar otra acción marca la columna con `data-es-consulta`, y desmarcar la consulta
  desmarca la fila. Es solo comodidad; el servidor valida.

**Checkpoint**: el negocio configura sus roles sin escalar privilegios.

---

## Phase 6: User Story 4 - Gestión de usuarios del negocio (Priority: P2)

**Goal**: CRUD de usuarios con roles múltiples, invitación con reenvío, estado pendiente y
protección de la última administración.

**Independent Test**: crear un usuario con dos roles de permisos distintos (uno con
`Usuarios.Ver` y otro con `Roles.Ver`) y verificar que sus permisos son la suma de ambos; reenviar la invitación invalida el enlace anterior (quickstart escenarios 7, 8
y 11).

### Tests for User Story 4

- [ ] T079 [P] [US4] Crear `Electronic.Application.Tests/Services/Usuarios/UsuarioServiceTests.cs`:
  - asignar un rol con permisos fuera de los del autor → rechazo (FR-008a);
  - editar los roles propios → rechazo;
  - sin roles → 400 (FR-008);
  - si el correo falla, el usuario se crea con una advertencia;
  - reenviar la invitación crea un token nuevo.
- [ ] T080 [P] [US4] Crear `Electronic.Application.Tests/Validators/UsuarioValidatorsTests.cs`:
  "Nombre y correo obligatorios; correo válido; al menos un rol".
- [ ] T081 [P] [US4] Crear `Electronic.Infrastructure.Tests/Usuarios/UsuariosProcedimientosTests.cs`:
  - el correo es único por negocio sin distinguir mayúsculas (FR-007), y puede repetirse en
    otro negocio;
  - 50002 al desactivar o quitar roles al último administrador (FR-010);
  - desactivar a un usuario invalida su sesión en la siguiente validación.

### Implementation for User Story 4

- [ ] T082 [US4] Crear `Database/Scripts/0012_usp_usuarios.sql` con:
  - `usp_Usuarios_Listar` (estado derivado: pendiente si `HashContrasena IS NULL`, y los nombres
    de los roles) y `_Obtener`;
  - `_Crear` y `_Actualizar`, con `@Roles dbo.TipoListaIds`;
  - `_CambiarEstado`, que valida 50002.
- [ ] T083 [P] [US4] Crear en Domain `Entities/Usuarios/UsuarioModel.cs` y
  `Repositories/Usuarios/IUsuarioRepository.cs`.
- [ ] T084 [US4] Crear `Electronic.Infraestructure/Repositories/Usuarios/UsuarioRepository.cs`.
- [ ] T085 [P] [US4] Crear en Application `DTO/Usuarios/{UsuarioDto,UsuarioListadoDto}.cs`,
  `Validators/UsuarioValidators.cs` y `Mapping/UsuarioMapping.cs`.
- [ ] T086 [US4] Crear `Application/Services/Usuarios/{IUsuarioService,UsuarioService}.cs`.
  Incluye `ReenviarInvitacion`, que vence a `HorasEnlaceInvitacion`. Solo ofrece los roles
  cuyos permisos son un subconjunto de los del autor. Registrarlo en `DependencyInjection.cs`.
- [ ] T087 [US4] Crear `Electronic.Web/Controllers/UsuariosController.cs` con `/usuarios*` y
  `[RequierePermiso]` según el contrato.
- [ ] T088 [P] [US4] Crear `Electronic.Web/Models/Usuarios/*ViewModel.cs` y las vistas
  `Electronic.Web/Views/Usuarios/{Index,Crear,Editar}.cshtml`. Muestran el estado "Pendiente",
  "Activo" o "Inactivo", y deshabilitan los roles propios o no asignables.

**Checkpoint**: el negocio incorpora a su personal.

---

## Phase 7: User Story 5 - Control de acceso en pantallas, acciones y menús (Priority: P2)

**Goal**: menú filtrado por permisos, página y auditoría de accesos denegados, 404 ante datos de
otro negocio y perfil limitado para usuarios sin roles activos.

**Independent Test**: un usuario con solo Técnico ve un menú reducido. La URL directa a
`/usuarios` responde "Acceso denegado" y queda auditada. Un Id de otro negocio da 404
(quickstart escenarios 7 y 10).

### Tests for User Story 5

- [ ] T089 [P] [US5] Crear `Electronic.Application.Tests/Services/Navegacion/ConstructorMenuTests.cs`.
  Solo aparecen las entradas cuyo permiso de consulta está en el conjunto, y las de plataforma
  solo en el ámbito Plataforma.
- [ ] T090 [P] [US5] Crear `Electronic.Infrastructure.Tests/Seguridad/AislamientoNegociosTests.cs`
  (SC-002). Para cada procedimiento de negocio (`usp_Usuarios_*`, `usp_Roles_*`,
  `usp_Archivos_Obtener` y `usp_Auditoria_Listar`), con la sesión de A y el Id de un recurso de
  B, el resultado es vacío o 50007. Nunca devuelve datos de B.
- [ ] T091 [P] [US5] Crear `Electronic.Infrastructure.Tests/Web/AutorizacionControladoresTests.cs`
  (SC-003), con una `ProjectReference` a `Electronic.Web` en el csproj de pruebas.
  - Recorre por reflexión todos los métodos de acción públicos de los controladores de
    `Electronic.Web`.
  - Falla con la lista de las acciones que no tengan (en el método o en la clase)
    `[RequierePermiso]`, `[SoloPlataforma]` o `[AllowAnonymous]`.
  - Es un `[Fact]` normal, sin SQL, para que corra en el CI.

### Implementation for User Story 5

- [ ] T092 [US5] Crear `Application/Services/Navegacion/{EntradaMenu,ConstructorMenu}.cs`. Las
  entradas declaran el código de permiso de consulta (`CodigosPermiso`) o
  `RequierePlataforma`.
- [ ] T093 [US5] Crear `Electronic.Web/Views/Shared/_Menu.cshtml` (un `<nav>` con
  `aria-label="Principal"`) a partir de `ConstructorMenu`, e incluirlo en `_Layout.cshtml`.
- [ ] T094 [US5] Crear `Electronic.Web/Seguridad/ManejadorAccesoDenegado.cs`
  (`IAuthorizationMiddlewareResultHandler`). Ante un rechazo, registra `AccesoDenegado` con
  `usp_Auditoria_RegistrarEvento` (vía `IAuditoriaRepository.RegistrarEvento`; crear la
  interfaz y el método si US6 aún no existe) y muestra
  `Electronic.Web/Views/Shared/AccesoDenegado.cshtml`.
- [ ] T095 [US5] En `Electronic.Web/Controllers/CuentaController.cs`, después de iniciar sesión,
  redirigir a `/cuenta/perfil` cuando los permisos efectivos estén vacíos (caso límite de
  usuario sin roles activos).

**Checkpoint**: el control de acceso es efectivo y verificable.

---

## Phase 8: User Story 6 - Consulta de auditoría (Priority: P3)

**Goal**: consulta paginada y filtrada de la auditoría del negocio (y de toda la plataforma para
el Super Administrador).

**Independent Test**: cambiar el teléfono de un usuario; la auditoría muestra el autor, la
fecha, el valor anterior y el nuevo (quickstart escenario 15).

### Tests for User Story 6

- [ ] T096 [P] [US6] Crear `Electronic.Application.Tests/Services/Auditoria/AuditoriaServiceTests.cs`:
  validación de filtros (desde ≤ hasta), paginación y deserialización de `Cambios`.
- [ ] T097 [P] [US6] Crear `Electronic.Infrastructure.Tests/Auditoria/AuditoriaProcedimientosTests.cs`:
  - un auditor de A solo ve A, y la Plataforma ve todo (FR-030);
  - `Cambios` nunca contiene `HashContrasena` ni tokens (FR-029);
  - `rol_aplicacion` no puede hacer UPDATE ni DELETE (FR-031).

### Implementation for User Story 6

- [ ] T098 [US6] Crear `Database/Scripts/0013_usp_auditoria_consulta.sql` con
  `usp_Auditoria_Listar` (`@NegocioId NULL` solo en el ámbito Plataforma, filtros de fecha,
  autor, entidad y acción, y paginación).
- [ ] T099 [P] [US6] Crear en Domain `Entities/Auditoria/RegistroAuditoriaModel.cs` y completar
  `Repositories/Auditoria/IAuditoriaRepository.cs` (`Listar` y `RegistrarEvento`).
- [ ] T100 [US6] Crear `Electronic.Infraestructure/Repositories/Auditoria/AuditoriaRepository.cs`.
- [ ] T101 [US6] Crear en Application `DTO/Auditoria/{FiltroAuditoriaDto,RegistroAuditoriaDto,CambioCampoDto}.cs`
  y `Services/Auditoria/{IAuditoriaService,AuditoriaService}.cs`. Registrarlos.
- [ ] T102 [US6] Crear `Electronic.Web/Controllers/AuditoriaController.cs`
  (`GET /auditoria`, `[RequierePermiso("Auditoria.Ver")]`) y agregar `GET /plataforma/auditoria`
  en `PlataformaController.cs`.
- [ ] T103 [P] [US6] Crear la vista `Electronic.Web/Views/Auditoria/Index.cshtml`: filtros con
  etiquetas, una tabla con fecha (convertida a la zona del navegador solo para mostrar), autor,
  acción, entidad y un detalle de cambios campo por campo.

**Checkpoint**: se pueden consultar los cambios y los eventos de seguridad.

---

## Phase 9: User Story 7 - Plantillas y configuración de plataforma (Priority: P3)

**Goal**: el Super Administrador edita la plantilla de roles sugeridos, las plantillas de correo
(FR-036), la configuración de seguridad (FR-035), las cuentas de Super Administrador (FR-027) y
el catálogo de permisos (FR-038).

**Independent Test**: cambiar el asunto de la invitación y agregar un rol sugerido. La siguiente
invitación usa el asunto nuevo y el siguiente negocio recibe el rol, sin cambiar los existentes.
Al renombrar un permiso, el nombre nuevo aparece en la matriz; al desactivarlo, deja de sumar.

### Tests for User Story 7

- [ ] T104 [P] [US7] Crear `Electronic.Application.Tests/Services/Plataforma/PlataformaServiceTests.cs`:
  - rangos de configuración (tabla de data-model) y "nunca menos de 8" en la contraseña;
  - al guardar la configuración se invalida la caché;
  - no se puede desactivar el último Super Administrador (50006 → 400).
- [ ] T105 [P] [US7] Crear `Electronic.Infrastructure.Tests/Plataforma/PlataformaProcedimientosTests.cs`:
  - cambiar `PlantillasRol` no altera los `Roles` de un negocio existente, y sí los del
    siguiente creado;
  - 50006 al desactivar el último Super Administrador;
  - cada cambio queda auditado.
- [ ] T106 [P] [US7] Crear `Electronic.Infrastructure.Tests/Plataforma/CatalogoPermisosProcedimientosTests.cs`
  (FR-038):
  - `usp_Permisos_Actualizar` cambia el nombre y la descripción, pero nunca `Codigo`,
    `EsConsulta` ni `EsAdministracion`;
  - desactivar `Usuarios.Ver` con `Usuarios.Crear` activo da 50008;
  - desactivar un permiso `EsAdministracion` da 50008;
  - un permiso desactivado deja de salir en `usp_Sesiones_Validar`, aun con `EsAccesoTotal`;
  - si la `RowVersion` cambió, da 50001;
  - cada cambio queda auditado con `Entidad` `Modulo` o `Permiso`.
- [ ] T107 [P] [US7] Crear `Electronic.Application.Tests/Services/Plataforma/CatalogoPermisosServiceTests.cs`:
  nombre obligatorio (≤ 100), descripción ≤ 500, 50008 → 400 con un mensaje en español, e
  invalidación de la caché del catálogo al guardar.

### Implementation for User Story 7

- [ ] T108 [US7] Crear `Database/Scripts/0014_usp_plataforma.sql` con:
  - `usp_Configuracion_Actualizar`;
  - `usp_PlantillasRol_Listar`, `_Obtener` y `_Guardar` (`@Permisos dbo.TipoListaIds`);
  - `usp_PlantillasCorreo_Listar` y `_Actualizar`;
  - `usp_SuperAdministradores_Listar`, `_Crear` (crea uno pendiente) y `_CambiarEstado` (50006);
  - `usp_CatalogoPermisos_Listar`;
  - `usp_Modulos_Actualizar`, `usp_Acciones_Actualizar` y `usp_Permisos_Actualizar` (solo
    `Nombre`, `Descripcion` y `Orden`, con `@RowVersion`);
  - `usp_Modulos_CambiarEstado` y `usp_Permisos_CambiarEstado` (50008 según
    [data-model.md](data-model.md#catálogo-de-permisos-global-sembrado-editable-por-el-super-administrador)).
- [ ] T109 [P] [US7] Crear en Domain `Entities/Plataforma/{PlantillaRolModel,PlantillaCorreoModel,SuperAdministradorModel}.cs`
  y completar `Repositories/Plataforma/` (`IPlantillaRepository` e
  `ISuperAdministradorRepository`). Completar también `Repositories/Permisos/IPermisoRepository.cs`
  con `ListarCatalogoCompleto`, `ActualizarModulo`, `ActualizarAccion`, `ActualizarPermiso`,
  `CambiarEstadoModulo` y `CambiarEstadoPermiso`.
- [ ] T110 [US7] Crear `Electronic.Infraestructure/Repositories/Plataforma/{PlantillaRepository,SuperAdministradorRepository}.cs`.
  Completar `ConfiguracionRepository.cs` (Actualizar + `CatalogoCache.InvalidarConfiguracion`) y
  `Repositories/Permisos/PermisoRepository.cs` (los métodos de edición +
  `CatalogoCache.InvalidarCatalogo`).
- [ ] T111 [US7] Crear en Application `DTO/Plataforma/*.cs` (incluidos
  `ModuloCatalogoDto`, `AccionCatalogoDto` y `PermisoCatalogoDto`),
  `Validators/{ConfiguracionSeguridadValidators,PlantillaCorreoValidators,CatalogoPermisosValidators}.cs`
  y `Services/Plataforma/{IPlataformaService,PlataformaService}.cs` y
  `Services/Plataforma/{ICatalogoPermisosService,CatalogoPermisosService}.cs`. Al crear un Super
  Administrador se le envía la invitación. Registrarlos.
- [ ] T112 [US7] Agregar a `Electronic.Web/Controllers/PlataformaController.cs` las rutas
  `/plataforma/configuracion`, `/plataforma/plantillas-rol*`, `/plataforma/plantillas-correo*`,
  `/plataforma/super-administradores*` y `/plataforma/catalogo-permisos*`.
- [ ] T113 [P] [US7] Crear las vistas `Electronic.Web/Views/Plataforma/{Configuracion,PlantillasRol,EditarPlantillaRol,PlantillasCorreo,EditarPlantillaCorreo,SuperAdministradores,CatalogoPermisos,EditarModulo,EditarPermiso}.cshtml`.
  - La plantilla de rol reutiliza el parcial de la matriz de US3, sin las restricciones de autor.
  - En el catálogo, los códigos se muestran como texto de solo lectura y los estados con un
    botón activar/desactivar, que confirma con `site.js`.

**Checkpoint**: todas las historias están completas.

---

## Phase 10: Polish & Cross-Cutting Concerns

- [ ] T114 [P] Actualizar `CLAUDE.md` con:
  - el módulo y la interfaz en `Electronic.Web`;
  - la fábrica Scoped con `SESSION_CONTEXT` y RLS;
  - DbUp, los scripts y los procedimientos `usp_`;
  - las sesiones validadas en cada solicitud;
  - la variable `PRUEBAS_SQL`;
  - lo que sigue siendo deuda de Product.
- [ ] T115 Ejecutar
  `dotnet test Electronic.Application.Tests --collect:"XPlat Code Coverage"` y verificar una
  cobertura ≥ 80% de líneas en `Electronic.Domain` y `Electronic.Application` (sin contar
  Product). Agregar pruebas donde falte.
- [ ] T116 [P] Crear `Electronic.Infrastructure.Tests/Rendimiento/RendimientoProcedimientosTests.cs`
  (`[RequiereSqlFact]`, meta de plan p95 < 500 ms por página):
  - siembra 50 usuarios, 10 roles y 10 000 filas de auditoría en un negocio;
  - mide el p95 de 100 ejecuciones de `usp_Sesiones_Validar` (< 50 ms), de `usp_Usuarios_Listar`
    (< 200 ms) y de `usp_Auditoria_Listar` con filtros (< 200 ms);
  - si una meta no se cumple, falla con los tiempos medidos.
- [ ] T117 Ejecutar `dotnet build Electronic.slnx` y confirmar que no hay advertencias nuevas
  (solo las 5 CS0168 heredadas de `ProductService`).
- [ ] T118 Revisión de seguridad:
  - ningún `ILogger` ni `Cambios` de auditoría contiene contraseñas, hashes, tokens, códigos ni
    cadenas de conexión;
  - todos los POST tienen antiforgery;
  - toda acción de controlador tiene `[RequierePermiso]`, `[SoloPlataforma]` o
    `[AllowAnonymous]` explícito.
- [ ] T119 Revisión de accesibilidad y responsive de todas las vistas nuevas: etiquetas,
  `alt`, navegación con teclado, foco visible y anchos de 360 px, 768 px y 1280 px.
- [ ] T120 Ejecutar con `PRUEBAS_SQL` definido `dotnet test Electronic.Infrastructure.Tests` y
  recorrer manualmente los escenarios 1–16 de [quickstart.md](quickstart.md).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup y bloquea todas las historias.
- **US2 (Phase 3)**: depende de Foundational. Es la primera historia, porque todas las
  pantallas requieren sesión.
- **US1 (Phase 4)**: depende de US2, porque necesita el inicio de sesión del Super
  Administrador.
- **US3, US4, US5 y US6**: dependen de US1, porque necesitan un negocio con administrador.
  - Entre sí son independientes.
  - **US4** usa los roles sembrados por US1; no depende de las pantallas de US3.
  - **US5** se valida mejor con US3 y US4 hechas, pero sus pruebas no las requieren.
- **US7**: depende de US1 (reutiliza el parcial de la matriz si US3 existe; si no, crea el suyo).
- **Polish**: al final.

### Within Each User Story

- Pruebas primero (deben fallar), luego script SQL, Domain, Infrastructure, Application y Web.
- El script de la historia va antes que su repositorio. El repositorio va antes que el
  servicio, y el servicio antes que el controlador y las vistas.

### Parallel Opportunities

- **Setup**: T004, T005 y T006.
- **Foundational**:
  - semillas T012 y T013 (después de T008);
  - Domain y Application T016–T020 en paralelo;
  - Infrastructure T023, T024 y T025;
  - Web T035, T036 y T037;
  - pruebas T038, T039 y T040.
- **Cada historia**: todas sus pruebas [P] juntas, sus modelos y DTO [P] juntos, y sus vistas y
  JS [P] junto al controlador.
- **Después de US1**: US3, US4, US5 y US6 pueden avanzar en paralelo.

---

## Parallel Example: User Story 3

```bash
# Pruebas juntas:
Task: "T068 MatrizPermisosValidatorsTests en Electronic.Application.Tests/Validators/"
Task: "T069 RolServiceTests en Electronic.Application.Tests/Services/Roles/"
Task: "T070 RolesProcedimientosTests en Electronic.Infrastructure.Tests/Roles/"

# Modelos y DTO juntos (después de T071):
Task: "T072 RolModel + IRolRepository en Electronic.Domain/"
Task: "T074 DTO/Validators/Mapping de roles en Electronic.Application/"

# Vistas y JS junto al controlador (después de T075):
Task: "T077 Vistas de roles en Electronic.Web/Views/Roles/"
Task: "T078 Comportamiento de matriz en Electronic.Web/wwwroot/js/site.js"
```

---

## Implementation Strategy

### MVP First (US2 + US1)

1. Phase 1 Setup y Phase 2 Foundational.
2. Phase 3 (US2): el Super Administrador se activa e inicia sesión con segundo factor.
3. Phase 4 (US1): crea un negocio con su administrador invitado.
4. **STOP and VALIDATE**: quickstart escenarios 1–5, 13 y 14.

### Incremental Delivery

1. MVP → US3 (roles) → US4 (usuarios) → US5 (control de acceso) → US6 (auditoría) → US7
   (plantillas y configuración).
2. Cada historia se cierra con su checkpoint y sus pruebas en verde.
3. Un PR por fase o por historia hacia `development`.
4. **Requisito de cada PR** (hasta que el CI tenga SQL Server): adjuntar en la descripción la
   salida de `dotnet test Electronic.Infrastructure.Tests` ejecutada con `PRUEBAS_SQL`, sin
   pruebas omitidas ni fallidas. El revisor no aprueba sin ella.

### Fuera de este módulo (decisión del 2026-11-11)

En la funcionalidad de deuda técnica:
- `TreatWarningsAsErrors` y `.editorconfig`;
- corregir `ProductService`;
- borrar `wwwroot/lib`, `_ValidationScriptsPartial` y `Electronic.Tests`;
- agregar SQL Server al CI.

---

## Notes

- [P] = archivos distintos y sin dependencias pendientes.
- Los scripts ya aplicados no se editan: los cambios van en un script nuevo con el número
  siguiente.
- Haz commit al terminar cada tarea o grupo lógico.
