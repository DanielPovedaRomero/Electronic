# Research: Negocios, usuarios, roles y permisos

**Fecha**: 2026-11-11 | **Plan**: [plan.md](plan.md) | **Spec**: [spec.md](spec.md)

Cada decisión resuelve una incógnita del contexto técnico y respeta la constitución 2.0.0.

---

## R1. Host de la interfaz

- **Decision**: la interfaz del módulo se construye en `Electronic.Web` con ASP.NET Core MVC y
  vistas Razor renderizadas en el servidor.
  - Los controladores web consumen los servicios de `Electronic.Application` por DI.
  - El módulo no agrega endpoints al API ni herramientas al MCP.
- **Rationale**: el Principio III exige renderizado en servidor y JavaScript sin llamadas al API.
  La especificación solo describe pantallas.
- **Alternatives considered**:
  - Web llamando al API por HTTP: rechazado, duplica autenticación y viola III.
  - Exponer el módulo también en el API: no lo pide la especificación; se puede hacer en un
    módulo posterior.

## R2. Autenticación y sesiones

- **Decision**: cookie de autenticación de ASP.NET Core (incluida en el framework compartido)
  con sesiones guardadas en el servidor.
  - La cookie (HttpOnly, Secure, SameSite=Lax) solo contiene un identificador de sesión
    aleatorio de 256 bits. La tabla `Sesiones` guarda su hash SHA-256.
  - En cada solicitud, `OnValidatePrincipal` llama a `usp_Sesiones_Validar`, que en una sola
    llamada:
    - verifica que la sesión exista, no esté revocada y no supere la inactividad configurada;
    - verifica que el usuario y su negocio estén activos;
    - devuelve los permisos efectivos actuales y actualiza la última actividad, como mucho una
      vez por minuto.
  - Cerrar sesión revoca la fila. Cambiar la contraseña revoca las demás sesiones del usuario.
- **Rationale**:
  - Cumple FR-005, FR-016 y FR-022: los cambios de estado y de permisos se aplican en la
    siguiente acción, porque los permisos no viajan en la cookie.
  - El costo es un procedimiento por solicitud, aceptable para la escala prevista (ver plan).
- **Alternatives considered**:
  - Permisos como claims dentro de la cookie: rechazado, no se pueden revocar hasta que vence
    la cookie.
  - JWT: rechazado, la aplicación es renderizada en servidor y no necesita tokens portables.

## R3. Inicio de sesión con correo en varios negocios

- **Decision**:
  1. `usp_Autenticacion_ObtenerCuentas(@Correo)` devuelve todas las cuentas con ese correo
     (usuarios de negocio y Super Administrador): hash, estado, estado del negocio y bloqueo
     vigente del correo.
  2. La aplicación verifica la contraseña contra cada hash con `PasswordHasher`.
  3. Según el resultado:
     - **0 coincidencias activas**: `usp_Autenticacion_RegistrarFallo`, que incrementa el
       contador por correo, bloquea al llegar al límite y audita.
     - **Hay coincidencias**: `usp_Autenticacion_RegistrarExito`, que reinicia el contador.
       - Con 1 cuenta, se crea la sesión.
       - Con varias, la persona elige. Los Id de las cuentas que coincidieron viajan en una
         cookie temporal protegida con Data Protection, que vence a los 5 minutos.
  4. Si la cuenta es de Super Administrador, antes de crear la sesión se envía el código del
     segundo factor (R7).
  5. Si `VerifyHashedPassword` devuelve `SuccessRehashNeeded`, se actualiza el hash.
- **Rationale**: la verificación del hash solo puede hacerse en la aplicación. Contar por
  correo cumple FR-014 aunque el negocio aún no se conozca.
- **Alternatives considered**: pedir el negocio antes de la contraseña. Rechazado en la
  aclaración Q1.

## R4. Hash de contraseñas

- **Decision**: `PasswordHasher<T>` de `Microsoft.Extensions.Identity.Core` (formato V3,
  PBKDF2-HMAC-SHA512).
  - Se obtiene con `<FrameworkReference Include="Microsoft.AspNetCore.App" />` en
    Infrastructure, sin paquete NuGet.
  - Application declara `IHasherContrasena` e Infrastructure lo implementa.
