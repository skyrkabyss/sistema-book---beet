# BiblioBeat 3.0 — Proyecto conectado a tu diagrama de phpMyAdmin

Esta edición **sí consulta y escribe directamente en las 15 tablas del archivo `BiblioBeat_Diagrama_phpMyAdmin.sql`** importado previamente a `bibliobeat_diagrama`. Conserva ASP.NET Core 9, Identity, las vistas otoñales, búsqueda de canciones en Internet, playlists, lectura gratuita y alquiler de la obra original mediante pago QR **solo si la pasarela real está correctamente configurada**.

## Antes de tocar la base: copia de seguridad

En phpMyAdmin, selecciona `bibliobeat_diagrama` > Exportar > Rápido > SQL. También exporta `bibliobeat_otono` si contiene tus antiguos usuarios, libros y listas. No elimines ninguna de las dos bases. Para probar la actualización sin arriesgar datos valiosos, haz primero una copia de la base en MySQL.

## Pasos de instalación (Windows / XAMPP / VS Code)

1. Enciende MySQL y Apache en XAMPP. En phpMyAdmin, con el usuario `root`, comprueba que existan las 15 tablas dentro de la base `bibliobeat_diagrama` (`usuarios`, `libros`, `canciones`, etc.). Si falta alguna, importa *primero* `BiblioBeat_Diagrama_phpMyAdmin.sql` de tu entrega anterior.
2. En phpMyAdmin, selecciona `bibliobeat_diagrama` y abre **Importar**. Importa `database/01_ADAPTAR_BIBLIOBEAT.sql` desde este proyecto (adapta columnas, crea tablas auxiliares necesarias para contraseñas y pagos; no borra tablas originales). Probado estáticamente para MariaDB 10.4+; puede requerir ajustes en MySQL que no soporte `ADD COLUMN IF NOT EXISTS`.
3. Como `root`, importa `database/02_DAR_PERMISOS.sql`. Debes ver en `SHOW GRANTS` el permiso `GRANT ALL PRIVILEGES ON bibliobeat_diagrama.* ...` para `bb_app`.
4. Abre `BDS/class/GestorTareas/appsettings.json` de **esta versión**. En `DefaultConnection` la base ahora es `bibliobeat_diagrama`. Sustituye `CAMBIA_ESTA_CLAVE` por la misma clave real de `bb_app` que ya te funcionaba. **No publiques ni compartas esa contraseña.** Si tu puerto MySQL no es 3306, modifícalo.
5. En VS Code usa **Archivo > Abrir carpeta** y selecciona la carpeta **`BDS`** (no su carpeta superior). Abre **Terminal > Nueva terminal** y escribe:

   ```powershell
   dotnet restore ".\class\GestorTareas\GestorTareas.csproj"
   dotnet build ".\class\GestorTareas\GestorTareas.csproj"
   dotnet run --project ".\class\GestorTareas\GestorTareas.csproj" --launch-profile http
   ```

6. Abre la URL de `Now listening on:` (normalmente `http://localhost:5137`). **Si la terminal muestra un error, no cambies ni borres tu base; copia primero el mensaje exacto.**

## Crear nueva cuenta de administrador

Esta versión cambia de base. Los usuarios existentes en `bibliobeat_otono` **no se copian automáticamente** a `bibliobeat_diagrama`. Si quieres preservar cuentas antiguas, es necesario planificar una migración de usuarios y contraseñas que respete las claves foráneas; nunca copies solo `usuarios` ni inventes contraseñas.

En PowerShell antes del primer inicio de esta nueva base:

```powershell
$env:AdminBootstrap__Email = "admin@bibliobeat.local"
$env:AdminBootstrap__Password = "PON_AQUI_TU_PROPIA_CLAVE_SEGURA"
dotnet run --project ".\class\GestorTareas\GestorTareas.csproj" --launch-profile http
```

Entra en `/Cuenta/Login` y luego `/Admin`. Para borrar variables de la terminal después, detén el programa y ejecuta:

```powershell
Remove-Item Env:AdminBootstrap__Email
Remove-Item Env:AdminBootstrap__Password
```

Las cuentas nuevas se almacenan **directamente en `usuarios`**, con contraseñas hasheadas en `PasswordHash`; los roles se verifican con `AspNetRoles` / `AspNetUserRoles`. El campo `usuarios.rol` se conserva en el diagrama como información descriptiva.

## Relación entre las tablas del diagrama y el proyecto

| Tabla original | Función en la aplicación |
|---|---|
| `usuarios` | Login, registro, correo único, rol descriptivo; columnas adicionales Identity |
| `editoriales`, `autores`, `categorias_libro` | Datos normalizados para cada libro y su autor/categoría |
| `libros` | Catálogo, derechos, edición digital y precio de alquiler |
| `generos_musica`, `canciones` | Géneros y pistas locales/online |
| `playlist`, `playlist_cancion` | Listas de cada usuario, canciones ordenadas |
| `me_gusta` | Favoritos de canciones locales |
| `reservas`, `libros_has_reservas` | Reserva de libros y relación muchos-a-muchos |
| `comentarios`, `valoraciones` | Reseñas y estrellas de libros |
| `alquiler_libros` | Solicitudes de alquiler, período de acceso |

Además se crean las tablas auxiliares **AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims, pagos y Tareas**. No forman parte de las 15 tablas del diagrama original, pero son necesarias para login seguro, roles y pagos de la aplicación.

## Qué probar en phpMyAdmin

```sql
USE bibliobeat_diagrama;
SELECT id, nombre, apellido, correo, rol FROM usuarios;
SELECT id, titulo, autor_id, categoria_id, precio_renta FROM libros;
SELECT id, titulo, genero_id, fuente FROM canciones;
SELECT id, nombre, usuario_id FROM playlist;
SELECT * FROM playlist_cancion;
SELECT * FROM reservas;
SELECT * FROM libros_has_reservas;
SELECT * FROM comentarios;
SELECT * FROM valoraciones;
SELECT * FROM me_gusta;
```

Prueba de usuario: registra una cuenta nueva, crea una playlist, agrega una canción, pulsa «Me gusta», abre un libro, deja un comentario/valoración y haz una reserva. Confirma los registros en esas tablas.

## ¿Por qué agregamos columnas al diagrama?

Tu imagen no incluye datos necesarios para un programa real: hashes de contraseña, datos de un proveedor externo de audio, precio/licencia de una edición digital y confirmaciones de pago. Conservar las claves foráneas de las 15 tablas y **agregar únicamente columnas/tablas de soporte** es más seguro que reemplazar tus tablas o desactivar el login. En `libros` y `canciones`, las columnas `autor` y `genero` son textos auxiliares para la búsqueda y el diseño existente; las relaciones formales siguen siendo `autor_id`, `categoria_id` y `genero_id`.

## Pagos

`WizPay:Enabled` continúa en `false` por seguridad. No es un sistema de cobro listo para producción, no hay QR bancario ficticio y la aplicación **no confirma pagos por sí sola**. Hace falta revisar la documentación vigente del proveedor, aprobar el comercio, configurar sus secretos por variables de entorno y verificar en un entorno HTTPS la autenticidad de sus callbacks. Nunca uses datos bancarios reales para probar un pago no verificado.

## Estado de la entrega

Archivos y esquema revisados mediante comprobaciones **estáticas**, pero este entorno no dispone del SDK .NET ni de un servidor MySQL para ejecutar `dotnet build` o probar llamadas reales. Ejecuta `dotnet build` y las pruebas descritas desde tu PC antes de publicar el sitio o habilitar pagos.
