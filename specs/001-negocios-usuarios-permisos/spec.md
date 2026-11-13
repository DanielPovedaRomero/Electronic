# Feature Specification: Negocios, usuarios, roles y permisos

**Feature Branch**: `001-negocios-usuarios-permisos`

**Created**: 2026-11-10

**Status**: Draft

**Input**: User description: "Módulo 1: Negocios, usuarios, roles y permisos. Base de un sistema
multinegocio para talleres de reparación con tienda de productos: administración de negocios por
el Super Administrador, usuarios por negocio, inicio de sesión con bloqueo por intentos fallidos,
roles configurables por negocio, catálogo global de permisos Módulo.Acción asignados en matriz,
control de acceso en pantallas y menús, y auditoría de cambios. Fuera de alcance: portal del
cliente final, cobro de mensualidades e inicio de sesión con redes sociales."

## Clarifications

### Session 2026-11-10

- Q: Si una persona tiene el mismo correo en varios negocios, ¿cómo sabe el sistema a qué
  negocio entra? → A: Inicia sesión con correo y contraseña. Si coinciden cuentas de varios
  negocios, elige el negocio tras autenticarse. Hay una cuenta por negocio, cada una con su
  contraseña.
- Q: ¿El Super Administrador puede entrar a la información interna de un negocio? → A: Sí, con
  acceso completo para soporte. Cada entrada, acción y salida queda auditada con su identidad.
- Q: ¿Un usuario que administra usuarios puede asignar roles con permisos que él no tiene? → A:
  No. Solo asigna roles cuyos permisos ya tiene, y nadie modifica sus propios roles.
- Q: ¿Las cuentas de Super Administrador usan un segundo factor? → A: Sí, un código de un solo
  uso enviado por correo en cada inicio de sesión.
- Q: ¿Los valores de seguridad son globales o por negocio? → A: Globales para toda la
  plataforma, y solo el Super Administrador los ajusta.
- Q: ¿Marcar Editar o Crear marca automáticamente Ver del mismo módulo? → A: Sí. Desmarcar Ver
  desmarca las demás acciones de ese módulo.

### Session 2026-11-11

- Q: ¿Cómo recibe el rol de administración de cada negocio los permisos que agreguen los módulos
  futuros? → A: Con una bandera "acceso total" en el rol, que solo el Super Administrador marca.
  Esos roles reciben automáticamente todo permiso nuevo, y el rol sugerido Administrador viene
  con la bandera.
- Q: Si una persona tiene el mismo correo en varios negocios, ¿qué se bloquea tras los intentos
  fallidos? → A: Los intentos se cuentan por correo en toda la plataforma. Al llegar al límite se
  bloquean todas las cuentas con ese correo.
- Q: ¿Cuánto dura el enlace de invitación de un usuario nuevo y qué pasa si vence? → A: Vence a
  las 72 horas (configurable). El administrador puede reenviarlo, el enlace anterior deja de
  servir, y el usuario queda "pendiente" hasta definir su contraseña.
- Q: ¿En qué formato se guarda el logo del negocio? → A: En su formato original, PNG o JPEG,
  sin conversión. No se usan paquetes de pago ni con licencia restrictiva. (Decidido durante
  /speckit-plan.)
- Q: ¿Las plantillas de roles sugeridos y de correos se pueden editar desde el sistema en este
  módulo? → A: Sí, las edita el Super Administrador. Es la funcionalidad de menor prioridad del
  módulo. (Decidido durante /speckit-plan.)

### Session 2026-11-12

Decisiones tomadas durante /speckit-analyze:

- Q: ¿El catálogo de permisos es administrable desde el sistema? → A: Sí. El Super
  Administrador edita nombres, descripciones y orden, y activa o desactiva módulos y permisos.
  Los códigos son fijos (FR-038).
- Q: ¿Con qué permisos se crean los roles sugeridos Recepción, Técnico y Bodega? → A: Sin
  permisos. Los módulos futuros se los agregan.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Super Administrador da de alta un negocio listo para operar (Priority: P1)

