# Publicar Niños para Cristo y conservar una ruta de crecimiento

Preparado el 3 de noviembre de 2026 sobre el commit 8205b2781b7bcccfc6539d97003401c0d9178680 de APP_MinisterioMaestras. No se han subido estos cambios a GitHub ni creado servicios o cargos.

## Decisión propuesta

Mantener PostgreSQL. Usar Neon para la base y Render para ejecutar la API y la PWA juntas. Empezar con los planes gratuitos permite preparar la misma instalación que después se mantendrá: el paso a un plan de pago no requiere reescribir la aplicación ni mover los datos si se conservan ambos proveedores.

Esto no equivale a garantizar gratuitamente todas las funciones. Render Free duerme tras 15 minutos sin tráfico, tarda en despertar y no ejecuta recordatorios mientras duerme. Tampoco permite SMTP en los puertos usados por este proyecto. No es la configuración recomendada para la entrega operativa completa.

Antes de depender de recordatorios, pasar el servicio web al plan de pago de 512 MB, actualmente US$7 mensuales, manteniendo Neon Free mientras el consumo lo permita. Es un costo base, no una tarifa total garantizada: pueden existir impuestos, excesos de uso y servicios de correo adicionales. Neon Free tiene límites de cómputo, almacenamiento y transferencia; al agotarse algunos límites puede suspender el cómputo. Su recuperación limitada no reemplaza una copia externa.

Los planes gratuitos no son una promesa de gratuidad perpetua. Revisar consumo mensualmente, mantener un responsable institucional y presupuestar la continuidad.

## Archivos preparados

- Dockerfile: empaqueta .NET 10 y sirve la aplicación en el puerto 10000 sin usuario root.
- .dockerignore: excluye compilados, copias de seguridad, configuración de desarrollo y archivos locales del contexto de construcción.
- render.yaml: crea únicamente un servicio web gratuito; no crea una base temporal de Render ni servicios de pago. Despliegue automático desactivado para revisar cada publicación.
- appsettings.Production.json: vacía secretos de ejemplo. Producción exige cadena de conexión, clave JWT y credenciales iniciales propias.
- /health: comprueba que el proceso arrancó después de inicializar la base; no es un monitor continuo de disponibilidad de PostgreSQL.
- .github/workflows/validar.yml: compila, ejecuta las 17 comprobaciones de regresión, verifica JavaScript y construye la imagen en GitHub Actions. No publica la aplicación ni requiere secretos.
- Se incorporan las correcciones iniciales de asistencia, roles y formularios explicadas en la revisión anterior.

## Pasos para la primera publicación

1. Revisar los cambios en una rama del repositorio, incluyendo los archivos nuevos. Si se usa el parche adjunto, ejecutarlo desde la raíz de una copia limpia del commit indicado: `git apply --check CAMBIOS_DESPLIEGUE.patch` y después `git apply CAMBIOS_DESPLIEGUE.patch`. No reemplazar una versión que tenga cambios nuevos sin compararla primero.
2. Crear las cuentas de Neon y Render, preferiblemente con acceso administrado por la institución. No compartir contraseñas por chat. Mantener el plan Free al iniciar.
3. Crear en Neon un proyecto PostgreSQL y una base vacía. Elegir una región cercana a la región de Render. Usar una base separada para pruebas; no conectar este primer despliegue a una base con información real existente.
4. En Neon, abrir Connect y obtener los datos de conexión. Para esta aplicación se requiere formato Npgsql de pares clave/valor, no pegar directamente una URL `postgresql://`. Por ejemplo:

```text
Host=HOST_DE_NEON;Port=5432;Database=neondb;Username=USUARIO;Password="CONTRASENA";SSL Mode=VerifyFull;Timeout=30
```

Usar los valores exactos del panel; las comillas permiten caracteres como punto y coma en la contraseña. Una contraseña que incluya comillas debe escaparse según el formato Npgsql. La conexión directa simplifica el primer arranque que crea el esquema; después se puede evaluar conexión agrupada. No colocar la cadena en GitHub ni en JavaScript del teléfono.

