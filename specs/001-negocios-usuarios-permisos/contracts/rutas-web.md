# Contrato: rutas web (Electronic.Web)

Pantallas renderizadas en el servidor.

- Todo `POST` exige un token anti-forgery.
- **Permiso**: lo que exige `[RequierePermiso]`.
  - **Plataforma** = solo el Super Administrador en la vista de plataforma.
  - **Anónimo** = no requiere sesión.
- Un acceso sin permiso responde 403 con la página "Acceso denegado" y queda auditado
  (FR-023).
- Un recurso de otro negocio responde 404 (FR-025).

## Cuenta (autenticación)

| Método | Ruta | Permiso | Descripción | Requisitos |
|---|---|---|---|---|
| GET/POST | `/cuenta/iniciar-sesion` | Anónimo + límite `Autenticacion` | Correo + contraseña | FR-011, FR-014 |
| GET/POST | `/cuenta/seleccionar-negocio` | Cookie temporal de preautenticación | Elegir el negocio si hay varias cuentas | FR-011 |
| GET/POST | `/cuenta/segundo-factor` | Cookie temporal de preautenticación + límite `Autenticacion` | Código del Super Administrador | FR-027a |
| POST | `/cuenta/cerrar-sesion` | Sesión | Revoca la sesión | FR-011 |
| GET/POST | `/cuenta/recuperar` | Anónimo + límite `Autenticacion` | Solicitar el enlace; siempre muestra la misma confirmación | FR-013 |
| GET/POST | `/cuenta/definir-contrasena?token=` | Anónimo | Invitación o recuperación; un solo uso | FR-009, FR-013 |
| GET/POST | `/cuenta/cambiar-contrasena` | Sesión | Exige la contraseña actual; cierra las demás sesiones | FR-012 |
| GET | `/cuenta/perfil` | Sesión | Datos propios; único acceso de un usuario sin roles activos | Caso límite |
| GET | `/acceso-denegado` | Sesión | Mensaje claro | FR-023 |

## Plataforma (Super Administrador)

| Método | Ruta | Permiso | Descripción | Requisitos |
|---|---|---|---|---|
| GET | `/plataforma/negocios?busqueda=&pagina=` | Plataforma | Listado paginado por nombre o NIT | FR-001 |
| GET/POST | `/plataforma/negocios/crear` | Plataforma | Datos + logo; copia los roles sugeridos | FR-001–004 |
| GET/POST | `/plataforma/negocios/{id}/editar` | Plataforma | Con `RowVersion` | FR-001 |
| POST | `/plataforma/negocios/{id}/estado` | Plataforma | Activar o desactivar | FR-005 |
| GET/POST | `/plataforma/negocios/{id}/administrador-inicial` | Plataforma | Crea el usuario con el rol de acceso total y lo invita | FR-026 |
| POST | `/plataforma/negocios/{id}/entrar` | Plataforma | Inicia soporte: la sesión toma ese NegocioId; queda auditado | FR-026a/b |
| POST | `/soporte/salir` | Super Administrador en soporte | Vuelve a la plataforma; queda auditado | FR-026a/b |
| GET/POST | `/plataforma/configuracion` | Plataforma | Valores de seguridad | FR-035 |
| GET/POST | `/plataforma/plantillas-rol[/{id}]` | Plataforma | Roles sugeridos y sus permisos (P3) | FR-034 |
| GET/POST | `/plataforma/plantillas-correo[/{id}]` | Plataforma | Asunto y cuerpo de los correos (P3) | Principio IX |
| GET/POST | `/plataforma/catalogo-permisos[/modulos/{id}\|/permisos/{id}]` | Plataforma | Nombre, descripción, orden y estado de módulos, acciones y permisos. Códigos de solo lectura (P3) | FR-038 |
| GET/POST | `/plataforma/super-administradores[/crear]`, `POST .../{id}/estado` | Plataforma | Altas y estado; nunca el último activo | FR-027 |
| POST | `/plataforma/roles/{id}/acceso-total` | Plataforma o soporte | Marca o desmarca la bandera | FR-019a |
| GET | `/plataforma/auditoria` | Plataforma | Auditoría de todos los negocios | FR-030 |

## Negocio

| Método | Ruta | Permiso | Descripción | Requisitos |
|---|---|---|---|---|
| GET | `/usuarios?busqueda=&pagina=` | `Usuarios.Ver` | Listado paginado | FR-006 |
| GET/POST | `/usuarios/crear` | `Usuarios.Crear` | Solo ofrece roles cuyos permisos tiene el autor | FR-006–009, FR-008a |
| GET/POST | `/usuarios/{id}/editar` | `Usuarios.Editar` | Datos y roles; los roles propios no se pueden editar | FR-008a |
| POST | `/usuarios/{id}/estado` | `Usuarios.CambiarEstado` | Protege la última administración | FR-010 |
| POST | `/usuarios/{id}/reenviar-invitacion` | `Usuarios.Editar` | Invalida el enlace anterior | FR-009 |
| GET | `/roles?pagina=` | `Roles.Ver` | Listado | FR-017 |
| GET/POST | `/roles/crear` | `Roles.Crear` | Nombre y descripción | FR-017 |
| GET/POST | `/roles/{id}/editar` | `Roles.Editar` | Datos + matriz de permisos | FR-020, FR-020a/b |
| POST | `/roles/{id}/estado` | `Roles.CambiarEstado` | Protege la última administración | FR-010 |
| GET | `/auditoria?desde=&hasta=&autor=&entidad=&accion=&pagina=` | `Auditoria.Ver` | Solo el negocio propio | FR-030 |
| GET | `/archivos/{id}` | Sesión | Devuelve el PNG o JPEG con su `TipoContenido` guardado; filtrado por RLS | Principio V |

## Matriz de permisos (formulario de `/roles/{id}/editar`)

- **Filas**: módulos del catálogo. **Columnas**: acciones. Solo hay casilla donde existe el
  permiso (FR-020).
- **Casillas deshabilitadas** (FR-020b): los permisos que el autor no tiene, y todas si el rol
  es del autor. El Super Administrador no tiene restricciones.
- **Rol con `EsAccesoTotal`**: matriz completa, solo lectura (FR-019a).
- **`site.js`** marca la consulta al marcar otra acción del módulo, y desmarca el módulo al
  quitar la consulta (FR-020a).
- **El servidor revalida todo**:
  - permisos fuera del autor → 400 + auditoría `AccesoDenegado`;
  - acción marcada sin su consulta → 400.
- **Envío**: lista de `PermisoId` + `RowVersion` del rol. Se guarda con un TVP.

## Menú

Se genera en el servidor a partir de los permisos efectivos (FR-024). Cada entrada declara el
permiso de consulta de su módulo. Las entradas de plataforma solo aparecen en el ámbito
Plataforma. En soporte, un aviso fijo dice "Soporte en {Negocio}" e incluye el botón "Salir".