El Super Administrador de la plataforma registra un negocio nuevo con sus datos e imagen. Al
guardarlo, el negocio queda con sus roles iniciales sugeridos (Administrador, Recepción, Técnico,
Bodega), y el Super Administrador registra al primer usuario administrador del negocio. Ese
usuario recibe un correo para definir su contraseña.

**Why this priority**: sin un negocio y su primer administrador no existe nada sobre lo que
trabajar. Es el punto de partida de todos los módulos.

**Independent Test**: el Super Administrador crea un negocio y su administrador inicial, y se
comprueba que el negocio aparece con sus 4 roles sugeridos y que el administrador recibe el
correo de activación.

**Acceptance Scenarios**:

1. **Given** el Super Administrador con sesión iniciada, **When** registra un negocio con nombre,
   NIT, dirección, teléfono, correo de contacto, horario, logo, colores, texto de agradecimiento,
   texto de condiciones y días de resguardo, **Then** el negocio queda activo y con los roles
   Administrador, Recepción, Técnico y Bodega creados como roles editables del propio negocio.
2. **Given** un negocio recién creado, **When** el Super Administrador registra su usuario
   administrador inicial con nombre, correo y teléfono, **Then** el usuario queda asociado a ese
   negocio con el rol Administrador y recibe un correo para definir su contraseña.
3. **Given** un negocio existente, **When** el Super Administrador intenta registrar otro negocio
   con el mismo NIT, **Then** el sistema rechaza el registro e indica que el NIT ya existe.
4. **Given** un negocio activo con usuarios con sesión iniciada, **When** el Super Administrador
   lo desactiva, **Then** esos usuarios pierden el acceso en su siguiente acción y nadie del
   negocio puede iniciar sesión hasta que se reactive.
5. **Given** el Super Administrador en la vista de plataforma, **When** entra a un negocio para
   dar soporte, **Then** tiene acceso completo a ese negocio, ve un indicador visible de que
   actúa como soporte, y su entrada, sus acciones y su salida quedan en la auditoría del negocio
   con su propia identidad.

---

### User Story 2 - Usuario inicia sesión, cierra sesión y gestiona su contraseña (Priority: P1)

Un usuario de un negocio entra con su correo y contraseña. Puede cerrar sesión, cambiar su
contraseña y recuperarla por correo si la olvidó. Tras varios intentos fallidos seguidos, su
cuenta se bloquea temporalmente.

**Why this priority**: ningún módulo es utilizable sin autenticación, y el bloqueo protege las
cuentas desde el primer día.

**Independent Test**: con un usuario activo de un negocio activo, se prueba el inicio y el
cierre de sesión, el cambio de contraseña, la recuperación por correo y el bloqueo tras los
intentos fallidos configurados.

**Acceptance Scenarios**:

1. **Given** un usuario activo de un negocio activo, **When** ingresa su correo y contraseña
   correctos, **Then** entra al sistema y ve solo las opciones que sus permisos permiten.
2. **Given** credenciales incorrectas, **When** el usuario intenta entrar, **Then** ve un mensaje
   genérico que no revela si el correo existe.
3. **Given** un usuario que alcanza el número configurado de intentos fallidos seguidos, **When**
   vuelve a intentar entrar, aunque sea con la contraseña correcta, **Then** el sistema rechaza
   el acceso hasta que termina el tiempo de bloqueo configurado.
4. **Given** un usuario que olvidó su contraseña, **When** solicita recuperarla con su correo,
   **Then** recibe un enlace de un solo uso y con vencimiento. Al usarlo, define una contraseña
   nueva y el enlace deja de servir.
5. **Given** un usuario con sesión iniciada, **When** cambia su contraseña escribiendo la actual
   y la nueva, **Then** la nueva contraseña queda vigente y sus otras sesiones abiertas se cierran.
6. **Given** un usuario inactivo, o de un negocio inactivo, **When** intenta entrar con
   credenciales correctas, **Then** el acceso se rechaza.