- **Rationale**: es el ejemplo que nombra el Principio VI y viene en el framework compartido.
- **Alternatives considered**: `Rfc2898DeriveBytes.Pbkdf2` directo, rechazado porque obliga a
  inventar un formato propio; BCrypt.Net, rechazado porque requiere un paquete NuGet.

## R5. Row-Level Security y contexto de sesión

- **Decision**: la función de predicado `seguridad.fn_PredicadoNegocio(@NegocioId)` permite la
  fila si se cumple cualquiera de estas condiciones:
  - `SESSION_CONTEXT(N'Ambito') = N'Negocio'` y `@NegocioId = SESSION_CONTEXT(N'NegocioId')`;
  - `SESSION_CONTEXT(N'Ambito') IN (N'Plataforma', N'Autenticacion')`.

  Aplicación de las políticas:
  - Se aplica como FILTER y BLOCK (AFTER INSERT, AFTER UPDATE) en todas las tablas con
    `NegocioId`.
  - En `Negocios` se aplica sobre la columna `Id`.

  La fábrica de conexiones (Scoped) abre cada conexión y ejecuta `sp_set_session_context` con
  `@read_only = 1` para `Ambito`, `NegocioId`, `UsuarioId` y `AutorTipo`, a partir de
  `IContextoUsuario`. Los ámbitos son:

  | Ámbito | Cuándo | NegocioId |
  |---|---|---|
  | `Negocio` | Usuario de negocio, o Super Administrador dentro de un negocio | el de la sesión |
  | `Plataforma` | Super Administrador en la vista de plataforma | NULL |
  | `Autenticacion` | Solo `IAutenticacionRepository`, antes de que exista una sesión | NULL |
  | `Anonimo` | Cualquier otra conexión sin sesión | NULL (no ve filas de negocio) |

  - El ámbito `Autenticacion` solo se obtiene con un método explícito de la fábrica
    (`CrearConexionAutenticacion`), que usa únicamente el repositorio de autenticación.
  - El usuario de base de datos de la aplicación solo tiene `EXECUTE` sobre los procedimientos
    y ningún permiso directo sobre las tablas.
- **Rationale**: cumple el Principio II. RLS es una segunda barrera aun si un procedimiento
  olvida filtrar por `@NegocioId`, y los valores de solo lectura no se pueden cambiar dentro
  de la sesión.
- **Alternatives considered**:
  - Desactivar RLS para la autenticación con `EXECUTE AS`: rechazado, RLS no exime al
    propietario y complicaría los permisos.
  - Una base de datos por negocio: rechazado, es desproporcionado al inicio.

## R6. Correo electrónico

