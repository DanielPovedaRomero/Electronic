# Data Model: Negocios, usuarios, roles y permisos

**Fecha**: 2026-11-11 | **Plan**: [plan.md](plan.md) | **Decisiones**: [research.md](research.md)

## Convenciones comunes

- **Claves**: `Id INT IDENTITY` (`BIGINT` en `Auditoria`).
- **Fechas**: todas en UTC (`DATETIME2(0)`, sufijo `Utc`).
- **Columnas de registro**: toda tabla de entidad tiene `CreadoPorId`, `CreadoPorTipo CHAR(1)`,
  `CreadoEnUtc`, `ModificadoPorId`, `ModificadoPorTipo` y `ModificadoEnUtc`. El tipo de autor
  es `U` (usuario), `S` (Super Administrador) o `A` (anónimo).
- **Concurrencia**: toda tabla de entidad tiene `RowVersion ROWVERSION`. Los
  `usp_*_Actualizar` reciben `@RowVersion` y la validan en el `WHERE`.
- **Borrado**: no hay borrado físico de entidades, solo `Activo BIT`. Las filas de relación
  (`RolPermisos`, `UsuarioRoles`) se reemplazan como conjunto dentro de una transacción
  auditada.
- **Correos**: se guardan como se escribieron y además en `CorreoNormalizado` (minúsculas, sin
  espacios), que es el que se usa para la unicidad.