7. **Given** una persona con cuentas con el mismo correo y la misma contraseña en dos negocios
   activos, **When** inicia sesión, **Then** elige en cuál negocio entrar y solo ve los datos de
   ese negocio.

---

### User Story 3 - Administrador del negocio configura roles y permisos (Priority: P2)

Un usuario con permiso de administración de roles crea, edita, activa o desactiva roles de su
negocio, y asigna permisos a cada rol marcando casillas en una matriz de módulos por acciones.

**Why this priority**: permite que cada taller se organice a su manera. Los roles iniciales
cubren la operación básica mientras tanto.

**Independent Test**: se crea un rol "Cajero" con dos permisos marcados en la matriz y se
comprueba que un usuario con ese rol obtiene exactamente esos permisos.

**Acceptance Scenarios**:

1. **Given** un administrador del negocio, **When** crea un rol con nombre y descripción,
   **Then** el rol aparece solo en su negocio y queda activo.
2. **Given** la matriz de permisos de un rol, **When** el administrador marca o desmarca
   casillas y guarda, **Then** los permisos del rol quedan exactamente como se marcaron, y los
   usuarios con ese rol ven el cambio en su siguiente acción.
3. **Given** un rol con usuarios asignados, **When** el administrador lo desactiva, **Then** sus
   permisos dejan de sumar para esos usuarios, pero la asignación se conserva para cuando el rol
   se reactive.
4. **Given** dos roles del mismo negocio, **When** se intenta guardar uno con el mismo nombre que
   el otro, **Then** el sistema lo rechaza.

---

### User Story 4 - Administrador del negocio gestiona sus usuarios (Priority: P2)

Un usuario con permiso de administración de usuarios registra, edita, activa o desactiva usuarios
de su negocio y les asigna uno o varios roles.

**Why this priority**: es necesario para incorporar al personal del taller, pero el
administrador inicial basta para empezar.

**Independent Test**: se crea un usuario con dos roles que tienen permisos distintos (por
ejemplo, uno con `Usuarios.Ver` y otro con `Roles.Ver`) y se verifica que sus permisos son la
suma de ambos roles.

**Acceptance Scenarios**:

1. **Given** un administrador del negocio, **When** registra un usuario con nombre, correo,
   teléfono y al menos un rol, **Then** el usuario queda "pendiente" en su negocio y recibe un
   correo para definir su contraseña. Al definirla, pasa a activo.
2. **Given** un correo que ya usa otro usuario del mismo negocio, **When** se intenta registrar
   un usuario con ese correo, **Then** el sistema lo rechaza.
3. **Given** un usuario activo, **When** el administrador lo desactiva, **Then** el usuario pierde
   el acceso en su siguiente acción.
4. **Given** que solo queda un usuario activo con permiso para administrar usuarios y roles,
   **When** alguien intenta desactivarlo o quitarle ese permiso, **Then** el sistema lo impide
   para que el negocio no quede sin administración.

---

### User Story 5 - Control de acceso en pantallas, acciones y menús (Priority: P2)

Cada pantalla y cada acción del sistema exige el permiso Módulo.Acción correspondiente. Los menús
muestran solo lo que el usuario puede usar.

**Why this priority**: es lo que hace efectivos los roles. Sin control de acceso, los permisos
no tienen efecto.

**Independent Test**: un usuario con solo el rol Técnico ve un menú reducido y recibe un aviso
de acceso denegado al abrir directamente la dirección de una pantalla sin permiso.

**Acceptance Scenarios**:

1. **Given** un usuario sin el permiso Usuarios.Ver, **When** abre el menú, **Then** la opción
   de usuarios no aparece.
2. **Given** el mismo usuario, **When** accede directamente a la pantalla o acción de usuarios,
   **Then** el sistema le niega el acceso con un mensaje claro y registra el intento.
3. **Given** un usuario del negocio A, **When** intenta ver o modificar cualquier dato del
   negocio B, aunque conozca su identificador, **Then** el sistema responde como si el dato no
   existiera.

---