- **Decision**: `System.Net.Mail.SmtpClient` (BCL) detrás de `IServicioCorreo`.
  - La configuración SMTP (host, puerto, usuario, contraseña, remitente) se lee de user secrets
    o de variables de entorno.
  - Las plantillas (asunto y cuerpo con marcadores `{{Nombre}}`, `{{Enlace}}`, `{{Codigo}}`,
    `{{Negocio}}`) son un catálogo global `PlantillasCorreo`, sembrado por script y editable
    por el Super Administrador (Principio IX).
  - El correo se envía después de confirmar la transacción. Si falla:
    - se registra en el log;
    - la operación se informa como exitosa con una advertencia ("no se pudo enviar el correo;
      reenvíe la invitación");
    - el usuario queda pendiente.
- **Rationale**: no requiere paquetes nuevos. El reenvío (FR-009) cubre las fallas.
- **Alternatives considered**: MailKit, recomendado por Microsoft, pero es un NuGet que
  requiere enmienda. Se puede adoptar después sin cambiar la interfaz.

## R7. Tokens de invitación, recuperación y segundo factor

- **Decision**: una tabla `TokensSeguridad` (Tipo, cuenta, hash SHA-256 del token, `ExpiraEnUtc`,
  `UsadoEnUtc`).
  - Los tokens de enlace son 32 bytes aleatorios en Base64Url.
  - El código del segundo factor tiene 6 dígitos, generados con `RandomNumberGenerator`.
  - Crear un token nuevo del mismo tipo invalida los anteriores (reenvío de invitación, FR-009).
  - Un token se consume una sola vez dentro de la transacción que define la contraseña.
  - Los vencimientos salen de `ConfiguracionSeguridad`.
  - Los tipos de token son constantes técnicas, permitidas por el Principio IV.
- **Rationale**: en la base de datos nunca queda un token usable (FR-029).
- **Alternatives considered**: tokens de Data Protection sin estado. Rechazado porque no
  permiten un solo uso ni invalidar al reenviar.

## R8. Configuración de seguridad y caché

- **Decision**: una tabla `ConfiguracionSeguridad` de una sola fila, con columnas tipadas y
  `RowVersion`:
  - `MaxIntentosFallidos` (5), `MinutosBloqueo` (15);
  - `MinutosEnlaceRecuperacion` (60), `HorasEnlaceInvitacion` (72);
  - `MinutosCodigoSegundoFactor` (10), `MinutosInactividadSesion` (30);
  - `LongitudMinimaContrasena` (8).

  Se lee mediante `IMemoryCache` (en Infrastructure) y la entrada se invalida al guardar desde
  la pantalla de plataforma.
- **Rationale**: FR-034 y FR-035, y los Principios IV y IX.
- **Alternatives considered**: una tabla clave/valor. Rechazada porque pierde el tipado y la
  validación.

## R9. Catálogo de permisos, acción de consulta y "acceso total"

- **Decision**: el catálogo global tiene tres tablas, sembradas por script:
  - `Modulos` (Codigo, Nombre, Orden);
  - `Acciones` (Codigo, Nombre, Orden, `EsConsulta`);
  - `Permisos` (ModuloId, AccionId, Codigo `Modulo.Accion`, `EsAdministracion`).

  Reglas que se apoyan en el catálogo:
  - **FR-020a**: la acción de consulta se identifica con la bandera `EsConsulta`, sin
    comparar contra el texto "Ver".
  - **FR-010**: los permisos con `EsAdministracion = 1` forman el conjunto que siempre debe
    tener al menos un usuario activo del negocio.
  - **FR-019a**: `Roles.EsAccesoTotal = 1` hace que los permisos efectivos sean todo el
    catálogo, calculado en `usp_Sesiones_Validar`, así que los permisos nuevos aplican sin
    asignarlos.

  Permisos iniciales:

  | Módulo | Acciones | EsAdministracion |
  |---|---|---|
  | Usuarios | Ver, Crear, Editar, CambiarEstado | Ver, Editar, CambiarEstado |
  | Roles | Ver, Crear, Editar, CambiarEstado | Ver, Editar |
  | Auditoria | Ver | — |

  - Las funciones de plataforma (negocios, configuración, plantillas y Super Administradores)
    no son permisos de catálogo: son exclusivas del ámbito `Plataforma`.
  - En C#, los códigos de permiso son constantes técnicas (`CodigosPermiso.UsuariosCrear`),
    que es el mecanismo de `Codigo` que autoriza el Principio IX.
- **Rationale**: banderas en vez de textos fijos, como exige el Principio IX.

## R10. Roles sugeridos

- **Decision**: catálogo global `PlantillasRol` + `PlantillaRolPermisos`, sembrado con:
  - Administrador (`EsAccesoTotal = 1`);
  - Recepción, Técnico y Bodega (`Usuarios.Ver` como permiso inicial y sin permisos
    administrativos).

  `usp_Negocios_Crear` copia la plantilla al negocio nuevo en la misma transacción. El Super
  Administrador puede editar la plantilla, y los cambios solo afectan a los negocios que se
  creen después.
  - Las pantallas de edición de plantillas, de rol y de correo, van como **última historia
    (P3)** del módulo (decisión del 2026-11-11). Hasta entonces rigen los valores sembrados.
- **Rationale**: FR-004, FR-034 y la regla de semillas del Principio IX.

## R11. Auditoría

- **Decision**: una tabla `Auditoria` de solo inserción:
  - Columnas: Id `bigint`, `NegocioId` NULL, `FechaUtc`, `AutorId`, `AutorTipo` (`U` usuario,
    `S` Super Administrador, `A` anónimo), `AutorNombre` (copia), `Entidad`, `EntidadId`,
    `Accion` y `Cambios` (JSON `[{campo, anterior, nuevo}]`).
  - Cada procedimiento que modifica datos llama a `usp_Auditoria_Registrar` dentro de su
    transacción. El autor se toma de `SESSION_CONTEXT`.
  - Los eventos de seguridad (FR-029) usan el mismo procedimiento.
  - Los accesos denegados los registra un filtro de autorización en Web, mediante
    `usp_Auditoria_RegistrarEvento`.
  - Las acciones del Super Administrador dentro de un negocio llevan el `NegocioId` de ese
    negocio, así que los auditores del negocio las ven (FR-026b).
  - RLS: un auditor de negocio ve solo su `NegocioId`; el ámbito `Plataforma` ve todo.
  - El usuario de la aplicación no tiene permiso de `UPDATE` ni `DELETE` sobre la tabla
    (FR-031).
- **Rationale**: es la regla de "auditoría dentro del mismo procedimiento" del Principio V.
- **Alternatives considered**:
  - Temporal tables o CDC: rechazados, no registran el autor de la aplicación.
  - Una fila por campo: rechazado, multiplica las filas sin aportar consultas útiles.

## R12. Logo del negocio

- **Decision**: el logo se guarda en `Archivos` (`VARBINARY(MAX)`) en su formato original,
  **PNG o JPEG, sin conversión ni redimensionado**.
  - `IValidadorImagenes` (Application) verifica el tipo real por la firma de bytes:
    - PNG: `89 50 4E 47 0D 0A 1A 0A`;
    - JPEG: `FF D8 FF`.
  - También verifica el tamaño (máximo 2 MB). No se confía en la extensión ni en el
    `Content-Type` del navegador.
  - `TipoContenido` guarda `image/png` o `image/jpeg` según la firma detectada.
  - La pantalla muestra el logo con CSS (`max-width`, `object-fit: contain`), así que no hace
    falta redimensionarlo.
- **Rationale**: es la decisión del dueño del proyecto (aclaración del 2026-11-11) y está en la
  constitución: formato original y ningún paquete de pago o con licencia restrictiva. Validar
  la firma solo usa la BCL.
- **Alternatives considered**:
  - Convertir a WebP con SkiaSharp: rechazado por decisión del dueño.
  - Usar SixLabors.ImageSharp: rechazado por licencia comercial.
  - Usar Magick.NET: rechazado, depende de binarios nativos grandes.
  - Redimensionar con `System.Drawing`: rechazado, solo funciona en Windows.

## R13. Migraciones con DbUp

- **Decision**: los scripts van en `Electronic.Infraestructure/Database/Scripts/NNNN_*.sql`
  como recursos embebidos.
  - `Database/Migraciones.cs` expone `AplicarMigraciones(string cadenaConexion)`.
  - `Electronic.Web` lo ejecuta al iniciar si `Migraciones:AplicarAlIniciar = true`, con una
    cadena de conexión separada `ConnectionStrings:DBMigraciones` que sí tiene permisos de DDL.
  - Las pruebas de integración usan el mismo método sobre una base vacía.
  - El primer Super Administrador se crea al iniciar: si no existe ninguno y
    `Instalacion:CorreoSuperAdministrador` tiene valor, se crea como pendiente y se le envía
    la invitación. Ninguna contraseña queda en scripts ni en configuración.
- **Rationale**: no agrega proyectos nuevos (Principio I) y la aplicación nunca corre con
  permisos de DDL en su conexión normal.
- **Alternatives considered**:
  - Un proyecto de consola para migraciones: rechazado por ahora, sería un proyecto nuevo sin
    necesidad.
  - Aplicar las migraciones con la conexión normal: rechazado, exigiría darle DDL al usuario de
    la aplicación.

## R14. Errores de base de datos y excepciones

- **Decision**:
  - Los procedimientos lanzan `THROW` con números reservados (ver
    [contracts/procedimientos-almacenados.md](contracts/procedimientos-almacenados.md#errores)).
  - Los repositorios traducen `SqlException` por número a excepciones de Domain:
    `ConflictoConcurrenciaException` y `ReglaNegocioException(codigo)`.
  - Los servicios atrapan solo esas excepciones y devuelven `ApiResponse` con 409 o 400 y un
    mensaje en español.
  - Cualquier otra excepción sube al manejador global de Web, que la registra con `ILogger` y
    muestra una página de error amigable (Principio VII).
- **Rationale**: elimina el `catch (Exception)` que descarta errores, que es la deuda del
  módulo Product.

## R15. Pruebas

- **Decision**:
  - **Unitarias** en `Electronic.Application.Tests` (xUnit + Moq): reglas de escalada
    (FR-008a, FR-020b), matriz (FR-020a), validadores, flujo de inicio de sesión con hasher y
    repositorio simulados, y mapeo de errores.
  - **Integración** en un proyecto nuevo `Electronic.Infrastructure.Tests` (xUnit), contra
    SQL Server real:
    - un fixture crea una base temporal con DbUp;
    - cubre RLS y aislamiento entre negocios (100% de los procedimientos de negocio),
      bloqueo por correo, última administración, concurrencia y auditoría.
  - **Ejecución sin SQL Server**: las pruebas de integración leen la variable de entorno
    `PRUEBAS_SQL`. Si no existe, se omiten con un motivo explícito (atributo propio que hereda
    de `FactAttribute`), para que `dotnet test` en el CI actual siga pasando.
  - **Fuera de este módulo** (decisión del 2026-11-11), en una funcionalidad de deuda técnica
    aparte: agregar SQL Server al CI y eliminar el proyecto vacío `Electronic.Tests`.
  - **Cobertura**: ≥ 80% en Domain y Application, medida con coverlet.
- **Alternatives considered**: Testcontainers, rechazado porque es un NuGet que requiere
  enmienda y el contenedor de servicio de GitHub Actions basta cuando se agregue.

## R16. Interfaz sin dependencias de terceros

- **Decision**:
  - `_Layout.cshtml` se reescribe con HTML semántico, sin referencias a Bootstrap ni a jQuery,
    y `site.css` con variables CSS (colores del negocio inyectados como custom properties) y
    diseño responsive.
  - Ninguna vista nueva usa `wwwroot/lib` ni `_ValidationScriptsPartial`.
  - `site.js` solo hace mostrar/ocultar, la vista previa del logo y el marcado automático de
    "Ver" en la matriz (FR-020a). El servidor vuelve a validar todo.
  - Hay un filtro global `AutoValidateAntiforgeryToken`.
  - **Fuera de este módulo** (decisión del 2026-11-11): borrar físicamente `wwwroot/lib` y
    `_ValidationScriptsPartial`, que dejan de usarse. Va en la funcionalidad de deuda técnica.
- **Rationale**: lo nuevo cumple el Principio III. La limpieza de archivos heredados se agrupa
  con el resto de la deuda.

## R18. Advertencias como errores y .editorconfig

- **Decision**: `TreatWarningsAsErrors` y `.editorconfig` **no se activan en este módulo**
  (decisión del 2026-11-11), porque romperían la compilación por las advertencias heredadas de
  `ProductService`.
  - Se activan en la funcionalidad de deuda técnica, junto con la corrección de Product.
  - Mientras tanto, el código nuevo DEBE compilar sin advertencias. Lo verifica la revisión del
    PR con la salida de `dotnet build`: ninguna advertencia nueva en los archivos del módulo.
- **Rationale**: evita mezclar la corrección de Product con este módulo, sin aflojar el
  criterio para el código nuevo.

## R17. Limitación de solicitudes en Web

- **Decision**: rate limiter integrado de ASP.NET Core en Web con la política `Autenticacion`:
  ventana fija de 10 solicitudes por minuto por IP en inicio de sesión, segundo factor y
  recuperación. Es complementaria al bloqueo por correo.
- **Rationale**: frena la adivinación de contraseñas repartida entre muchos correos, que el
  bloqueo por correo no detecta.
