using GestorTareas.Data;
using GestorTareas.Models;
using GestorTareas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Controllers;
public class LibrosController : Controller
{
    private readonly AppDbContext _db;
    private readonly LibrosOnlineService _libres;
    private readonly UserManager<AppUser> _users;
    private readonly IWebHostEnvironment _env;
    private readonly WizPayService _qr;
    private readonly PagoManualQrService _manual;
    public LibrosController(AppDbContext db,LibrosOnlineService libres,UserManager<AppUser> users,IWebHostEnvironment env,WizPayService qr,PagoManualQrService manual)
        => (_db,_libres,_users,_env,_qr,_manual)=(db,libres,users,env,qr,manual);
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? buscar,string? genero)
    {
        var query=_db.Libros.AsNoTracking().Where(l=>l.Activo);
        if(!string.IsNullOrWhiteSpace(buscar))query=query.Where(l=>l.Titulo.Contains(buscar)||l.Autor.Contains(buscar));
        if(!string.IsNullOrWhiteSpace(genero))query=query.Where(l=>l.Genero==genero);
        ViewBag.Buscar=buscar;ViewBag.Genero=genero;
        ViewBag.Generos=await _db.Libros.Where(l=>l.Activo).Select(l=>l.Genero).Distinct().OrderBy(x=>x).ToListAsync();
        return View(await query.OrderBy(l=>l.Titulo).ToListAsync());
    }
    [AllowAnonymous]
    public async Task<IActionResult> Detalle(int id)
    {
        var libro=await _db.Libros.AsNoTracking().FirstOrDefaultAsync(l=>l.Id==id&&l.Activo);
        if(libro is null)return NotFound();
        ViewBag.Comentarios=await _db.Comentarios.AsNoTracking().Include(x=>x.Usuario)
            .Where(x=>x.LibroId==id).OrderByDescending(x=>x.Fecha).Take(30).ToListAsync();
        ViewBag.Valoraciones=await _db.Valoraciones.AsNoTracking().Where(x=>x.LibroId==id)
            .OrderByDescending(x=>x.Fecha).Take(30).ToListAsync();
        ViewBag.Reservas=await _db.LibrosReservas.AsNoTracking().CountAsync(x=>x.LibroId==id);
        ViewBag.Habilitado=_manual.Disponible || _qr.Disponible;
        ViewBag.Modo=_manual.Disponible ? "MANUAL" : _qr.Modo;
        ViewBag.OpcionesDuracion=TarifasAlquiler.OpcionesDias;
        ViewBag.Acceso=await PuedeLeerAsync(libro);
        return View(libro);
    }
    // Lectura gratuita sin login solo con licencia/permiso documentado en la ficha.
    // Alquileres: con webhook autenticado o aprobación bancaria manual por un Admin.
    private async Task<bool> PuedeLeerAsync(LibroRenta libro)
    {
        if(string.IsNullOrWhiteSpace(libro.Derechos))return false;
        if(libro.PrecioRenta==0)return true;
        if(User.Identity?.IsAuthenticated!=true)return false;
        if(!int.TryParse(_users.GetUserId(User),out var uid))return false;
        return await _db.Alquileres.AsNoTracking().AnyAsync(a=>a.UsuarioId==uid&&a.LibroId==libro.Id&&
            a.Estado=="Activo"&&a.VenceEn>DateTime.UtcNow&&a.Pago!=null&&a.Pago.Estado=="Pagado");
    }
    [AllowAnonymous]
    public async Task<IActionResult> Leer(int id)
    {
        var libro=await _db.Libros.AsNoTracking().FirstOrDefaultAsync(l=>l.Id==id&&l.Activo);
        if(libro is null)return NotFound();
        if(!await PuedeLeerAsync(libro))return User.Identity?.IsAuthenticated==true?Forbid():Challenge();
        Response.Headers.CacheControl="private, no-store";
        if(!string.IsNullOrWhiteSpace(libro.ArchivoPdf))
            return View("LectorPdf",libro);
        // URLs externas solo en títulos gratuitos: su acceso no puede caducar remotamente.
        if(libro.PrecioRenta==0 && !string.IsNullOrWhiteSpace(libro.UrlLectura) &&
            Uri.TryCreate(libro.UrlLectura,UriKind.Absolute,out var enlace) &&
            enlace.Scheme==Uri.UriSchemeHttps && !enlace.IsLoopback && enlace.UserInfo.Length==0)
            return Redirect(enlace.AbsoluteUri);
        if(libro.RutaLectura is not null &&
           System.Text.RegularExpressions.Regex.IsMatch(libro.RutaLectura,@"^[a-zA-Z0-9_-]+\.txt$"))
        {
            var path=Path.Combine(_env.ContentRootPath,"PrivateBooks",libro.RutaLectura);
            if(!System.IO.File.Exists(path))return NotFound();
            return View("Lectura",new LecturaVista{Titulo=libro.Titulo,Autor=libro.Autor,
                Texto=await System.IO.File.ReadAllTextAsync(path),Origen="Edición con lectura autorizada"});
        }
        return NotFound();
    }
    [AllowAnonymous]
    public async Task<IActionResult> ArchivoPdf(int id)
    {
        var libro=await _db.Libros.AsNoTracking().FirstOrDefaultAsync(l=>l.Id==id&&l.Activo);
        if(libro?.ArchivoPdf is null ||
            !System.Text.RegularExpressions.Regex.IsMatch(libro.ArchivoPdf,@"^[a-f0-9]{32}\.pdf$"))return NotFound();
        if(!await PuedeLeerAsync(libro))return User.Identity?.IsAuthenticated==true?Forbid():Challenge();
        var path=Path.Combine(_env.ContentRootPath,"PrivateBooks",libro.ArchivoPdf);
        if(!System.IO.File.Exists(path))return NotFound();
        Response.Headers.CacheControl="private, no-store";
        Response.Headers["X-Content-Type-Options"]="nosniff";
        Response.Headers["Content-Disposition"]="inline";
        var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
        return File(file,"application/pdf",enableRangeProcessing:true);
    }
    [Authorize,HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Comentar(int libroId,string texto)
    {
        if(string.IsNullOrWhiteSpace(texto)||texto.Length>1500) return BadRequest("Escribe un comentario de hasta 1500 caracteres.");
        if(!await _db.Libros.AnyAsync(x=>x.Id==libroId&&x.Activo)) return NotFound();
        _db.Comentarios.Add(new ComentarioLibro{LibroId=libroId,UsuarioId=int.Parse(_users.GetUserId(User)!),Texto=texto.Trim()});
        await _db.SaveChangesAsync();return RedirectToAction(nameof(Detalle),new{id=libroId});
    }
    [Authorize,HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Valorar(int libroId,int puntuacion,string? comentario)
    {
        if(puntuacion<1||puntuacion>5||comentario?.Length>1000)return BadRequest();
        if(!await _db.Libros.AnyAsync(x=>x.Id==libroId&&x.Activo))return NotFound();
        var uid=int.Parse(_users.GetUserId(User)!);
        var actual=await _db.Valoraciones.FirstOrDefaultAsync(x=>x.LibroId==libroId&&x.UsuarioId==uid);
        if(actual==null)_db.Valoraciones.Add(new ValoracionLibro{LibroId=libroId,UsuarioId=uid,Puntuacion=puntuacion,Comentario=comentario?.Trim()});
        else {actual.Puntuacion=puntuacion;actual.Comentario=comentario?.Trim();actual.Fecha=DateTime.Today;}
        await _db.SaveChangesAsync();return RedirectToAction(nameof(Detalle),new{id=libroId});
    }
    [Authorize,HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Reservar(int libroId)
    {
        if(!await _db.Libros.AnyAsync(x=>x.Id==libroId&&x.Activo))return NotFound();
        var uid=int.Parse(_users.GetUserId(User)!);
        if(await _db.LibrosReservas.AnyAsync(x=>x.LibroId==libroId&&x.Reserva.UsuarioId==uid))
        {TempData["Mensaje"]="Ya tienes una reserva registrada para este libro.";return RedirectToAction(nameof(Detalle),new{id=libroId});}
        var reserva=new ReservaLibro{UsuarioId=uid,FechaReserva=DateTime.Today};
        reserva.Libros.Add(new LibroReserva{LibroId=libroId});
        _db.Reservas.Add(reserva);await _db.SaveChangesAsync();
        TempData["Mensaje"]="Reserva registrada en MySQL.";
        return RedirectToAction(nameof(Detalle),new{id=libroId});
    }
    [AllowAnonymous]
    public async Task<IActionResult> Gratis(string? buscar,string idioma="es",string? fuente=null)
    {
        idioma=idioma is "es" or "en" or "todos" ? idioma : "es";
        buscar=buscar?.Trim();if(buscar?.Length>90)buscar=buscar[..90];
        var consulta=_db.Libros.AsNoTracking().Where(l=>l.Activo && l.PrecioRenta==0);
        if(!string.IsNullOrWhiteSpace(buscar))
            consulta=consulta.Where(l=>l.Titulo.Contains(buscar)||l.Autor.Contains(buscar)||l.Genero.Contains(buscar));
        // MySQL es la fuente principal. Gutenberg se consulta solo si se solicita
        // expresamente, para que la biblioteca gratis funcione incluso sin Internet.
        var locales=await consulta.OrderBy(l=>l.Titulo).ToListAsync();
        var buscarExternos=fuente=="gutenberg";
        var externos=buscarExternos ? await _libres.BuscarAsync(buscar??"",idioma) : new List<LibroEnLinea>();
        return View(new LibrosLibresVista { Buscar=buscar,LocalesGratis=locales,Libros=externos,
            MostrarGutenberg=buscarExternos,
            Aviso=buscarExternos&&externos.Count==0
              ? "El catálogo externo no devolvió resultados. Tus libros gratuitos de MySQL siguen disponibles." : null });
    }
    [AllowAnonymous]
    public async Task<IActionResult> LeerGratis(int id)
    {
        var book=await _libres.ObtenerAsync(id);
        if(book is null)return NotFound();
        var text=await _libres.LeerTextoAsync(book);
        if(string.IsNullOrWhiteSpace(text))
        {TempData["Error"]="El texto no está disponible en formato compatible. Puedes abrirlo en Project Gutenberg.";return Redirect(book.Pagina);}
        return View("Lectura",new LecturaVista{ Titulo=book.Titulo,Autor=book.Autor,Texto=text,
            Origen="Texto gratuito: Project Gutenberg · verifica las condiciones de uso en tu país" });
    }
}