### User Story 6 - Consultar la auditoría de cambios (Priority: P3)

Un usuario con permiso de auditoría consulta quién creó, modificó, activó o desactivó negocios,
usuarios, roles y permisos, con fecha, valor anterior y valor nuevo.

**Why this priority**: el registro se genera desde el inicio (requisito transversal), pero la
pantalla de consulta puede llegar después de la operación básica.

**Independent Test**: se cambia el teléfono de un usuario y se verifica que la auditoría muestra
quién hizo el cambio, cuándo, el teléfono anterior y el nuevo.

**Acceptance Scenarios**:

1. **Given** un cambio sobre un usuario, **When** el auditor consulta el historial de ese usuario,
   **Then** ve la fecha, el autor, la acción, el valor anterior y el valor nuevo de cada campo
   modificado.
2. **Given** un auditor de un negocio, **When** consulta la auditoría, **Then** solo ve registros
   de su propio negocio. El Super Administrador ve los de todos.
3. **Given** un cambio de contraseña, **When** se consulta la auditoría, **Then** aparece el
   evento, pero nunca el valor de la contraseña.

---

### User Story 7 - Super Administrador ajusta las plantillas de la plataforma (Priority: P3)

El Super Administrador edita los roles sugeridos que reciben los negocios nuevos, los textos de
los correos de invitación, recuperación y segundo factor, y los nombres y el estado de los
permisos del catálogo global.

**Why this priority**: los valores iniciales cubren la operación; editarlos es un ajuste que
puede llegar al final del módulo.

**Independent Test**: se cambia el asunto del correo de invitación y se agrega un rol sugerido.
Se comprueba que la siguiente invitación usa el asunto nuevo y que el siguiente negocio creado
recibe el rol nuevo, sin afectar a los negocios existentes.

**Acceptance Scenarios**:

1. **Given** el Super Administrador en la plataforma, **When** cambia el texto del correo de
   recuperación, **Then** los siguientes correos de recuperación usan el texto nuevo.
2. **Given** un negocio ya existente, **When** el Super Administrador modifica la plantilla de
   roles sugeridos, **Then** los roles de ese negocio no cambian, y solo los negocios creados
   después reciben la plantilla nueva.
3. **Given** el catálogo de permisos, **When** el Super Administrador cambia el nombre visible
   de un permiso o desactiva uno, **Then** la matriz de todos los negocios muestra el nombre
   nuevo, y el permiso desactivado deja de aparecer y de sumar en la siguiente acción.

---

### Edge Cases

- **Correo en varios negocios**: el mismo correo puede existir en dos negocios distintos, porque
  solo es único dentro de cada negocio. Cada cuenta tiene su propia contraseña. Al iniciar
  sesión, si el correo y la contraseña coinciden con cuentas activas de varios negocios activos,
  la persona elige el negocio. Si coinciden con una sola, entra directamente.
- **Bloqueo con correo en varios negocios**: los intentos fallidos suman al mismo contador
  aunque la persona quisiera entrar a negocios distintos. Al bloquearse, quedan bloqueadas todas
  sus cuentas con ese correo hasta que termina el tiempo de bloqueo.
- **Recuperación con correo en varios negocios**: el correo de recuperación indica a qué negocio
  pertenece cada enlace, y cada enlace cambia solo la contraseña de esa cuenta.
- **Desactivación con sesión abierta**: si se desactiva un usuario, un rol o un negocio mientras
  alguien tiene sesión abierta, el cambio se aplica en la siguiente acción, no al vencer la sesión.
- **Usuario sin roles activos**: si todos los roles de un usuario quedan inactivos, puede entrar
  pero solo ve su perfil y el cambio de contraseña.
- **Recuperación con correo inexistente**: se muestra el mismo mensaje de confirmación que con un
  correo válido, para no revelar qué cuentas existen.
- **Enlace de recuperación inválido**: un enlace vencido o ya usado muestra un aviso y ofrece
  solicitar otro.
- **Invitación vencida o no recibida**: el usuario sigue "pendiente" y ve un aviso que le pide
  contactar a su administrador, quien reenvía la invitación.
