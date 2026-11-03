# Niños para Cristo · Sistema de Gestión

Sistema web responsive/PWA para el Ministerio de Niños **Niños para Cristo** de la Comunidad Cristiana Pozos de Agua Viva.

Esta versión integra el flujo real de calendarios que actualmente se distribuye de forma manual: **división de clases por grupo/edad/mes**, calendario dominical de **protocolo**, asignación de **meriendas** y consulta personalizada por maestra.

## Qué incluye

- Autenticación con JWT.
- Roles: Administradora, Maestra de clase y Maestra de protocolo.
- Registro de niños, alergias, comentarios y encargado principal.
- Grupos de clase configurables por rangos de edad.
- Grupos iniciales: Cuna (1-3), Párvulos (4-5), Primarios (6-8), Intermedios y Adolescentes (9-14).
- Calendario de clases trimestral por mes, grupo y una o varias maestras.
- Material/enlace y tema opcional por asignación mensual.
- Calendario de protocolo por domingo.
- Generación automática de todos los domingos de un trimestre.
- Responsable de merienda y responsable de protocolo independientes.
- Bitácora de protocolo.
- Pase de asistencia filtrado para que una maestra vea únicamente los niños de los grupos que tiene asignados ese mes.
- Inventario de materiales.
- Reportes de asistencia, cumpleaños, alergias y ausencias.
- Registro de comunicaciones con padres/encargados.
- Notificaciones internas automáticas una semana antes.
- SignalR para que los cambios aparezcan en los dispositivos conectados sin tener que cerrar sesión.
- Diseño responsive para PC, tablet y celular.
- PWA instalable desde navegadores compatibles.
- Vista imprimible de calendarios para conservar una versión física o guardar como PDF.
- Scripts de respaldo/restauración de PostgreSQL.

## Requisitos

- .NET 10 SDK
- PostgreSQL 18 (también funciona con versiones recientes compatibles)
- pgAdmin 4, opcional pero recomendado
- Navegador moderno

## 1. Base de datos

Crea una base en PostgreSQL llamada:

`NinosParaCristo`

No es necesario crear las tablas manualmente. Al iniciar, el sistema garantiza el esquema requerido, incluso si vienes de la primera versión del TCU que solo tenía la tabla `Ninos`.

## 2. Cadena de conexión

Abre:

`NinosParaCristo.API/appsettings.json`

Cambia `CAMBIA_ESTA_CONTRASENA` por la contraseña local del usuario `postgres`.

No publiques una contraseña real en GitHub.

## 3. Iniciar

Desde la carpeta raíz:

```powershell
dotnet restore
dotnet build
dotnet run --project NinosParaCristo.API
```

O usa `INICIAR.ps1`.

Luego abre la dirección indicada por .NET; normalmente será:

`http://localhost:5021`

## Usuario inicial

- Correo: `admin@ninosparacristo.local`
- Contraseña: `Admin123!`

Cambia la contraseña y crea las cuentas reales antes de cargar datos reales de menores.

## Cómo cargar el calendario de clases

1. Entra como Administradora.
2. Crea las usuarias con rol **Maestra de clase**.
3. Abre **Calendarios de clases**.
4. Elige año y trimestre.
5. Los cuatro grupos iniciales ya aparecen; pueden editarse si la organización cambia sus edades.
6. Agrega una asignación por cada maestra. Si Cuna tiene dos maestras en julio, registra dos asignaciones para Cuna/julio.
7. Opcionalmente agrega tema, material y observaciones.
8. Usa **Imprimir / guardar PDF** para obtener una vista similar al calendario que se utilizaba en papel.

## Cómo cargar protocolo y merienda

1. Crea las usuarias con rol **Maestra de protocolo**.
2. Abre **Meriendas y protocolo**.
3. Selecciona año y trimestre.
4. Pulsa **Preparar domingos** para generar todas las fechas dominicales del trimestre.
5. Edita cada domingo y asigna responsable de merienda, responsable de protocolo, merienda y tareas.
6. Las maestras verán únicamente sus propias asignaciones.

## Protección de datos