- **RLS**: aplica a toda tabla marcada "RLS" (ver [research.md R5](research.md#r5-row-level-security-y-contexto-de-sesión)).

En los modelos C# los nombres van en español (`NegocioModel`, `UsuarioModel`, ...), según la
constitución.

---

## Entidades de plataforma (sin NegocioId)

### Negocios (RLS sobre `Id`)

| Campo | Tipo | Reglas |
|---|---|---|
| Id | INT | PK |
| Nombre | NVARCHAR(150) | Obligatorio |
| Nit | NVARCHAR(30) | Obligatorio, **único en la plataforma** (FR-003) |
| Direccion | NVARCHAR(250) | |
| Telefono | NVARCHAR(30) | |
| CorreoContacto | NVARCHAR(256) | Obligatorio, formato de correo |
| Horario | NVARCHAR(250) | |
| ColorPrimario, ColorSecundario | CHAR(7) | Formato `#RRGGBB` |
| TextoAgradecimiento | NVARCHAR(1000) | |
| TextoCondiciones | NVARCHAR(4000) | |
| DiasResguardo | INT | `>= 0` (CHECK) |
| LogoArchivoId | INT NULL | FK → Archivos |
| Activo | BIT | Por defecto 1 |

**Estados**: Activo ⇄ Inactivo, solo por el Super Administrador. Mientras está inactivo:
`usp_Sesiones_Validar` rechaza las sesiones de sus usuarios y el inicio de sesión lo rechaza
(FR-005). El Super Administrador puede entrar igual (FR-026a).

### SuperAdministradores

| Campo | Tipo | Reglas |
|---|---|---|
| Id | INT | PK |
| Nombre | NVARCHAR(150) | Obligatorio |
| Correo, CorreoNormalizado | NVARCHAR(256) | Único en la tabla |
| HashContrasena | NVARCHAR(500) NULL | NULL = pendiente |
| Activo | BIT | |

No se puede desactivar el último Super Administrador activo (error 50006).

### Catálogo de permisos (global, sembrado)

| Tabla | Campos | Reglas |
|---|---|---|
| Modulos | Id, Codigo (único), Nombre, Orden | |
| Acciones | Id, Codigo (único), Nombre, Orden, **EsConsulta** BIT | Exactamente una acción de consulta en el catálogo (Ver) |
| Permisos | Id, ModuloId, AccionId, Codigo (`Modulo.Accion`, único), Descripcion, **EsAdministracion** BIT | UNIQUE(ModuloId, AccionId) |

Todo módulo con permisos DEBE tener su permiso de consulta (FR-020a).

### PlantillasRol / PlantillaRolPermisos (global, sembrado)

| Tabla | Campos |
|---|---|
| PlantillasRol | Id, Nombre, Descripcion, EsAccesoTotal, Orden, Activo |
| PlantillaRolPermisos | PlantillaRolId, PermisoId |

`usp_Negocios_Crear` las copia a `Roles` y `RolPermisos` del negocio nuevo (FR-004).

### ConfiguracionSeguridad (una sola fila)

| Campo | Defecto | Validación |
|---|---|---|
| MaxIntentosFallidos | 5 | 1–20 |
| MinutosBloqueo | 15 | 1–1440 |
| MinutosEnlaceRecuperacion | 60 | 5–1440 |
| HorasEnlaceInvitacion | 72 | 1–720 |
| MinutosCodigoSegundoFactor | 10 | 1–60 |
| MinutosInactividadSesion | 30 | 5–720 |
| LongitudMinimaContrasena | 8 | 8–128 (FR-015: nunca menos de 8) |

Solo el Super Administrador la edita (FR-035). Cada cambio se audita.

### PlantillasCorreo (global, sembrado)

| Campo | Reglas |
|---|---|
| Id, Codigo | Código técnico único: `Invitacion`, `Recuperacion`, `SegundoFactor` |
| Asunto | NVARCHAR(200) |
| CuerpoHtml | NVARCHAR(MAX), con marcadores `{{Nombre}}`, `{{Negocio}}`, `{{Enlace}}`, `{{Codigo}}`, `{{Vence}}` |

### ControlIntentos (por correo, FR-014)

| Campo | Reglas |
|---|---|
| CorreoNormalizado | PK |
| IntentosFallidos | Se reinicia con un inicio de sesión exitoso |
| BloqueadoHastaUtc | NULL = sin bloqueo |

Lo actualizan atómicamente `usp_Autenticacion_RegistrarFallo` y `usp_Autenticacion_RegistrarExito`.

### Sesiones

| Campo | Reglas |
|---|---|
| Id | BIGINT PK |
| HashIdentificador | BINARY(32), único (SHA-256 del valor de la cookie) |
| CuentaId, CuentaTipo | `U` → Usuarios, `S` → SuperAdministradores |
| NegocioId | NULL en la vista de plataforma. Con valor cuando es usuario de negocio o Super Administrador en soporte |
| CreadaEnUtc, UltimaActividadUtc | |
| RevocadaEnUtc | NULL = vigente |

**Transiciones**: creada → vigente → revocada, por cierre de sesión, cambio de contraseña o
desactivación. Una sesión también es inválida si supera la inactividad configurada.

### TokensSeguridad

| Campo | Reglas |
|---|---|
| Id | PK |
| Tipo | `Invitacion`, `Recuperacion` o `SegundoFactor` (constante técnica) |
| CuentaId, CuentaTipo | |
| HashToken | BINARY(32) |
| ExpiraEnUtc, UsadoEnUtc | |

Crear un token invalida los anteriores vigentes del mismo tipo y cuenta (reenvío, FR-009).

---

## Entidades de negocio (con NegocioId, RLS)

### Usuarios

| Campo | Tipo | Reglas |
|---|---|---|
| Id | INT | PK |
| NegocioId | INT | FK → Negocios |
| Nombre | NVARCHAR(150) | Obligatorio |
| Correo, CorreoNormalizado | NVARCHAR(256) | UNIQUE(NegocioId, CorreoNormalizado) (FR-007) |
| Telefono | NVARCHAR(30) | |
| HashContrasena | NVARCHAR(500) NULL | NULL = **pendiente** |
| Activo | BIT | |

**Estados** (FR-009): no hay una columna de estado; el estado se deriva de `HashContrasena` y
`Activo`.

```text
Pendiente (HashContrasena NULL, Activo 1) --define contraseña--> Activo
Activo <--> Inactivo (Activo 0)
Pendiente --desactivar--> Inactivo (sigue sin contraseña)
```

**Reglas**:
- Al menos un rol (FR-008).
- Ningún usuario modifica sus propios roles (FR-008a).
- No se puede dejar el negocio sin un usuario activo que tenga todos los permisos con
  `EsAdministracion` (FR-010, error 50002).

### Roles

| Campo | Tipo | Reglas |
|---|---|---|
| Id | INT | PK |
| NegocioId | INT | FK |
| Nombre | NVARCHAR(100) | UNIQUE(NegocioId, Nombre) |
| Descripcion | NVARCHAR(500) | |
| EsAccesoTotal | BIT | Solo la cambia el Super Administrador (FR-019a) |
| Activo | BIT | Inactivo: sus permisos no suman, pero las asignaciones se conservan |

### RolPermisos / UsuarioRoles (relaciones, RLS)

| Tabla | Campos | Reglas |
|---|---|---|
| RolPermisos | NegocioId, RolId, PermisoId | PK(RolId, PermisoId). Incluye el permiso de consulta de cada módulo que tenga otra acción (FR-020a, error 50005) |
| UsuarioRoles | NegocioId, UsuarioId, RolId | PK(UsuarioId, RolId). El rol y el usuario pertenecen al mismo NegocioId |

**Permisos efectivos** (FR-021):
- Si el usuario tiene algún rol **activo** con `EsAccesoTotal`, son todo el catálogo.
- Si no, son la unión de `RolPermisos` de sus roles activos.

### Archivos (RLS)

| Campo | Reglas |
|---|---|
| Id, NegocioId | |
| NombreOriginal | NVARCHAR(255) |
| TipoContenido | `image/png` o `image/jpeg`, según la firma de bytes detectada. Sin conversión |
| TamanoBytes | |
| Contenido | VARBINARY(MAX). **Nunca se carga en listados** |

### Auditoria (RLS, solo inserción)

| Campo | Reglas |
|---|---|
| Id | BIGINT PK |
| NegocioId | NULL en eventos de plataforma o con correo sin cuenta |
| FechaUtc | |
| AutorId, AutorTipo, AutorNombre | AutorNombre es una copia del nombre en el momento del evento |
| Entidad | `Negocio`, `Usuario`, `Rol`, `RolPermisos`, `UsuarioRoles`, `ConfiguracionSeguridad`, `PlantillaRol`, `PlantillaCorreo`, `SuperAdministrador` o `Sesion` |
| EntidadId | |
| Accion | `Crear`, `Modificar`, `Activar`, `Desactivar`, `AsignarPermisos`, `AsignarRoles`, `InicioSesion`, `InicioSesionFallido`, `Bloqueo`, `CambioContrasena`, `RecuperacionContrasena`, `AccesoDenegado`, `EntradaSoporte`, `SalidaSoporte` o `ReenvioInvitacion` |
| Cambios | NVARCHAR(MAX) JSON `[{"campo","anterior","nuevo"}]`. Nunca contiene hashes, tokens ni contraseñas |

Índices: (NegocioId, FechaUtc DESC), (NegocioId, Entidad, EntidadId) y (NegocioId, AutorId).

---

## Tipos de tabla (TVP)

| Tipo | Definición | Uso |
|---|---|---|
| `dbo.TipoListaIds` | `(Id INT NOT NULL PRIMARY KEY)` | Permisos de un rol, roles de un usuario, permisos de una plantilla |

## Relaciones

```text
Negocios 1─* Usuarios *─* Roles (UsuarioRoles)
Negocios 1─* Roles *─* Permisos (RolPermisos)
Permisos *─1 Modulos, Permisos *─1 Acciones
Negocios 0..1─1 Archivos (logo)
PlantillasRol *─* Permisos (PlantillaRolPermisos)
Sesiones / TokensSeguridad → Usuarios | SuperAdministradores (CuentaTipo)
Auditoria → Negocios (opcional)
```

## Validaciones de aplicación (validadores estáticos, mensajes en español)

| DTO | Reglas |
|---|---|
| NegocioDto | Nombre, Nit y CorreoContacto obligatorios; correo válido; colores `#RRGGBB`; DiasResguardo ≥ 0; longitudes máximas |
| UsuarioDto | Nombre y correo obligatorios; correo válido; al menos un rol |
| RolDto | Nombre obligatorio (≤ 100) |
| MatrizPermisosDto | Toda acción marcada incluye la consulta de su módulo (FR-020a) |
| CambioContrasenaDto | Longitud ≥ `LongitudMinimaContrasena`; al menos una letra y un número; distinta de la actual |
| ConfiguracionSeguridadDto | Rangos de la tabla anterior |
| Logo | Firma PNG o JPEG (sin conversión); tamaño ≤ 2 MB |