- **Edición simultánea**: si dos administradores editan el mismo rol o usuario a la vez, el
  segundo en guardar recibe un aviso de conflicto en lugar de sobrescribir los cambios del primero.
- **Escalada de privilegios**: si un usuario intenta asignar un rol con permisos que no tiene,
  cambiar sus propios roles o marcar un permiso que no posee, el sistema lo rechaza y registra
  el intento en la auditoría.
- **Roles sugeridos editados**: si el negocio renombra o elimina sus roles sugeridos, no se
  vuelven a crear ni afectan a otros negocios.
- **Logo inválido**: un archivo que no sea PNG ni JPEG (aunque tenga esa extensión), o que pese
  más de 2 MB, se rechaza con un mensaje claro.
- **Último Super Administrador**: no se puede desactivar el último Super Administrador activo de
  la plataforma.

## Requirements *(mandatory)*

### Functional Requirements

**Negocios**

- **FR-001**: El Super Administrador MUST poder crear, editar, listar (con paginación y búsqueda
  por nombre o NIT), activar y desactivar negocios.
- **FR-002**: Cada negocio MUST registrar: nombre, logo, color principal y color secundario, NIT,
  dirección, teléfono, correo de contacto, horario de atención, texto de agradecimiento, texto de
  condiciones y días de resguardo (entero mayor o igual a cero).
- **FR-003**: El NIT MUST ser único en la plataforma. Nombre, NIT y correo de contacto son
  obligatorios.
- **FR-004**: Al crear un negocio, el sistema MUST cargarle los roles sugeridos Administrador,
  Recepción, Técnico y Bodega, con permisos iniciales sugeridos, como roles propios y editables
  del negocio.
- **FR-005**: Mientras un negocio esté inactivo, ninguno de sus usuarios MUST poder iniciar
  sesión ni continuar usando una sesión abierta.

**Usuarios**

- **FR-006**: Los usuarios con permiso MUST poder crear, editar, listar (con paginación y
  búsqueda), activar y desactivar usuarios de su propio negocio, con nombre, correo, teléfono y
  estado.
- **FR-007**: El correo MUST ser único dentro de cada negocio, sin distinguir mayúsculas de
  minúsculas.
- **FR-008**: Cada usuario de negocio MUST tener al menos un rol, y puede tener varios.
- **FR-008a**: Un usuario solo MUST poder asignar a otros usuarios roles cuyos permisos estén
  todos incluidos en sus propios permisos efectivos. Ningún usuario MUST poder agregar ni quitar
  sus propios roles. El Super Administrador no tiene esta restricción.
- **FR-009**: Al crear un usuario, el sistema MUST enviarle un correo con un enlace de invitación
  para definir su contraseña. Nadie más conoce ni define su contraseña.
  - El enlace es de un solo uso y vence a las 72 horas (configurable).
  - Hasta que el usuario define su contraseña, aparece como "pendiente" y no puede iniciar
    sesión.
  - El administrador MUST poder reenviar la invitación. Al reenviarla, el enlace anterior deja de
    servir.
- **FR-010**: El sistema MUST impedir que un negocio quede sin al menos un usuario activo con
  permiso para administrar usuarios y roles.

**Autenticación**

- **FR-011**: Los usuarios MUST poder iniciar sesión con correo y contraseña, y cerrar sesión.
  Si las credenciales coinciden con cuentas activas de varios negocios activos, el usuario MUST
  elegir el negocio antes de entrar. Si coinciden con una sola cuenta, entra directamente.
- **FR-012**: Los usuarios MUST poder cambiar su contraseña ingresando la actual. Al hacerlo, se
  cierran sus demás sesiones.
- **FR-013**: Los usuarios MUST poder recuperar su contraseña mediante un enlace enviado por
  correo, de un solo uso y que vence a los 60 minutos.
