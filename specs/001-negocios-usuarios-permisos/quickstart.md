# Quickstart: validar el módulo 1

Guía para comprobar de punta a punta que el módulo funciona. Los detalles están en
[data-model.md](data-model.md), [contracts/rutas-web.md](contracts/rutas-web.md) y
[contracts/procedimientos-almacenados.md](contracts/procedimientos-almacenados.md).

## Prerrequisitos

- .NET SDK 10.0.x
- SQL Server 2019 o posterior (LocalDB, Express o un contenedor `mcr.microsoft.com/mssql/server:2022-latest`)
- Un servidor SMTP de pruebas que capture los correos (por ejemplo, un contenedor de smtp4dev
  en `localhost:2525`)

## Configuración local (user secrets de Electronic.Web)

```powershell
dotnet user-secrets --project Electronic.Web set "ConnectionStrings:DBConnection"  "<cadena con usuario solo EXECUTE>"
dotnet user-secrets --project Electronic.Web set "ConnectionStrings:DBMigraciones" "<cadena con permisos DDL>"
dotnet user-secrets --project Electronic.Web set "Migraciones:AplicarAlIniciar" "true"
dotnet user-secrets --project Electronic.Web set "Instalacion:CorreoSuperAdministrador" "admin@local.test"
dotnet user-secrets --project Electronic.Web set "Smtp:Host" "localhost"
dotnet user-secrets --project Electronic.Web set "Smtp:Puerto" "2525"
dotnet user-secrets --project Electronic.Web set "Smtp:Remitente" "no-responder@local.test"
```

## Compilar y probar

```powershell
dotnet build Electronic.slnx            # sin advertencias nuevas (solo las 5 heredadas de ProductService)
dotnet test Electronic.Application.Tests --collect:"XPlat Code Coverage"   # ≥ 80% en Domain y Application
$env:PRUEBAS_SQL = "<cadena a SQL Server con permisos para crear bases>"
dotnet test Electronic.Infrastructure.Tests   # crea una base temporal con DbUp; sin PRUEBAS_SQL se omiten
```

Un test puntual: `dotnet test --filter "FullyQualifiedName~AislamientoNegociosTests"`.

## Ejecutar

```powershell
dotnet run --project Electronic.Web
```

Al iniciar, DbUp aplica los scripts y, si no hay ningún Super Administrador, invita a
`admin@local.test`.

## Escenarios de validación

| # | Pasos | Resultado esperado | Spec |
|---|---|---|---|
| 1 | Abrir el correo de invitación del Super Administrador y definir la contraseña | La cuenta pasa de pendiente a activa | FR-009, FR-027 |
| 2 | Iniciar sesión como Super Administrador | Pide el código enviado por correo. Al ingresarlo, entra a la vista de plataforma | FR-027a |
| 3 | Crear el negocio "Taller A" (con logo PNG) y su administrador inicial | El negocio queda activo, con 4 roles (Administrador con acceso total), y el logo se guarda como PNG sin cambios. El administrador recibe la invitación | US1, FR-004 |
| 3b | Subir como logo un `.gif` renombrado a `.png`, o un PNG de más de 2 MB | Rechazo con un mensaje claro (la firma de bytes no coincide, o se excede el tamaño) | Caso límite (logo inválido) |
| 4 | Crear "Taller B" con el NIT de "Taller A" | Rechazo: NIT duplicado | FR-003 |
| 5 | El administrador de A define su contraseña e inicia sesión | Ve los menús Usuarios, Roles y Auditoría | US2 |
| 6 | Crear el rol "Cajero" y marcar `Usuarios.Crear` | La matriz marca `Usuarios.Ver` sola. Al guardar, el rol queda con los 2 permisos | FR-020a |
| 7 | Crear un usuario Técnico; iniciar sesión con él; abrir `/usuarios` | El menú no muestra Usuarios. La URL directa da "Acceso denegado", auditado | US5, FR-023 |
| 8 | Con el Técnico en sesión, el administrador le quita el rol | En la siguiente acción, el Técnico pierde las opciones, sin volver a entrar | FR-022 |
| 9 | Fallar la contraseña 5 veces con un correo que existe en A y en B | Las cuentas de A y B quedan bloqueadas 15 minutos | FR-014 |
| 10 | Un usuario de A pide `/usuarios/{id}/editar` con un Id de B | 404 | FR-025 |
| 11 | El administrador de A intenta desactivarse a sí mismo o quitar el último rol administrativo | Rechazo: el negocio no puede quedar sin administración | FR-008a, FR-010 |
| 12 | Dos pestañas editan el mismo rol; guardar ambas | La segunda recibe un aviso de conflicto | Caso límite |
| 13 | El Super Administrador entra a soporte en A, cambia un teléfono y sale | Se ve el aviso de soporte. La auditoría de A muestra entrada, cambio y salida con su identidad | FR-026a/b |
| 14 | Desactivar "Taller A" con usuarios en sesión | Pierden el acceso en la siguiente acción y no pueden volver a entrar | FR-005 |
| 15 | Consultar la auditoría de A | Autor, fecha, valor anterior y nuevo; nunca contraseñas | US6, FR-028–031 |

## Verificación de aislamiento en la base de datos

En SSMS, con el usuario de la aplicación:

```sql
EXEC sp_set_session_context N'Ambito', N'Negocio', 1;
EXEC sp_set_session_context N'NegocioId', <IdTallerA>, 1;
SELECT COUNT(*) FROM dbo.Usuarios WHERE NegocioId = <IdTallerB>;  -- debe dar error de permiso (sin SELECT directo)
```

Con un usuario de pruebas que tenga `SELECT`, la misma consulta devuelve 0 filas, por RLS.
