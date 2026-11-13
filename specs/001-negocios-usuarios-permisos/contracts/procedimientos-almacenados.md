# Contrato: procedimientos almacenados

Es el contrato entre los repositorios de Infrastructure y la base de datos.

- **Plantilla obligatoria** de todos los procedimientos (Principio V): `SET NOCOUNT ON`,
  `TRY/CATCH` con `THROW`, y `SET XACT_ABORT ON` + transacción cuando modifican varias tablas.
- **Cambios auditados**: todo procedimiento que modifica datos llama a `usp_Auditoria_Registrar`
  dentro de su transacción. El autor se toma de `SESSION_CONTEXT`.
- **Abreviaturas de la tabla**:
  - `@N` = `@NegocioId INT`;
  - `@RV` = `@RowVersion BINARY(8)`;
  - `@P` = `@Pagina INT, @TamanoPagina INT`;
  - `TVP` = `dbo.TipoListaIds`.
- **Listados**: devuelven `TotalRegistros` con `COUNT(*) OVER()`.
- **Concurrencia en todo UPDATE** (constitución V, ver [data-model.md](../data-model.md#convenciones-comunes)):
  - las actualizaciones de usuario reciben `@RV`;
  - las de sistema (`usp_Sesiones_Validar`, `usp_Autenticacion_RegistrarFallo` y
    `_RegistrarExito`, `usp_Tokens_Consumir`) leen la `RowVersion` con `UPDLOCK, HOLDLOCK` y la
    validan en el `UPDATE`.
- **Columnas de registro**: todo `INSERT` y `UPDATE` llena las columnas de creación y
  modificación desde `SESSION_CONTEXT`.

## Autenticación (ámbito `Autenticacion`)

| Procedimiento | Parámetros | Resultado |
|---|---|---|
| usp_Autenticacion_ObtenerCuentas | @Correo | Cuentas: Id, Tipo, NegocioId, NegocioNombre, NegocioActivo, HashContrasena, Activo, Nombre + BloqueadoHastaUtc del correo |
| usp_Autenticacion_RegistrarFallo | @Correo | IntentosFallidos, BloqueadoHastaUtc. Audita `InicioSesionFallido` y, si corresponde, `Bloqueo` |
| usp_Autenticacion_RegistrarExito | @Correo, @CuentaId, @CuentaTipo | Reinicia el contador y audita `InicioSesion` |
| usp_Autenticacion_ActualizarHash | @CuentaId, @CuentaTipo, @HashContrasena | Rehash por `SuccessRehashNeeded` |
| usp_Sesiones_Crear | @HashIdentificador, @CuentaId, @CuentaTipo, @N NULL | SesionId |
| usp_Sesiones_Validar | @HashIdentificador | Validez, CuentaId, CuentaTipo, NegocioId, nombres, colores y logo del negocio + conjunto de permisos efectivos (FR-021, FR-019a) |
| usp_Sesiones_Revocar | @HashIdentificador | — |
| usp_Sesiones_RevocarOtras | @CuentaId, @CuentaTipo, @HashIdentificadorActual | — (FR-012) |
| usp_Sesiones_EntrarSoporte / usp_Sesiones_SalirSoporte | @HashIdentificador, @N | Audita `EntradaSoporte` y `SalidaSoporte` (FR-026b) |
| usp_Tokens_Crear | @Tipo, @CuentaId, @CuentaTipo, @HashToken, @ExpiraEnUtc | Invalida los anteriores del mismo tipo |
| usp_Tokens_Consumir | @Tipo, @HashToken, @HashContrasena NULL | Marca el token como usado y, si trae hash, define la contraseña (pendiente → activo). Audita. Error 50004 si el token es inválido o venció |
| usp_Tokens_ObtenerCuentasRecuperacion | @Correo | Cuentas activas con ese correo, con el nombre de su negocio, para armar un enlace por cuenta |

## Plataforma (ámbito `Plataforma`)

| Procedimiento | Parámetros | Notas |
|---|---|---|
| usp_Negocios_Listar | @Busqueda, @P | Sin el contenido del logo |
| usp_Negocios_Obtener | @Id | |
| usp_Negocios_Crear | datos, @LogoNombre, @LogoTipoContenido, @LogoContenido VARBINARY(MAX) NULL | Crea el negocio y el archivo, y copia `PlantillasRol` (FR-004). Error 50003 si el NIT ya existe |
| usp_Negocios_Actualizar | @Id, datos, logo opcional, @RV | 50001 / 50003 |
| usp_Negocios_CambiarEstado | @Id, @Activo, @RV | Audita `Activar` o `Desactivar` |
| usp_Negocios_CrearAdministradorInicial | @N, @Nombre, @Correo, @Telefono | Usuario pendiente con el rol de acceso total. Devuelve UsuarioId |
| usp_Configuracion_Obtener / usp_Configuracion_Actualizar | campos, @RV | |
| usp_PlantillasRol_Listar / _Obtener / _Guardar | @Id, datos, @Permisos TVP, @RV | |
| usp_PlantillasCorreo_Listar / _Obtener / _Actualizar | @Codigo, @Asunto, @CuerpoHtml, @RV | |
| usp_SuperAdministradores_Listar / _Crear / _CambiarEstado | | 50006 al desactivar el último |
| usp_CatalogoPermisos_Listar | — | Módulos, acciones y permisos con estado y RowVersion (incluye inactivos) |
| usp_Modulos_Actualizar / usp_Acciones_Actualizar / usp_Permisos_Actualizar | @Id, @Nombre, @Descripcion, @Orden, @RV | No modifican Codigo, EsConsulta ni EsAdministracion (FR-038) |
| usp_Modulos_CambiarEstado / usp_Permisos_CambiarEstado | @Id, @Activo, @RV | 50008 si se desactiva la consulta con otras acciones activas o un permiso `EsAdministracion` |
| usp_Roles_CambiarAccesoTotal | @N, @Id, @EsAccesoTotal, @RV | Solo ámbito Plataforma o soporte |

## Negocio (ámbito `Negocio`, siempre con `@NegocioId`)

| Procedimiento | Parámetros | Notas |
|---|---|---|
| usp_Usuarios_Listar | @N, @Busqueda, @P | Incluye el estado derivado y los nombres de los roles |
| usp_Usuarios_Obtener | @N, @Id | Datos + RolIds + RowVersion |
| usp_Usuarios_Crear | @N, @Nombre, @Correo, @Telefono, @Roles TVP | 50003 si el correo ya existe en el negocio |
| usp_Usuarios_Actualizar | @N, @Id, datos, @Roles TVP, @RV | 50001, 50002, 50003 |
| usp_Usuarios_CambiarEstado | @N, @Id, @Activo, @RV | 50002 al desactivar la última administración |
| usp_Roles_Listar | @N, @P | |
| usp_Roles_Obtener | @N, @Id | Datos + PermisoIds + EsAccesoTotal + RowVersion |
| usp_Roles_Crear | @N, @Nombre, @Descripcion | 50003 si el nombre ya existe |
| usp_Roles_Actualizar | @N, @Id, @Nombre, @Descripcion, @Permisos TVP, @RV | 50001, 50002, 50003, 50005 |
| usp_Roles_CambiarEstado | @N, @Id, @Activo, @RV | 50002 |
| usp_Permisos_ObtenerCatalogo | — | Módulos, acciones, permisos y banderas (se guarda en caché) |
| usp_Auditoria_Listar | @N NULL, @Desde, @Hasta, @AutorId, @Entidad, @Accion, @P | Con @N NULL solo funciona en el ámbito Plataforma |
| usp_Archivos_Obtener | @N, @Id | Contenido (único lugar donde se carga) |

## Auditoría (interna)

| Procedimiento | Parámetros | Notas |
|---|---|---|
| usp_Auditoria_Registrar | @N NULL, @Entidad, @EntidadId, @Accion, @Cambios NVARCHAR(MAX) | Lo llaman los demás procedimientos. Toma el autor de `SESSION_CONTEXT` |
| usp_Auditoria_RegistrarEvento | igual | Lo llama Web para `AccesoDenegado` |

## Errores

Números reservados para `THROW`. Los repositorios los traducen (ver
[research.md R14](../research.md#r14-errores-de-base-de-datos-y-excepciones)).

| Número | Significado | Traducción | HTTP en servicio |
|---|---|---|---|
| 50001 | Conflicto de concurrencia (`RowVersion` cambió) | `ConflictoConcurrenciaException` | 409 |
| 50002 | Se perdería la última administración del negocio (FR-010) | `ReglaNegocioException` | 400 |
| 50003 | Valor duplicado (NIT, correo o nombre de rol). También 2601/2627 | `ReglaNegocioException` | 400 |
| 50004 | Token inválido, vencido o usado | `ReglaNegocioException` | 400 |
| 50005 | Matriz inválida: acción sin su consulta (FR-020a) | `ReglaNegocioException` | 400 |
| 50006 | Se desactivaría el último Super Administrador | `ReglaNegocioException` | 400 |
| 50007 | Recurso inexistente o de otro negocio | `ReglaNegocioException` | 404 |
| 50008 | Desactivación de permiso no permitida (consulta con acciones activas, o permiso de administración) | `ReglaNegocioException` | 400 |