- **FR-014**: Los intentos fallidos MUST contarse por correo en toda la plataforma, no por
  cuenta.
  - Un intento es fallido cuando la contraseña no coincide con ninguna cuenta activa de ese
    correo.
  - Tras un número de intentos fallidos consecutivos (por defecto 5), el sistema MUST bloquear
    todas las cuentas con ese correo durante un tiempo configurable (por defecto 15 minutos).
  - Un inicio de sesión exitoso reinicia el contador.
  - Los mensajes de error de inicio de sesión MUST ser genéricos.
- **FR-015**: Las contraseñas MUST tener al menos 8 caracteres e incluir letras y números, y
  nunca se guardan ni se muestran en forma legible.
- **FR-016**: Las sesiones MUST cerrarse tras un período de inactividad configurable (por
  defecto 30 minutos).

**Roles y permisos**

- **FR-017**: Los usuarios con permiso MUST poder crear, editar, listar, activar y desactivar
  roles de su negocio, con nombre (único dentro del negocio) y descripción.
- **FR-018**: El sistema MUST NOT tener roles fijos para los negocios. Ninguna regla del sistema
  depende del nombre de un rol, solo de sus permisos.
- **FR-019**: MUST existir un catálogo global de permisos con la forma Módulo.Acción, definido
  por la plataforma y común a todos los negocios. Los negocios no lo modifican.
- **FR-019a**: Un rol MUST poder tener la bandera "acceso total".
  - Solo el Super Administrador MUST poder marcar o desmarcar la bandera.
  - Un rol con la bandera tiene todos los permisos del catálogo, incluidos los que se agreguen
    después, sin que nadie tenga que asignarlos.
  - Su matriz se muestra completamente marcada y no se puede editar.
  - El rol sugerido Administrador se crea con la bandera.
- **FR-020**: La asignación de permisos a un rol MUST hacerse en una matriz de módulos (filas)
  por acciones (columnas), marcando o desmarcando casillas. Solo se muestran las combinaciones
  que existen en el catálogo.
- **FR-020a**: Cada módulo del catálogo MUST tener identificada su acción de consulta (Ver).
  - Al marcar cualquier otra acción de un módulo, la matriz MUST marcar también su acción de
    consulta.
  - Al desmarcar la acción de consulta, MUST desmarcar todas las acciones de ese módulo.
  - El sistema MUST rechazar guardar un rol que tenga acciones de un módulo sin su acción de
    consulta.
- **FR-020b**: Un usuario solo MUST poder marcar en la matriz los permisos que él mismo tiene.
  Las casillas de los demás permisos se muestran deshabilitadas. Tampoco MUST poder editar los
  permisos de un rol que él tenga asignado. El Super Administrador no tiene esta restricción.
- **FR-021**: Los permisos efectivos de un usuario MUST ser la unión de los permisos de sus roles
  activos.
- **FR-022**: Los cambios en roles, permisos o asignaciones MUST aplicarse en la siguiente acción
  de los usuarios afectados, sin que tengan que volver a iniciar sesión.

**Control de acceso y aislamiento**

- **FR-023**: Cada pantalla y cada acción MUST exigir su permiso Módulo.Acción. Un acceso sin
  permiso MUST rechazarse y registrarse.
- **FR-024**: Los menús MUST mostrar solo las opciones para las que el usuario tiene permiso.
- **FR-025**: Un usuario de negocio NUNCA MUST poder ver ni modificar datos de otro negocio. Un
  intento de hacerlo MUST tratarse como dato inexistente.
- **FR-026**: El Super Administrador MUST ser una cuenta de plataforma que no pertenece a ningún
  negocio, que puede ver y administrar todos los negocios y crear el usuario administrador
  inicial de cada uno.
- **FR-026a**: El Super Administrador MUST poder entrar a cualquier negocio, activo o inactivo,
  con acceso completo para dar soporte: usuarios, roles y, en módulos futuros, toda la
  información operativa. Mientras está dentro de un negocio:
  - trabaja solo sobre ese negocio;
  - ve en todo momento un indicador visible de que está actuando como soporte en ese negocio;
  - puede salir y volver a la vista de plataforma.
