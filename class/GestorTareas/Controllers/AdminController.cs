using GestorTareas.Data;
using GestorTareas.Models;
using GestorTareas.Services;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace GestorTareas.Controllers;
[Authorize(Roles="Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly PagoManualQrService _manual;
    public AdminController(AppDbContext db, IWebHostEnvironment environment, PagoManualQrService manual)
    { _db=db; _environment=environment; _manual=manual; }

    // Los formularios HTML pueden enviar 7.50 o 7,50 dependiendo del navegador.
    // No aceptamos separadores de miles: 7.50 significa 7 bolivianos y 50 centavos.
    private static bool TryPrecioBs(string? texto, out decimal precio)
    {
        precio = 0m;
        if (string.IsNullOrWhiteSpace(texto)) return false;
        var limpio = texto.Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(limpio, @"^[0-9]{1,4}([.,][0-9]{1,2})?$"))
            return false;
        return decimal.TryParse(limpio.Replace(',', '.'),
                   System.Globalization.NumberStyles.AllowDecimalPoint,
                   System.Globalization.CultureInfo.InvariantCulture, out precio)
               && precio >= 0m && precio <= 1000m;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.QrManual=_manual.Datos;
        ViewBag.TotalReservas=await _db.Reservas.CountAsync();
        ViewBag.TotalComentarios=await _db.Comentarios.CountAsync();
        ViewBag.TotalLikes=await _db.MeGusta.CountAsync();
        ViewBag.Libros = await _db.Libros.OrderBy(l=>l.Titulo).ToListAsync();
        ViewBag.Canciones = await _db.Canciones.OrderBy(c=>c.Titulo).ToListAsync();
        ViewBag.Usuarios = await _db.Users.AsNoTracking().OrderBy(u=>u.Email).ToListAsync();
        return View(await _db.Alquileres.AsNoTracking().Include(a=>a.Usuario).Include(a=>a.Libro).Include(a=>a.Pago)
            .OrderByDescending(a=>a.CreadoEn).Take(150).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(1_200_000)]
    public async Task<IActionResult> ConfigurarQrBancario(IFormFile? archivo, string titular,
        string banco, bool autorizacionConfirmada)
    {
        if (!autorizacionConfirmada || archivo is null)
        { TempData["Error"]="Debes usar un QR auténtico con permiso del titular y marcar la autorización.";
          return RedirectToAction(nameof(Index)); }
        // Evitar cambiar el beneficiario cuando hay cobros esperando pago o revisión.
        if (_manual.Disponible && await _db.Alquileres.AnyAsync(a=>a.Estado=="PendientePago" &&
            a.Pago!=null && a.Pago.Proveedor=="BancoManual" &&
            (a.Pago.Estado=="Pendiente" || a.Pago.Estado=="EnRevision")))
        { TempData["Error"]="No puedes cambiar el QR mientras existan pagos bancarios pendientes o en revisión.";
          return RedirectToAction(nameof(Index)); }
        try
        {
            await _manual.ConfigurarAsync(archivo,titular,banco);
            TempData["Mensaje"]="QR bancario configurado. Los pagos requieren verificación manual.";
        }
        catch (ArgumentException ex) { TempData["Error"]=ex.Message; }
        catch (IOException) { TempData["Error"]="No se pudo guardar el QR; comprueba los permisos de la carpeta."; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmarPagoManual(int id,string? referenciaBancaria,
        bool confirmoIngreso)
    {
        var refBanco=referenciaBancaria?.Trim();
        if (!confirmoIngreso || string.IsNullOrWhiteSpace(refBanco) || refBanco.Length is < 4 or > 120)
        { TempData["Error"]="Confirma el ingreso REAL en el banco e introduce la referencia bancaria (4–120 caracteres).";
          return RedirectToAction(nameof(Index)); }
        await using var tx=await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var alquiler=await _db.Alquileres.Include(a=>a.Pago)
            .FirstOrDefaultAsync(a=>a.Id==id);
        if (alquiler?.Pago is null || alquiler.Pago.Proveedor!="BancoManual" ||
            alquiler.Estado!="PendientePago" || alquiler.Pago.Estado!="EnRevision")
        { TempData["Error"]="Esta solicitud no está pendiente de revisión manual.";
          return RedirectToAction(nameof(Index)); }
        // Impide reutilizar una referencia confirmada en otro alquiler.
        if (await _db.Pagos.AnyAsync(p=>p.Id!=alquiler.Pago.Id && p.Proveedor=="BancoManual" &&
            p.Estado=="Pagado" && p.TransaccionExterna==refBanco))
        { TempData["Error"]="Esta referencia bancaria ya está asociada a otro pago aprobado.";
          return RedirectToAction(nameof(Index)); }
        var hoy=DateTime.UtcNow;
        alquiler.Pago.TransaccionExterna=refBanco;
        alquiler.Pago.Estado="Pagado";
        alquiler.Pago.VerificadoEn=hoy;
        alquiler.Estado="Activo";
        alquiler.InicioEn=hoy;
        alquiler.VenceEn=hoy.AddDays(TarifasAlquiler.Valida(alquiler.DuracionDias) ? alquiler.DuracionDias : 7);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        TempData["Mensaje"]=$"Pago verificado en el banco. Se habilitó la lectura por {alquiler.DuracionDias} días.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RechazarPagoManual(int id)
    {
        var alquiler=await _db.Alquileres.Include(a=>a.Pago).FirstOrDefaultAsync(a=>a.Id==id);
        if (alquiler?.Pago is null || alquiler.Pago.Proveedor!="BancoManual" ||
            alquiler.Estado!="PendientePago" || alquiler.Pago.Estado!="EnRevision") return BadRequest();
        alquiler.Pago.Estado="Rechazado";
        alquiler.Estado="Cancelado";
        await _db.SaveChangesAsync();
        TempData["Mensaje"]="Solicitud rechazada. Esta acción no realiza devoluciones bancarias.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearLibro([Bind("Titulo,Autor,Genero,Descripcion,PrecioRenta,RutaLectura,Derechos")] LibroRenta libro)
    {
        if (!string.IsNullOrWhiteSpace(libro.RutaLectura))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(libro.RutaLectura,@"^[a-zA-Z0-9_-]+\.txt$") ||
                !System.IO.File.Exists(Path.Combine(_environment.ContentRootPath,"PrivateBooks",libro.RutaLectura)) ||
                string.IsNullOrWhiteSpace(libro.Derechos))
                ModelState.AddModelError(nameof(libro.RutaLectura),"Se requiere un .txt autorizado en PrivateBooks y una descripción de derechos.");
        }
        if (!ModelState.IsValid) { TempData["Error"]="Revisa título, autor, derechos, género y precio del libro. Solo admite archivos .txt autorizados."; return RedirectToAction(nameof(Index)); }
        libro.Activo=true;
        (libro.AutorId,libro.CategoriaId)=await CatalogoHelper.LibroClavesAsync(_db,libro.Autor,libro.Genero);
        _db.Libros.Add(libro); await _db.SaveChangesAsync();
        TempData["Mensaje"]="Libro creado.";
        return RedirectToAction(nameof(Index));
    }


    // Carga local de PDF o enlace legal. El PDF queda fuera de wwwroot y se entrega
    // a través de LibrosController.ArchivoPdf, que verifica acceso en cada petición.
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> CrearLibroDigital(string titulo, string autor, string genero,
        string? descripcion, string precioRenta, string? urlLectura, string? derechos,
        bool derechosConfirmados, IFormFile? pdf)
    {
        if (!TryPrecioBs(precioRenta, out var precioBase))
        { TempData["Error"]="Precio inválido: escribe 7,50 o 7.50 para Bs 7,50 (máximo Bs 1000).";
          return RedirectToAction(nameof(Index)); }
        if (!derechosConfirmados || string.IsNullOrWhiteSpace(derechos) || derechos.Length>100)
            { TempData["Error"]="Debes acreditar los derechos de la edición que publicarás."; return RedirectToAction(nameof(Index)); }
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Length>120 ||
            string.IsNullOrWhiteSpace(autor) || autor.Length>100 ||
            string.IsNullOrWhiteSpace(genero) || genero.Length>50 ||
            descripcion?.Length>600 || precioBase<0 || precioBase>1000)
            { TempData["Error"]="Revisa los campos del libro y el precio."; return RedirectToAction(nameof(Index)); }
        if ((pdf is null || pdf.Length==0) == string.IsNullOrWhiteSpace(urlLectura))
            { TempData["Error"]="Elige una opción: PDF o URL HTTPS de lectura, pero no ambas."; return RedirectToAction(nameof(Index)); }
        if (!string.IsNullOrWhiteSpace(urlLectura) &&
            (precioBase>0 || !Uri.TryCreate(urlLectura,UriKind.Absolute,out var url) || url.Scheme != Uri.UriSchemeHttps ||
             url.IsLoopback || url.UserInfo.Length>0 || urlLectura.Length>2048))
            { TempData["Error"]="La URL debe ser HTTPS pública y solo se acepta para libros gratis. Para cobrar, sube un PDF autorizado."; return RedirectToAction(nameof(Index)); }
        if (pdf is not null && (pdf.Length < 5 || pdf.Length > 25_000_000 ||
            !string.Equals(Path.GetExtension(pdf.FileName),".pdf",StringComparison.OrdinalIgnoreCase)))
            { TempData["Error"]="Solo se aceptan PDF de hasta 25 MB."; return RedirectToAction(nameof(Index)); }

        string? archivoGuardado = null;
        try
        {
            if(pdf is {Length: > 0})
            {
                await using var stream=pdf.OpenReadStream();
                var head=new byte[5];
                await stream.ReadExactlyAsync(head);
                if(System.Text.Encoding.ASCII.GetString(head)!="%PDF-")
                    {TempData["Error"]="El archivo no tiene una cabecera PDF válida.";return RedirectToAction(nameof(Index));}
                var carpeta=Path.Combine(_environment.ContentRootPath,"PrivateBooks");
                Directory.CreateDirectory(carpeta);
                archivoGuardado=Guid.NewGuid().ToString("N")+".pdf";
                await using var salida=new FileStream(Path.Combine(carpeta,archivoGuardado),FileMode.CreateNew,FileAccess.Write);
                await salida.WriteAsync(head);
                await stream.CopyToAsync(salida);
            }
            // Todas las altas del catálogo quedan atómicas (autor, categoría, libro y URL).
            await using var transaction=await _db.Database.BeginTransactionAsync();
            var claves=await CatalogoHelper.LibroClavesAsync(_db,autor.Trim(),genero.Trim());
            var libro=new LibroRenta {
                Titulo=titulo.Trim(),Autor=autor.Trim(),Genero=genero.Trim(),
                Descripcion=descripcion?.Trim()??"",PrecioRenta=precioBase,Estado="Disponible",
                AutorId=claves.AutorId,CategoriaId=claves.CategoriaId,
                Derechos=derechos.Trim(),ArchivoPdf=archivoGuardado,UrlLectura=urlLectura?.Trim(),Activo=true
            };
            _db.Libros.Add(libro);
            await _db.SaveChangesAsync();
            if(archivoGuardado is not null)
            {
                libro.UrlLectura=$"/Libros/ArchivoPdf/{libro.Id}";
                await _db.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            TempData["Mensaje"]=$"Libro '{libro.Titulo}' publicado ({(precioBase==0?"gratis":"alquiler")} ).";
        }
        catch(Exception)
        {
            if(archivoGuardado is not null)
            {
                var ruta=Path.Combine(_environment.ContentRootPath,"PrivateBooks",archivoGuardado);
                if(System.IO.File.Exists(ruta))System.IO.File.Delete(ruta);
            }
            TempData["Error"]="No se pudo crear el libro. Comprueba los datos, los permisos y el SQL 03.";
        }
        return RedirectToAction(nameof(Index));
    }
    // Edición completa del libro existente, con PDF opcional y sin perder historiales de alquiler.
    [HttpGet]
    public async Task<IActionResult> EditarLibro(int id)
    {
        var libro=await _db.Libros.AsNoTracking().FirstOrDefaultAsync(l=>l.Id==id);
        if(libro is null) return NotFound();
        ViewBag.QrManual=_manual.Datos; // QR de cobro compartido por los libros; el monto depende del plazo.
        return View(libro);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(30_000_000)]
    public async Task<IActionResult> EditarLibro(int id, string titulo, string autor, string genero,
        string? descripcion, string precioRenta, string? derechos, bool derechosConfirmados,
        string? urlLectura, bool quitarPdf, IFormFile? pdf)
    {
        if (!TryPrecioBs(precioRenta, out var precioBase))
        { TempData["Error"]="Precio inválido: escribe 7,50 o 7.50 para Bs 7,50 (máximo Bs 1000).";
          return RedirectToAction(nameof(EditarLibro), new { id }); }
        var libro=await _db.Libros.FirstOrDefaultAsync(l=>l.Id==id);
        if(libro is null) return NotFound();
        if (!derechosConfirmados || string.IsNullOrWhiteSpace(derechos) || derechos.Length>100 ||
            string.IsNullOrWhiteSpace(titulo) || titulo.Length>120 ||
            string.IsNullOrWhiteSpace(autor) || autor.Length>100 ||
            string.IsNullOrWhiteSpace(genero) || genero.Length>50 ||
            descripcion?.Length>600 || precioBase<0 || precioBase>1000)
        { TempData["Error"]="Revisa los campos, el precio base y los derechos del libro.";
          return RedirectToAction(nameof(EditarLibro),new{id}); }
        var hayPdfNuevo=pdf is {Length: > 0};
        if (hayPdfNuevo && (pdf!.Length<5 || pdf.Length>25_000_000 ||
            !string.Equals(Path.GetExtension(pdf.FileName),".pdf",StringComparison.OrdinalIgnoreCase)))
        { TempData["Error"]="El PDF debe tener entre 5 bytes y 25 MB.";
          return RedirectToAction(nameof(EditarLibro),new{id}); }
        // No permitir una URL externa para alquiler con fecha de expiración.
        if (!string.IsNullOrWhiteSpace(urlLectura) &&
            (!Uri.TryCreate(urlLectura,UriKind.Absolute,out var enlace) || enlace.Scheme!="https" ||
             enlace.IsLoopback || !string.IsNullOrEmpty(enlace.UserInfo) || urlLectura.Length>2048 || precioBase>0))
        { TempData["Error"]="Los enlaces externos solo se permiten en libros gratuitos. Para alquilar, carga un PDF autorizado.";
          return RedirectToAction(nameof(EditarLibro),new{id}); }
        if (hayPdfNuevo && !string.IsNullOrWhiteSpace(urlLectura))
        { TempData["Error"]="Elige PDF o URL, pero no ambos.";
          return RedirectToAction(nameof(EditarLibro),new{id}); }
        if(precioBase>0 && !hayPdfNuevo && (quitarPdf || string.IsNullOrWhiteSpace(libro.ArchivoPdf))
           && string.IsNullOrWhiteSpace(libro.RutaLectura))
        { TempData["Error"]="El libro de pago necesita un PDF interno o un texto autorizado para controlar el vencimiento.";
          return RedirectToAction(nameof(EditarLibro),new{id}); }

        string? guardado=null;
        try
        {
            if(hayPdfNuevo)
            {
                await using var stream=pdf!.OpenReadStream();
                var head=new byte[5]; await stream.ReadExactlyAsync(head);
                if(System.Text.Encoding.ASCII.GetString(head)!="%PDF-")
                { TempData["Error"]="Cabecera de PDF inválida.";return RedirectToAction(nameof(EditarLibro),new{id}); }
                var dir=Path.Combine(_environment.ContentRootPath,"PrivateBooks");
                Directory.CreateDirectory(dir);
                guardado=Guid.NewGuid().ToString("N")+".pdf";
                await using var output=new FileStream(Path.Combine(dir,guardado),FileMode.CreateNew,FileAccess.Write);
                await output.WriteAsync(head); await stream.CopyToAsync(output);
            }
            await using var tx=await _db.Database.BeginTransactionAsync();
            var keys=await CatalogoHelper.LibroClavesAsync(_db,autor.Trim(),genero.Trim());
            libro.Titulo=titulo.Trim();libro.Autor=autor.Trim();libro.Genero=genero.Trim();
            libro.Descripcion=descripcion?.Trim()??"";libro.PrecioRenta=precioBase;
            libro.Derechos=derechos.Trim();libro.AutorId=keys.AutorId;libro.CategoriaId=keys.CategoriaId;
            if(guardado is not null)
            {
                libro.ArchivoPdf=guardado;
                libro.UrlLectura=$"/Libros/ArchivoPdf/{libro.Id}";
                libro.RutaLectura=null;
            }
            else if(quitarPdf || !string.IsNullOrWhiteSpace(urlLectura))
            {
                libro.ArchivoPdf=null;
                libro.UrlLectura=string.IsNullOrWhiteSpace(urlLectura)?null:urlLectura.Trim();
                if(!string.IsNullOrWhiteSpace(urlLectura)) libro.RutaLectura=null;
            }
            await _db.SaveChangesAsync();await tx.CommitAsync();
            TempData["Mensaje"]=$"Libro '{libro.Titulo}' actualizado. Los pagos ya solicitados conservan su importe.";
        }
        catch(Exception)
        {
            if(guardado is not null)
            {
                var p=Path.Combine(_environment.ContentRootPath,"PrivateBooks",guardado);
                if(System.IO.File.Exists(p))System.IO.File.Delete(p);
            }
            TempData["Error"]="No se pudo guardar la edición. Revisa el archivo PDF y la base de datos.";
        }
        return RedirectToAction(nameof(EditarLibro),new{id});
    }

    // Publicar canción original desde el panel, sin que el admin tenga que copiar archivos a mano.
    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(21_000_000)]
    public async Task<IActionResult> SubirCancion(string titulo,string artista,string genero,
        bool derechosConfirmados,IFormFile? audio)
    {
        if(!derechosConfirmados || string.IsNullOrWhiteSpace(titulo) || titulo.Length>110 ||
           string.IsNullOrWhiteSpace(artista) || artista.Length>100 ||
           string.IsNullOrWhiteSpace(genero) || genero.Length>50 ||
           audio is null || audio.Length<12 || audio.Length>20_000_000)
        { TempData["Error"]="Completa título, artista, género y selecciona un audio propio/autorizado (máximo 20 MB).";
          return RedirectToAction(nameof(Index)); }
        var ext=Path.GetExtension(audio.FileName).ToLowerInvariant();
        if(ext is not (".wav" or ".mp3"))
        { TempData["Error"]="Solo se permiten archivos WAV o MP3.";return RedirectToAction(nameof(Index)); }
        string? file=null;
        try
        {
            await using var input=audio.OpenReadStream();
            var head=new byte[12];await input.ReadExactlyAsync(head);
            bool valid=ext==".wav"
                ? System.Text.Encoding.ASCII.GetString(head,0,4)=="RIFF" &&
                  System.Text.Encoding.ASCII.GetString(head,8,4)=="WAVE"
                : System.Text.Encoding.ASCII.GetString(head,0,3)=="ID3" ||
                  (head[0]==0xFF && (head[1]&0xE0)==0xE0);
            if(!valid){TempData["Error"]="El contenido no parece ser un archivo WAV/MP3 válido.";
                return RedirectToAction(nameof(Index));}
            var folder=Path.Combine(_environment.ContentRootPath,"PrivateAudio");
            Directory.CreateDirectory(folder);
            file=Guid.NewGuid().ToString("N")+ext;
            await using(var output=new FileStream(Path.Combine(folder,file),FileMode.CreateNew,FileAccess.Write))
            { await output.WriteAsync(head);await input.CopyToAsync(output); }
            await using var tx=await _db.Database.BeginTransactionAsync();
            var c=new Cancion { Titulo=titulo.Trim(),Artista=artista.Trim(),Genero=genero.Trim(),
                AudioUrl=file,Fuente="Local",Licencia="Original / publicación autorizada",Activa=true };
            c.GeneroId=await CatalogoHelper.GeneroIdAsync(_db,c.Genero);
            _db.Canciones.Add(c);await _db.SaveChangesAsync();await tx.CommitAsync();
            TempData["Mensaje"]="¡Canción publicada! Ya se puede escuchar y añadir a playlists.";
        }
        catch(Exception)
        {
            if(file is not null)
            {
                var p=Path.Combine(_environment.ContentRootPath,"PrivateAudio",file);
                if(System.IO.File.Exists(p))System.IO.File.Delete(p);
            }
            TempData["Error"]="No se pudo subir el audio. Comprueba el formato y espacio disponible.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarLibro(int id)
    {
        var l=await _db.Libros.FindAsync(id);
        if (l==null) return NotFound();
        l.Activo=!l.Activo; await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCancion([Bind("Titulo,Artista,Genero,AudioUrl")] Cancion cancion)
    {
        // Solo .wav propios/autorizados dentro de PrivateAudio (no accesible como archivo estático).
        if (!string.IsNullOrWhiteSpace(cancion.AudioUrl))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(cancion.AudioUrl, @"^[a-zA-Z0-9_-]+\.wav$") ||
                !System.IO.File.Exists(Path.Combine(_environment.ContentRootPath, "PrivateAudio", cancion.AudioUrl)))
                ModelState.AddModelError(nameof(Cancion.AudioUrl), "Archivo inexistente o nombre inválido en PrivateAudio.");
        }
        if (!ModelState.IsValid) { TempData["Error"]="Datos de canción no válidos; guarda primero el audio autorizado en PrivateAudio."; return RedirectToAction(nameof(Index)); }
        cancion.Activa=true;
        cancion.GeneroId=await CatalogoHelper.GeneroIdAsync(_db,cancion.Genero);
        _db.Canciones.Add(cancion); await _db.SaveChangesAsync();
        TempData["Mensaje"]="Canción agregada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarCancion(int id)
    {
        var c=await _db.Canciones.FindAsync(id);
        if (c==null) return NotFound();
        c.Activa=!c.Activa; await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }


    // WizPay: solo webhook autenticado. Banco manual: aprobación Admin tras revisar el abono real.
}
