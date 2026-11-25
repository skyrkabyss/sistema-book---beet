# Book & Beet 7 — última actualización

## Qué hay de nuevo
- Renombrada toda la interfaz a Book & Beet (la base de datos sigue siendo `bibliobeat_diagrama`, para evitar perder registros).
- Administración > Catálogo de libros > **Editar**: cambia título, autor, género, descripción, precio base, derechos, reemplaza PDF o usa enlace HTTPS solamente en libros gratuitos. Puedes cambiar el QR bancario compartido desde la sección «Configurar o cambiar QR bancario».
- Administración > **Publicar canción propia**: subir MP3 o WAV de máximo 20 MB con permiso; aparece en Música y puede añadirse a playlists. El archivo se almacena en `PrivateAudio` fuera de `wwwroot`.
- En cada ficha de libro de pago, el lector elige **1, 3, 7, 14 o 30 días**. El precio del catálogo corresponde a 7 días y la fórmula es precio = base × días / 7, redondeado a 2 decimales (mínimo Bs 0,01). Ejemplo base Bs 14: 1 día Bs 2; 7 días Bs 14; 30 días Bs 60.
- Los alquileres guardan precio y duración al crearse: futuros cambios del precio no alteran pedidos existentes. El plazo comienza al confirmar el pago, sea en webhook autenticado o verificación bancaria manual por Admin.
- Tras vencer, nuevos accesos a PDF y texto interno se bloquean en servidor. No es posible revocar copias ya obtenidas ni enlaces externos, por eso **el alquiler de pago exige PDF o texto interno autorizado**. Los libros gratuitos pueden usar enlaces externos.
- Se conserva QR bancario real con confirmación manual; la declaración del comprador NO desbloquea automáticamente. El administrador debe verificar en el banco antes de aprobar. No se almacena información bancaria sensible.

## INSTALACIÓN (necesita actualización SQL)
1. Detén el sitio con Ctrl+C y haz respaldo de toda la carpeta, incluido `PrivateBooks`, `PrivateAudio`, `PrivatePayments` y `appsettings.json`. Exporta la base de datos `bibliobeat_diagrama` en phpMyAdmin.
2. **Importante: no se incluye ni reemplaza tu configuración local**. Si aplicas el parche, copia sus archivos encima de la carpeta donde existe `BDS`. No borres las carpetas privadas ni SQL ya existentes. El ZIP de proyecto completo es para instalación nueva o comparación, no para reemplazar los datos locales sin revisar.
3. En phpMyAdmin selecciona `bibliobeat_diagrama`, abre SQL y ejecuta **UNA VEZ** `BDS/class/GestorTareas/database/04_BOOK_BEET_DURACION_ALQUILER.sql`. Crea `alquiler_libros.duracion_dias` con 7 por defecto para alquileres antiguos; no elimina registros.
4. Comprueba que MySQL de XAMPP está encendido. Inicia desde la carpeta que contiene `BDS`:
   `dotnet run --project ".\BDS\class\GestorTareas\GestorTareas.csproj" --launch-profile http`
5. Admin: edita un libro, sube una canción y configura/cambia el QR autorizado. Prueba con un usuario de ensayo: 3 días => importe correcto, declara pago => espera, Admin confirma solo si se acredita, se habilita hasta fecha exacta.

## Nota
Esta versión se ha preparado y comprobado estructuralmente sobre el proyecto existente, pero no se ha podido ejecutar `dotnet build` en este entorno. Requiere compilación y prueba con MySQL en tu Windows. Nunca compartas claves API ni `appsettings.json` con contraseñas.