- **FR-026b**: La entrada y la salida del Super Administrador de un negocio MUST registrarse en
  la auditoría. Cada acción que realice dentro del negocio MUST registrarse con su identidad de
  Super Administrador, nunca a nombre de un usuario del negocio. Esos registros MUST ser
  visibles para los auditores del propio negocio.
- **FR-027**: Las cuentas de Super Administrador MUST poder ser creadas y desactivadas solo por
  otro Super Administrador. La primera se crea al instalar la plataforma.
- **FR-027a**: Después de la contraseña, el Super Administrador MUST ingresar un código de un
  solo uso enviado a su correo en cada inicio de sesión. El código vence a los 10 minutos
  (configurable). Los códigos incorrectos cuentan como intentos fallidos para el bloqueo.

**Auditoría**

- **FR-028**: El sistema MUST registrar la creación, modificación, activación y desactivación de
  negocios, usuarios, roles y asignaciones de permisos. Cada registro incluye quién, cuándo, la
  acción, el valor anterior y el valor nuevo de cada campo cambiado.
- **FR-029**: El sistema MUST registrar también los eventos de seguridad: inicios de sesión
  exitosos y fallidos, bloqueos, cambios y recuperaciones de contraseña, y accesos denegados.
  Nunca se registran contraseñas ni enlaces de recuperación.
- **FR-030**: La auditoría MUST poder consultarse con filtros por fecha, usuario autor, tipo de
  registro y acción, por usuarios con permiso, dentro de su propio negocio. El Super
  Administrador puede consultarla en todos los negocios.
- **FR-031**: Los registros de auditoría MUST NOT poder modificarse ni eliminarse desde el sistema.

**Generales**

- **FR-032**: Ningún negocio, usuario ni rol se elimina físicamente: solo se desactiva.
- **FR-033**: La interfaz y todos los mensajes MUST estar en español.
- **FR-034**: Los valores configurables de este módulo (intentos antes del bloqueo, duración del
  bloqueo, vencimiento del enlace de recuperación, vencimiento del enlace de invitación,
  vencimiento del código del segundo factor, inactividad de sesión, roles y permisos sugeridos)
  MUST administrarse como configuración almacenada, no como valores fijos.
- **FR-035**: Los valores de seguridad (intentos y duración del bloqueo, vencimiento de los
  enlaces de recuperación e invitación y del código del segundo factor, inactividad de sesión)
  MUST ser globales para toda la plataforma y ajustables solo por el Super Administrador, desde
  una pantalla de configuración de plataforma. Cada cambio queda en la auditoría.
- **FR-036**: El Super Administrador MUST poder editar, desde la plataforma, la plantilla de
  roles sugeridos (nombres, descripciones, permisos y bandera "acceso total") y las plantillas de
  los correos de invitación, recuperación y segundo factor (asunto y texto).
  - Los cambios a la plantilla de roles solo afectan a los negocios que se creen después.
  - Cada cambio queda en la auditoría.
  - Hasta que exista esta pantalla, rigen los valores iniciales.
- **FR-037**: El logo MUST ser una imagen PNG o JPEG de hasta 2 MB. Se guarda en su formato
  original, sin conversión. El sistema verifica el tipo real del archivo, no solo su extensión.
- **FR-038**: El Super Administrador MUST poder editar el catálogo global de permisos.
  - Puede cambiar el nombre, la descripción y el orden de módulos, acciones y permisos.
  - Puede activar o desactivar módulos y permisos. Lo inactivo no aparece en la matriz ni suma
    permisos efectivos, ni siquiera con "acceso total".
  - Los códigos `Módulo.Acción` no se pueden cambiar.
  - Los permisos nuevos solo los agrega cada módulo al construirse.
  - No se puede desactivar el permiso de consulta (Ver) de un módulo con otras acciones activas,
    ni un permiso necesario para administrar usuarios y roles (FR-010).
  - Cada cambio queda en la auditoría.

### Key Entities *(include if feature involves data)*

