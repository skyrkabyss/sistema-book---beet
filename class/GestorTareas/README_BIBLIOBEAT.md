> **VERSIÓN 2.0:** Este documento corresponde a la versión original. Para las nuevas funciones y pago QR, lee primero [`README_NUEVAS_FUNCIONES.md`](README_NUEVAS_FUNCIONES.md). Los pagos manuales de la versión anterior ya no activan alquileres.

# BiblioBeat — Biblioteca y música (prototipo académico)

Este proyecto actualiza el proyecto ASP.NET Core MVC C# original a un sitio con **MySQL** (base administrable desde **phpMyAdmin**) y diseño otoñal (crema, terracota, café, salvia).

## Qué funciona

- Catálogos públicos de libros y canciones con búsqueda.
- Inicio de sesión, registro, cierre de sesión, contraseñas con hash de ASP.NET Core Identity, bloqueo tras varios intentos y cookies HttpOnly.
- Dos roles: `Admin` y `Usuario`. Los visitantes no necesitan registrarse para explorar.
- Los detalles del libro, solicitudes de alquiler, historial, escucha de audios y playlists requieren sesión.
- Solicitudes de alquiler de 7 días y selección de modalidad QR o tarjeta, con estado **Pendiente**; cancelación, devoluciones y confirmación manual por un administrador.
- Creación de playlists personales, añadir/quitar canciones, reproductor HTML5 privado (ruta autenticada `/Musica/Escuchar/{id}`) y cuatro pistas ambientales de **composición generada para este proyecto** (demo). No incluye Spotify ni catálogo comercial; las canciones se sirven solo a usuarios autenticados.
- Panel de administrador: alta/ocultamiento de libros y canciones, listado de usuarios, solicitudes y verificación manual de pagos QR.
- El gestor de tareas antiguo se conserva pero es accesible solo por un administrador.

## Requisitos

- Windows con **.NET SDK 9**, Visual Studio 2022 o VS Code con extensión C#.
- **XAMPP** (Apache opcional; activa MySQL/MariaDB) y **phpMyAdmin**. ASP.NET Core sirve la web; no necesitas alojarla en Apache ni escribir PHP.
- Internet la primera vez para restaurar paquetes NuGet (`Microsoft.AspNetCore.Identity.EntityFrameworkCore` 9.0.2, `Pomelo.EntityFrameworkCore.MySql` 9.0.0).

## Configuración inicial (paso a paso)

1. Activa MySQL en XAMPP, entra a `http://localhost/phpmyadmin`.
2. Abre `database/00_crear_base.sql`, cambia **CAMBIA_ESTA_CLAVE** por una contraseña local propia y ejecuta todo en pestaña SQL de phpMyAdmin (usa un usuario con permiso de crear base/usuarios).
3. En `appsettings.json`, edita **Password=CAMBIA_ESTA_CLAVE** en `ConnectionStrings:DefaultConnection` con **la misma contraseña**. No publiques ese archivo con credenciales reales; para producción usa variables de entorno o secretos de usuario.
4. Para crear tu primer administrador configura las siguientes variables de entorno **antes de ejecutar el sitio** (NO hay una cuenta ni una contraseña de administrador predefinida):

   En PowerShell, desde la carpeta `BDS`:

   ```powershell
   $env:AdminBootstrap__Email = "admin@tudominio.local"
   $env:AdminBootstrap__Password = "PonAquiUnaClaveSegura123!"
   .\ejecutar.ps1
   ```

   Tras crear la cuenta, borra esas variables de tu sesión (`Remove-Item Env:AdminBootstrap__Email; Remove-Item Env:AdminBootstrap__Password`). En otros arranques no hace falta mantenerlas.
5. La primera ejecución creará automáticamente las tablas de una base **nueva y vacía** y algunos datos de demostración. Si ya existía una base de datos con contenido, respáldala: `EnsureCreated` **no migra** esquemas existentes.
6. Ejecuta `BDS/ejecutar.bat` (o en `BDS/class/GestorTareas`: `dotnet restore` y `dotnet run --launch-profile http`). Abre la dirección que muestre la consola, normalmente `http://localhost:5000` o la que indique `Properties/launchSettings.json`.
7. Accede con tu cuenta de administrador. Para probar el rol Usuario, regístrate desde la propia web con otro correo.

## Advertencia esencial: pagos QR y tarjeta

**Este prototipo NO cobra dinero, NO crea un QR bancario válido, NO recibe tarjetas y NO verifica pagos con un proveedor.** La interfaz ofrece las opciones QR y tarjeta y guarda la elección como solicitud pendiente. El administrador puede confirmar manualmente **solo pagos QR** después de revisar la transacción en el banco real. No se debe confirmar un pago que no se haya comprobado. Las solicitudes por tarjeta deben quedar pendientes hasta integrar una pasarela.

Para cobrar realmente en Bolivia, el siguiente paso es contratar un proveedor que acepte QR y Visa/Mastercard, como **Libélula**, configurar credenciales de pruebas, generar órdenes desde el servidor y confirmar pagos mediante la respuesta autenticada del proveedor (webhooks o consulta de estado). Los datos sensibles de tarjetas nunca deben pasar por formularios propios; usar checkout alojado/tokenizado. En producción hacen falta HTTPS obligatorio, política de datos, confirmación de correo, recuperación de contraseña, protección de credenciales y migraciones EF Core para MySQL.

## Notas sobre los datos anteriores

- El ZIP original estaba en C# / ASP.NET Core 9 con **SQLite** y migraciones SQLite. El archivo `tareas.db` y las antiguas migraciones SQLite **no se eliminan de tus originales**. Las migraciones se conservan en `Migrations/` como referencia pero se excluyen de la compilación porque no son compatibles con el nuevo proveedor MySQL.
- **No hay importación automática de los datos SQLite anteriores**. Esta entrega crea una base MySQL nueva y carga ejemplos. Exporta o migra datos previos por separado si necesitas conservarlos.
- Los audios `.wav` guardados en `PrivateAudio/` son demostraciones originales breves; añade solo música con derechos adecuados.

## Rutas principales

| Ruta | Acceso |
|---|---|
| `/` | Público |
| `/Libros` | Público: lista y búsqueda |
| `/Libros/Detalle/1` | Cuenta: detalle y solicitud |
| `/Musica` | Público: catálogo; reproducción privada |
| `/Musica/Playlists` | Cuenta |
| `/Alquileres` | Cuenta |
| `/Cuenta/Login`, `/Cuenta/Registro` | Público |
| `/Admin` | Solo Admin |

## Verificación básica manual

1. Entra como invitado: debes ver inicio, libros y música sin botones de alquiler/reproducción privados.
2. Crea usuario normal: debe poder solicitar libro y crear playlist pero NO acceder a `/Admin`.
3. Solicita libro con QR: debe aparecer como `PendientePago` y bloquear otra solicitud del mismo libro.
4. Verifica QR **tras comprobar un pago auténtico fuera de la app**: debe quedar activo durante 7 días.
5. Devuelve: debe liberar el libro. Crea y edita playlists y comprueba persistencia al reiniciar.

**Validación pendiente en este entorno:** no se pudo ejecutar `dotnet build` ni conectar a MySQL porque las herramientas .NET/MySQL no están instaladas en el entorno de generación. Prueba `dotnet restore; dotnet build` en tu PC y comunica cualquier error de compilación si aparece.
