# Documentación técnica resumida

## Arquitectura

- Cliente: HTML/CSS/JavaScript responsive + PWA.
- Backend: ASP.NET Core 10 Web API.
- Persistencia: Entity Framework Core + Npgsql.
- Base de datos: PostgreSQL.
- Autenticación: JWT.
- Tiempo real: SignalR.
- Organización: Domain, Application, Infrastructure y API.

## Calendarios

### GrupoClase
Catálogo editable que define los rangos de edad. No se codifican las edades en la interfaz, por lo que la institución puede modificarlas sin cambiar el programa.

Campos principales: Id, Nombre, EdadMinima, EdadMaxima, Orden, Activo.

### AsignacionClaseMensual
Representa la estructura observada en el calendario manual: una maestra asignada a un grupo durante un mes y año. Un grupo puede tener varias maestras en el mismo mes porque cada maestra se almacena en una fila independiente.

Campos principales: Anio, Mes, GrupoClaseId, MaestraId, Tema, MaterialUrl, Observaciones, Activo.

### ProtocoloSemana
Representa cada domingo y permite asignar por separado a la responsable de merienda y a la responsable de protocolo. Incluye merienda, tareas, bitácora y estado de completado.

## Control de acceso basado en calendario

Cuando una maestra de clase consulta `ninos/aula` o pasa asistencia, el servidor obtiene sus asignaciones del mes y calcula la edad de cada niño en la fecha consultada. Solo devuelve o acepta registros que correspondan a los rangos de edad de sus grupos.

Esto evita depender únicamente de filtros visuales y aplica el principio de mínimo acceso a datos de menores.

## Notificaciones

El servicio de recordatorios revisa asignaciones para siete días después. Cuando la fecha objetivo es domingo, genera una notificación para cada maestra asignada al grupo de ese mes. También genera recordatorios de merienda y protocolo.

## Tiempo real

Los controladores notifican cambios mediante `UpdatesHub`. Los clientes conectados actualizan la pantalla actual al recibir el evento.

## Compatibilidad con etapas previas del TCU

Las migraciones iniciales se conservan. En ejecución, `DatabaseBootstrapper` utiliza instrucciones `CREATE TABLE IF NOT EXISTS` para asegurar el esquema completo y permitir abrir bases creadas durante etapas anteriores del proyecto sin perder los registros existentes.

## Seguridad

- Contraseñas almacenadas como hash.
- JWT con expiración.
- Autorización por roles en controladores.
- Maestra de protocolo sin acceso a niños.
- Maestra de clase con datos mínimos y filtrado por grupo.
- Administradora con acceso completo.
- HTTPS previsto para producción.
- Respaldos de PostgreSQL incluidos en `scripts/`.

## Producción

Para publicar en Internet se recomienda alojar API/PWA y PostgreSQL en servicios administrados, activar HTTPS, reemplazar la clave JWT, cambiar el usuario administrador inicial y configurar copias de seguridad automáticas. Para notificaciones push con la app cerrada puede añadirse Firebase Cloud Messaging como siguiente etapa.

## PWA instalada y Web Push

### Manifest y Service Worker

`wwwroot/manifest.webmanifest` define el nombre, colores, iconos, `start_url`, `scope` y modo `standalone`. En iPhone se incluye además `apple-touch-icon` y metadatos de web app.

`wwwroot/sw.js` tiene tres responsabilidades:

1. Mantener en caché el shell visual básico.
2. Recibir eventos `push` y mostrar una notificación del sistema.
3. Gestionar `notificationclick` para abrir/focalizar la aplicación y dirigir a la sección de notificaciones.

Las peticiones `/api/` y `/hubs/` nunca se sirven desde caché para evitar mostrar datos sensibles desactualizados.

### Suscripción de dispositivos

El navegador genera una `PushSubscription` después de que la usuaria concede permiso. El cliente envía al backend `endpoint`, `p256dh` y `auth`; el backend los asocia al usuario autenticado en `PushSuscripciones`.

La clave privada VAPID nunca se envía al navegador. El cliente recibe únicamente la clave pública mediante `GET /api/push/public-key`.

### Endpoints

- `GET /api/push/public-key`: obtiene la clave pública VAPID.
- `POST /api/push/subscribe`: registra o renueva el dispositivo del usuario autenticado.
- `POST /api/push/unsubscribe`: desactiva la suscripción del dispositivo.
- `POST /api/push/test`: envía una notificación de prueba al usuario autenticado.

### Entrega automática

`ReminderService` crea recordatorios internos con `Enviada = false`. `ReminderBackgroundService` ejecuta periódicamente dos pasos:

1. Generar los recordatorios que correspondan a siete días vista.
2. Intentar entregar las notificaciones pendientes mediante `WebPushNotificationService`.

Una notificación se marca como enviada cuando al menos un dispositivo activo del usuario la recibe. Si no hay ningún dispositivo suscrito, permanece pendiente y se puede entregar después de que la maestra active las notificaciones.

### Seguridad

Las suscripciones Push requieren un JWT válido para asociarse a un usuario. El `endpoint` y las claves de cifrado de la suscripción no se exponen a otros usuarios. Las claves privadas VAPID deben configurarse mediante secretos/variables de entorno en producción.
