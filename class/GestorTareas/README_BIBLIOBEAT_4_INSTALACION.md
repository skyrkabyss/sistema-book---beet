# BiblioBeat 4.0 — Actualización para C#, MySQL, lectura PDF, música YouTube y estadísticas

Esta versión se basa en **BiblioBeat 3.0**, usando la base **bibliobeat_diagrama** de phpMyAdmin. NO sobrescribe tablas ni elimina cuentas. Incluye los nuevos archivos y código listo para probar; **no fue compilado aquí con .NET 9 ni conectado al MySQL de tu computadora**, así que comienza con `dotnet build`.

## 1. Antes de empezar

1. En XAMPP arranca MySQL (y Apache, para entrar en phpMyAdmin).
2. En phpMyAdmin selecciona `bibliobeat_diagrama` y **Exportar → SQL** como respaldo.
3. Conserva las contraseñas del `appsettings.json` que ya funcionaba en tu PC. La versión nueva trae `CAMBIA_ESTA_CLAVE` como marcador, NO la contraseña anterior.
4. Si vienes de 3.0, las migraciones `01_ADAPTAR_BIBLIOBEAT.sql` y `02_DAR_PERMISOS.sql` ya deberían estar aplicadas. **No borres ni recrees la base.**
5. Si aún faltan las columnas `usuarios.PasswordHash`, ejecuta primero `BDS/class/GestorTareas/database/01_ADAPTAR_BIBLIOBEAT.sql` con la base `bibliobeat_diagrama` seleccionada.

## 2. NUEVO: actualizar SQL

En phpMyAdmin, conectado como administrador (`root`) y con la base `bibliobeat_diagrama` seleccionada, importa **solo**:

`BDS/class/GestorTareas/database/03_BIBLIOBEAT_4_LIBROS.sql`

Añade estas columnas a `libros` sin eliminar datos:

- `url_lectura` VARCHAR(2048) NULL: enlace HTTP**S** o ruta local del lector.
- `archivo_pdf` VARCHAR(160) NULL: nombre aleatorio de archivo privado, nunca un nombre suministrado por el navegador.

Comprobar:

```sql
USE bibliobeat_diagrama;
SHOW COLUMNS FROM libros LIKE 'url_lectura';
SHOW COLUMNS FROM libros LIKE 'archivo_pdf';
```

## 3. Configuración de VS Code en Windows

Abre en VS Code la carpeta `BDS/class/GestorTareas` de BiblioBeat 4.0. Edita **solo** `ConnectionStrings:DefaultConnection` de `appsettings.json` con los datos y contraseña MySQL que funcionaban en BiblioBeat 3.0. No publiques ese archivo con credenciales reales.

Desde la terminal abierta en `GestorTareas`:

```powershell
dotnet --version
dotnet restore
dotnet build
dotnet run --launch-profile http
```

También puedes abrir la carpeta raíz descomprimida y hacer doble clic en `INICIAR_BIBLIOBEAT_4.bat`. Primero activa MySQL en XAMPP.

Abre la URL exacta indicada por `Now listening on:`; para desarrollo suele ser `http://localhost:5137`.

## 4. Libros PDF y URL

- Con una cuenta **Admin**, entra en `/Admin`.
- En **Nuevo libro · PDF o URL** introduce título, autor, género y precio.
- Selecciona **PDF** propio/con permiso (máx. 25 MB) O introduce una URL HTTPS de una edición legal. No uses los dos simultáneamente.
- Completa el titular/licencia y confirma la autorización para publicar esa edición; guarda.
- Si el precio es `0`, la lectura se ofrece gratis. Si es mayor, exige alquiler vigente y pago **realmente** confirmado por el proveedor; el panel nunca puede confirmar cobros manualmente.
- El PDF se guarda en `PrivateBooks/` fuera de `wwwroot`. La aplicación crea en MySQL la URL `/Libros/ArchivoPdf/{id}` y revisa el acceso en cada petición.
- Los enlaces externos se abren en la web del proveedor y pueden restringir el acceso o el uso de iframes. La URL y el PDF se mantienen identificados en columnas distintas.
- En el catálogo inicial aparece **Relatos de otoño — PDF gratuito**, una edición de demostración original, para comprobar el lector.