- La administradora tiene acceso a la ficha completa del niño.
- La maestra de clase no recibe comentarios administrativos ni datos del encargado.
- La maestra de clase ve únicamente nombre, alergias y los niños cuyos rangos de edad corresponden a los grupos que tiene asignados ese mes.
- La maestra de protocolo no tiene acceso a datos personales de los niños.
- Los permisos se validan en el backend, no solo ocultando botones.

## Calendario manual usado como referencia

La versión actual del ministerio organiza la división de clases por **mes + grupo + rango de edad + maestras**, y el protocolo por **fecha dominical + persona responsable**. El sistema digitaliza esa estructura en datos editables y consultables; no depende de una imagen o diapositiva para funcionar.

## Nota sobre la app móvil

Esta entrega utiliza una **PWA responsive**, por lo que funciona desde navegador en Android, iPhone, tablet y computadora y puede instalarse como acceso directo. El backend quedó separado para que, si la universidad exige un cliente Flutter nativo, pueda agregarse después sin rehacer la base de datos ni la API.

# PWA y notificaciones Web Push

Esta versión agrega instalación real como PWA y notificaciones Web Push estándar.

## Qué cambia para las maestras

- Pueden instalar **Niños para Cristo** desde el navegador y abrirlo desde un ícono en la pantalla de inicio.
- En iPhone/iPad se muestran instrucciones específicas para Safari → Compartir → Agregar a pantalla de inicio.
- Después de iniciar sesión pueden pulsar **Activar notificaciones**.
- Cada dispositivo queda asociado a la cuenta de la maestra mediante una suscripción Web Push.
- El sistema puede enviar recordatorios de clase, merienda y protocolo aunque la aplicación no esté abierta.
- Si una suscripción expira, el backend la desactiva automáticamente.

## Prueba local rápida en Chrome

En desarrollo, `localhost` se considera un contexto seguro para Service Workers y Push API.

1. Configura la contraseña de PostgreSQL en `NinosParaCristo.API/appsettings.json`.
2. Ejecuta:

```powershell
dotnet restore
dotnet build
dotnet run --project NinosParaCristo.API
```

3. Abre `http://localhost:5021`.
4. Inicia sesión.
5. En el menú lateral pulsa **🔔 Activar notificaciones**.
6. Chrome pedirá permiso. Selecciona **Permitir**.
7. El sistema registra el dispositivo y envía automáticamente una notificación de prueba.
8. También aparecerá el botón **🧪 Probar notificación**.

## iPhone / iPad

Para Web Push en iOS/iPadOS, la versión publicada debe usar **HTTPS**. El flujo será:

1. Abrir la URL publicada en Safari.
2. Compartir → **Agregar a pantalla de inicio**.
3. Abrir la app desde el ícono `Niños para Cristo`.
4. Iniciar sesión.
5. Pulsar **Activar notificaciones**.
6. Aceptar el permiso del sistema.

No se necesita publicar la aplicación en App Store para este flujo.

## Claves VAPID

En desarrollo se incluye un par VAPID dentro de `appsettings.Development.json` para que puedas probar inmediatamente.

**Antes de publicar o subir el código a un repositorio público**, genera un par nuevo y configura las claves como variables de entorno del hosting. No cambies las claves después de que las usuarias se hayan suscrito, salvo que vayas a pedirles que activen de nuevo las notificaciones.

Variables de configuración esperadas en producción:

```text
WebPush__Subject
WebPush__PublicKey
WebPush__PrivateKey
```

## Nueva tabla

`PushSuscripciones` almacena únicamente la información técnica necesaria para entregar notificaciones a cada dispositivo:

- UsuarioId
- Endpoint
- P256dh
- Auth
- UserAgent
- Activa
- FechaRegistro
- FechaActualizacion

No contiene contraseñas ni datos de los niños.

## Alojamiento previsto

Para el TCU se puede usar un esquema sin costo inicial:

- ASP.NET Core + PWA: servicio web gratuito con HTTPS.
- PostgreSQL: plan gratuito de un proveedor PostgreSQL compatible.
- Web Push: protocolo estándar sin costo por notificación.

La configuración local no depende del proveedor final, por lo que el hosting se puede cambiar después sin rehacer la aplicación.
