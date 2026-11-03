# Prueba rápida · PWA + Notificaciones

## 1. Compilar

Desde la carpeta raíz:

```powershell
dotnet restore
dotnet build
```

## 2. Ejecutar

```powershell
dotnet run --project NinosParaCristo.API
```

## 3. Abrir

Chrome:

`http://localhost:5021`

## 4. Iniciar sesión

Usuario inicial de desarrollo:

- `admin@ninosparacristo.local`
- `Admin123!`

## 5. Probar Push

- Pulsa **Activar notificaciones**.
- Permite las notificaciones en Chrome.
- El sistema enviará una prueba automáticamente.
- También puedes usar **Probar notificación**.

Si no aparece, revisa que Chrome no tenga bloqueadas las notificaciones del sitio.

## 6. Instalar PWA en escritorio

Usa el botón **Instalar app** o el icono de instalación que muestra Chrome en la barra de direcciones.

## 7. iPhone

No uses la IP local `http://192.168...` para probar Push en iPhone. Para iOS necesitamos publicar primero la aplicación con HTTPS. Una vez publicada:

Safari → Compartir → Agregar a pantalla de inicio → abrir app → iniciar sesión → Activar notificaciones.