5. En Render, crear un Blueprint desde el repositorio y la rama que contenga estos cambios. El archivo render.yaml pide:

| Variable | Valor |
| --- | --- |
| ConnectionStrings__DefaultConnection | Cadena de Neon en formato Npgsql |
| Jwt__Key | Render genera un valor aleatorio; conservarlo entre despliegues |
| InitialAdmin__Email | Correo de la administradora |
| InitialAdmin__Password | Contraseña propia, al menos 12 caracteres |
| InitialAdmin__Nombre | Nombre visible de la administradora |

6. Revisar que el servicio muestre Free antes de crearlo. El arranque actual crea/ajusta el esquema y la cuenta inicial. No usar también `dotnet ef database update` sin revisar antes la convivencia de migraciones y DatabaseBootstrapper.
7. Abrir el enlace HTTPS asignado por Render. Entrar con las credenciales configuradas, crear cuentas de prueba de cada rol y probar con dos teléfonos usando redes distintas. Reiniciar/desplegar nuevamente y comprobar que los registros siguen en Neon.
8. Probar instalación de la PWA desde el navegador de cada teléfono. La disponibilidad de instalación y Push depende del navegador y del sistema operativo; verificar los dispositivos que realmente usarán las maestras.

Hosting__HttpsAtProxy=true se utiliza aquí porque Render termina TLS y redirige HTTP a HTTPS en su borde. No activar esa opción en un servidor público que no garantice HTTPS. La base permanece accesible solo mediante credenciales del servidor; el teléfono accede a la API, no directamente a PostgreSQL.

## Paso a la operación institucional

Cambiar el plan del MISMO servicio Render a uno de pago. Actualizar también `plan` en render.yaml con el identificador vigente del plan seleccionado para que una sincronización posterior no revierta la configuración a Free. Conservar nombre, URL, cadena de Neon, clave JWT y claves Push. No crear una base nueva para este cambio.

La capacidad inicial debe validarse con uso real. Revisar los límites de Neon y aumentar su plan si el uso o la necesidad de recuperación lo requieren. Si se prefiere PostgreSQL pagado de Render, eso sí requiere una migración de datos planificada; no forma parte del cambio simple del plan web.

Antes de la entrega:

- Configurar nuevas claves Web Push y verificar avisos con la app cerrada. No reutilizar las claves de desarrollo del repositorio.
- Corregir recordatorios tras interrupciones y cambios de responsable; el alojamiento siempre activo no resuelve esos casos por sí solo.
- Configurar SMTP con un proveedor y una cuenta institucional cuando el plan lo permita; realizar una prueba de entrega autorizada. WhatsApp continúa siendo una bitácora manual.
- Establecer respaldos externos con retención acordada y restaurar una copia en una base desechable. Los scripts originales solo apuntan a PostgreSQL local y necesitan adaptación para la base remota; no se consideran respaldo en nube ya resuelto.
- Validar roles, datos ficticios, formularios y concurrencia; corregir los pendientes de calendarios e inventario antes del uso institucional.
- Entregar manual, accesos institucionales, responsable de mantenimiento y procedimiento para actualizar desde GitHub sin perder datos.

## Validación y límites de esta entrega

El código se prepara localmente. No se ha validado la conexión a una cuenta Neon, el Blueprint en Render ni el uso real desde teléfonos. El motor Docker local no estaba en ejecución; la construcción de la imagen debe comprobarse mediante el workflow o un motor Docker disponible. No se presenta este paquete como una aplicación ya publicada o como el cierre completo del proyecto.

## Fuentes oficiales consultadas

- Neon precios y límites: https://neon.com/pricing
- Neon y Npgsql: https://neon.com/docs/guides/dotnet-npgsql
- Render límites gratuitos: https://render.com/docs/free
- Render planes: https://render.com/pricing
- Render servicios de pago sin suspensión por inactividad: https://render.com/docs/faq
- Render HTTPS: https://render.com/docs/tls
- Render Blueprint: https://render.com/docs/blueprint-spec