**Sobre los tres PDF proporcionados**: se añadieron sus **fichas bibliográficas**: *Noches blancas*, *Las penas del joven Werther* y *Flipped*. Sus archivos no se incluyen en este ZIP ni se activan para lectura o alquiler por defecto. La edición Elejandría y las traducciones tienen derechos propios; *Flipped* es una obra moderna. Para publicar cualquiera de esas copias, consigue permiso o usa una edición expresamente autorizada. La presencia de un PDF en tu equipo no demuestra permiso para distribución o cobro.

## 5. Búsqueda legal en YouTube y playlists

Usa **YouTube Data API v3** oficial y el iframe oficial de YouTube (sin descargar MP3/MP4 ni omitir anuncios). Configuración:

1. Crea un proyecto en Google Cloud y habilita **YouTube Data API v3** para ese proyecto, de acuerdo con sus términos.
2. Genera una **API key** para uso en el servidor con restricciones apropiadas. **No la publiques en HTML/JavaScript**.
3. En la misma terminal PowerShell desde la que ejecutarás `dotnet run`, configura temporalmente:

   ```powershell
   $env:YouTube__ApiKey = "TU_API_KEY"
   dotnet run --launch-profile http
   ```

4. Desde `/Musica`, busca un título o artista, pulsa **▶ Ver/escuchar** para usar el reproductor oficial, o **+ Añadir** para guardarlo en una de tus playlists (necesitas iniciar sesión y crear primero una playlist).
5. `/Musica/Playlists` permite reproducir los videos guardados mediante el iframe de YouTube. Algunos videos podrían dejar de poder incrustarse o no estar disponibles en tu región.

Sin una clave, YouTube queda desactivado; Apple Music y Wikimedia Commons siguen disponibles como antes. La cuota de búsqueda de YouTube es limitada. Las pistas guardan el ID oficial y no copian los archivos de audio.

Documentación: https://developers.google.com/youtube/data/reference/rest/v3/search/list y https://developers.google.com/youtube/iframe_api_reference

## 6. Estadística exclusiva para Admin

Al iniciar sesión con rol Admin, abre **Administración → Probabilidad y Estadística** o directamente `/Estadisticas`.

Incluye:
- Indicadores: libros, alquileres activos, usuarios y playlists.
- Frecuencia absoluta/relativa de categorías de libros.
- Estados de alquiler, títulos más solicitados y alquileres de los últimos 14 días.
- Duración media de los alquileres devueltos cuando existe dato suficiente.
- Selector con cuatro distribuciones y sus gráficos de P(X=k):
  - **Binomial:** en 10 usuarios independientes, número con playlist, p estimada por usuarios con playlist / total.
  - **Geométrica:** intento del primer usuario con playlist (mismo p).
  - **Hipergeométrica:** muestra aleatoria de n libros sin reemplazo, cuántos son de la categoría más común.
  - **Poisson:** número de alquileres diarios, lambda estimada como alquileres últimos 30 días / 30.
- Se muestran esperanza matemática y varianza de cada modelo. Cuando faltan datos, el panel avisa si usa un escenario **didáctico** y no presenta esos valores como reales.

Estadística requiere autenticación y rol `Admin` real de ASP.NET Identity. La lista de usuarios ya **no muestra el ID**.

## 7. Comprobación rápida

- Entrar como usuario anónimo y ver `Libros`.
- Leer `Relatos de otoño — PDF gratuito` sin iniciar sesión; abre el PDF en un iframe y en una pestaña nueva.
- Entrar como `Admin` a `/Admin` y verificar que existen campos **URL de lectura** y **Archivo PDF**.
- Entrar a `/Estadisticas`, seleccionar las cuatro distribuciones y confirmar gráficos, tasas y parámetros.
- Buscar música en `/Musica`. Sin API key seguirán Apple/Commons, con API key podrán aparecer videos de YouTube.
- Crear playlist y añadir video autorizado, luego verla desde `/Musica/Playlists`.
- Confirmar que las cuentas con rol `Usuario` no acceden a `/Estadisticas` ni `/Admin`.

## 8. Recomendaciones antes de publicar

El formulario valida extensión, tamaño y cabecera del PDF, y lo guarda fuera de `wwwroot`; en un servicio público agrega análisis antimalware, límites por usuario, control de documentos, copias de seguridad, un gestor de secretos y HTTPS. Cualquier cobro por QR sigue desactivado hasta que un proveedor financiero confirme pagos reales mediante su mecanismo autorizado. El panel no marca pagos como exitosos.
