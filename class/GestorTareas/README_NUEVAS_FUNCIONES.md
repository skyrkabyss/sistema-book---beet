# BiblioBeat 2 — música en línea, libros libres y pago QR

## Qué se implementó

- **Buscar música en Internet:** dos catálogos. Wikimedia Commons permite reproducir audios con licencias abiertas y muestra origen y licencia. iTunes/Apple permite encontrar canciones comerciales y abrir la ficha en el servicio oficial (no distribuye audios comerciales dentro del sitio). Es una búsqueda global en Internet, NO una integración con Spotify.
- **Playlists reales en MySQL:** crear, agregar canciones locales, música libre o enlaces a canciones comerciales; reproducir solo las pistas con audio autorizado. Las playlists y canciones importadas quedan en la base de datos.
- **Leer libros gratis:** sección `Libros > Leer gratis` conectada a Gutendex / Project Gutenberg. Busca clásicos en español o inglés. El lector muestra el texto en la web cuando Gutenberg ofrece texto plano compatible. No exige cuenta ni pago. El copyright puede variar según país/edición.
- **Alquileres de libros autorizados:** libro de prueba original `Relatos de otoño — edición BiblioBeat` con texto privado. La lectura pagada se habilita solo al recibir una confirmación autenticada de WizPay; dura 7 días. Los títulos famosos precargados que no tengan un archivo autorizado se muestran como fichas y NO se cobran ni se pueden leer desde esta app.
- **QR comercial real preparado:** integración con [WizPay](https://wizpay.app/docs) para generar código QR bancario y recibir webhooks. Sin afiliación, credenciales y un HTTPS público, los cobros quedan desactivados. Nunca se simula una confirmación de pago desde el administrador.

## Actualizar tu proyecto que ya corre

1. **Respalda tu proyecto y tu base de datos** antes de actualizar. Desde phpMyAdmin: selecciona `bibliobeat_otono` > Exportar > SQL, guárdalo en otro lugar. Conserva tu `appsettings.json` anterior, donde pusiste la contraseña local.
2. Cierra el programa anterior (`Ctrl+C` en la terminal). Descomprime este ZIP en otra carpeta. La nueva app está en `BDS/class/GestorTareas`.
3. En `BDS/class/GestorTareas/appsettings.json` coloca la **misma contraseña MySQL** que ya funciona en tu computadora. No publiques este archivo con la contraseña.
4. **No necesitas borrar ni recrear MySQL.** Al arrancar, BiblioBeat 2 amplía las columnas de `Canciones`, `Libros` y `Pagos` que falten en la base existente, sin borrar las filas previas (SchemaUpgrade). Un registro de prueba del libro original se añade una sola vez.
5. Abre una terminal PowerShell en la carpeta extraída (donde está `BDS`) y ejecuta:

   ```powershell
   dotnet restore .\BDS\class\GestorTareas\GestorTareas.csproj
   dotnet run --project ".\BDS\class\GestorTareas\GestorTareas.csproj" --launch-profile http
   ```

6. Abre la dirección que muestre `Now listening on:`. Revisa `/Musica`, `/Musica/Playlists`, `/Libros/Gratis`, `/Libros` y `/Admin`.
7. Para búsqueda en línea, el equipo debe tener conexión a Internet y poder acceder a `commons.wikimedia.org`, `itunes.apple.com`, `gutendex.com` y `gutenberg.org`. Los catálogos externos pueden tener límites o indisponibilidad temporal.

## Cobrar de verdad con QR boliviano

El proyecto incluye una **integración de código**, no una cuenta bancaria/comercio ni credenciales válidas. Se necesita:

- Afiliación aprobada de comercio con el proveedor; completar requisitos de identidad, capacidad legal y titularidad de cuenta (si es necesario, con un representante adulto autorizado). No eludir límites de edad ni requisitos legales.
- Clave API legítima del entorno SANDBOX/PRODUCCION, una clave secreta para webhook configurada también en el panel, y una URL pública HTTPS que apunte a `https://TU-DOMINIO/webhooks/wizpay`.
- Pruebas end-to-end en SANDBOX antes de activar PRODUCCION. El entorno de pruebas de WizPay utiliza `https://api-test.wizpay.app` y producción `https://api.wizpay.app`.

**Configura secretos solamente en el servidor, nunca en el cliente ni en GitHub.** En PowerShell (valores ficticios para sustituir con credenciales otorgadas por el proveedor):

```powershell
$env:WizPay__Enabled="true"
$env:WizPay__Mode="SANDBOX"
$env:WizPay__ApiKey="CLAVE_OTORGADA_POR_WIZPAY"
$env:WizPay__WebhookSecret="SECRETO_DE_WEBHOOK_COINCIDENTE_CON_EL_PANEL"
# dotnet run --project ... --launch-profile http
```

Para producción, después de certificación, usar `WizPay__Mode=PRODUCCION` y una clave *de producción*. **Una página en `localhost` no puede recibir webhooks públicos**, por lo que sin desplegarla en un servidor HTTPS no se confirmarán ni desbloquearán pagos reales. El usuario ve QR solo si la pasarela está configurada. Un QR de SANDBOX no significa que se haya recibido dinero.

La interfaz protege las operaciones con antiforgery, mantiene los secretos fuera del HTML y valida el Bearer, importe, moneda, referencia, ID QR y pedido al recibir la confirmación de pago. El sistema no recopila datos de tarjeta. No actives el comercio antes de verificar las condiciones fiscales y las licencias del contenido.

## Derechos de contenido

- Los resultados comerciales de Apple son **metadatos y enlaces a Apple Music**; no se descarga, almacena ni reproduce la canción comercial en esta aplicación. Para reproducir música comercial con licencia dentro de la web, contrata un servicio con permisos adecuados.
- Wikimedia Commons contiene archivos con distintas licencias libres. Esta búsqueda filtra licencias abiertas y presenta el enlace a la ficha original, pero debes comprobar la atribución y el permiso comercial de cada archivo antes de monetizar una distribución.
- Project Gutenberg recopila libros libres según condiciones estadounidenses; verifica el dominio público en Bolivia y de las traducciones cuando corresponda. No cobres por el acceso a estos libros gratuitos.
- Libros famosos del catálogo original sin textos con licencia de distribución **no están disponibles para cobro ni lectura**. Para ofrecerlos, debes contratar los derechos y añadir una edición autorizada.

## Comprobación rápida

1. Busca `Queen` en Música: deben mostrarse resultados del catálogo de Apple con enlace oficial (dependiendo de red). No se reproducen dentro de BiblioBeat.
2. Busca `piano` en Música: deberían aparecer algunos audios libres de Commons, si la API devuelve resultados aptos.
3. Crea una playlist con tu cuenta e importa un resultado; reinicia la app y comprueba que sigue en MySQL.
4. En Libros > Leer gratis busca `Quijote`; abre un texto disponible en el lector.
5. Sin credenciales comerciales el libro original mostrará el QR desactivado; con la clave de Sandbox, debe generar QR de prueba y esperar webhook autorizado. Nunca pongas a usuarios reales a pagar antes de completar pruebas y publicación segura.

## Fuentes de API y documentación

- Wikimedia Commons API: https://commons.wikimedia.org/wiki/Commons:API/MediaWiki
- Apple Search API: https://performance-partners.apple.com/search-api
- Gutendex: https://github.com/garethbjohnson/gutendex
- Project Gutenberg: https://www.gutenberg.org/
- WizPay: https://wizpay.app/docs/flujos/qr y https://wizpay.app/docs/webhooks

**Estado:** cambios implementados en código; la integración no ha sido compilada ni probada contra tu instalación Windows/MySQL o credenciales reales desde este entorno. Comprueba los errores en VS Code y corrígelos antes de publicar o cobrar.