- **Negocio**: un taller que usa la plataforma. Atributos: nombre, NIT, dirección, teléfono, correo
  de contacto, horario, logo, colores, texto de agradecimiento, texto de condiciones, días de
  resguardo y estado. Todo dato operativo de los módulos siguientes pertenece a un negocio.
- **Usuario**: persona que trabaja en un negocio. Atributos: nombre, correo (único en el negocio),
  teléfono y estado (pendiente, activo o inactivo). Pertenece a un negocio y tiene uno o varios roles.
- **Control de intentos por correo**: contador de intentos fallidos y bloqueo vigente para cada
  correo, común a todas las cuentas con ese correo en cualquier negocio.
- **Super Administrador**: cuenta de plataforma, sin negocio, que administra todos los negocios.
- **Rol**: agrupación de permisos con nombre y descripción, propia de un negocio. Puede estar
  activo o inactivo, y puede tener la bandera "acceso total" (FR-019a).
- **Permiso**: elemento del catálogo global, identificado por Módulo y Acción
  (por ejemplo, Ordenes.Crear). Se asigna a roles.
- **Asignación de rol / Asignación de permiso**: relación usuario↔rol y rol↔permiso.
- **Registro de auditoría**: evento con autor, fecha y hora, negocio, tipo de registro afectado,
  acción, y valores anterior y nuevo.
- **Configuración de seguridad**: valores globales de la plataforma (no pertenecen a ningún
  negocio) para el bloqueo, la recuperación, el segundo factor y la sesión.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El Super Administrador deja un negocio nuevo listo, con su administrador inicial
  invitado, en menos de 5 minutos.
- **SC-002**: En las pruebas de aislamiento, el 100% de los intentos de un usuario de leer o
  modificar datos de otro negocio son rechazados.
- **SC-003**: El 100% de las pantallas y acciones rechazan a usuarios sin el permiso
  correspondiente, y ningún menú muestra opciones sin permiso.
- **SC-004**: Un administrador crea un rol nuevo con sus permisos en menos de 3 minutos, sin
  ayuda, en su primer intento.
- **SC-005**: Un usuario recupera el acceso a su cuenta mediante el correo de recuperación en
  menos de 5 minutos.
- **SC-006**: Los cambios de permisos, roles o estados se reflejan en la siguiente acción de los
  usuarios afectados, sin que vuelvan a iniciar sesión.
- **SC-007**: El 100% de los cambios sobre negocios, usuarios, roles y permisos aparecen en la
  auditoría con autor, fecha, valor anterior y valor nuevo.
- **SC-008**: Una cuenta que recibe ataques de adivinación de contraseña queda bloqueada tras los
  intentos configurados en el 100% de los casos.

## Assumptions

- Al inicio hay un solo negocio, pero el sistema funciona igual con varios desde el primer día.
- Los permisos iniciales del catálogo cubren los módulos Usuarios, Roles y Auditoría. La
  gestión de negocios, la configuración y las plantillas son exclusivas del Super Administrador
  y no forman parte del catálogo. Cada módulo futuro agrega sus propios permisos al catálogo
  cuando se construye.
- Los roles sugeridos son Administrador, con la bandera "acceso total", y Recepción, Técnico y
  Bodega **sin permisos**. Los módulos futuros les agregan permisos en sus datos iniciales.
  Mientras tanto, un usuario con solo esos roles entra y ve únicamente su perfil.
- El envío de correos se hace mediante un servicio de correo disponible para la plataforma.
- Los registros de auditoría se conservan indefinidamente en esta versión.
- Los colores del negocio son un color principal y uno secundario. El logo sigue FR-037.
- "Días de resguardo" es el número de días que el taller guarda un equipo sin reclamar. En este
  módulo solo se registra; su uso pertenece al módulo de órdenes.
- La verificación en dos pasos es solo para el Super Administrador (FR-027a). Los usuarios de
  negocio no la tienen en esta versión.
- Fuera de alcance: portal del cliente final, cobro de mensualidades a negocios e inicio de
  sesión con redes sociales.
