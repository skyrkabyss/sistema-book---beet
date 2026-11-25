using GestorTareas.Data;
using GestorTareas.Models;
using GestorTareas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Controllers;
public class MusicaController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly IWebHostEnvironment _environment;
    private readonly MusicaOnlineService _online;
    public MusicaController(AppDbContext db,UserManager<AppUser> users,IWebHostEnvironment environment,MusicaOnlineService online)
        => (_db,_users,_environment,_online)=(db,users,environment,online);

    [AllowAnonymous,HttpGet]
    public async Task<IActionResult> Escuchar(int id)
    {
        var cancion=await _db.Canciones.AsNoTracking().FirstOrDefaultAsync(c=>c.Id==id&&c.Activa&&c.Fuente=="Local");
        if(cancion?.AudioUrl is not string nombre ||
           !System.Text.RegularExpressions.Regex.IsMatch(nombre,@"^[a-zA-Z0-9_-]+\.(wav|mp3)$")) return NotFound();
        var ruta=Path.Combine(_environment.ContentRootPath,"PrivateAudio",nombre);
        if(!System.IO.File.Exists(ruta))return NotFound();
        Response.Headers.CacheControl="public, max-age=3600";
        Response.Headers["X-Content-Type-Options"]="nosniff";
        return PhysicalFile(ruta,nombre.EndsWith(".mp3",StringComparison.OrdinalIgnoreCase)?"audio/mpeg":"audio/wav",enableRangeProcessing:true);
    }
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? buscar)
    {
        var vm=new MusicaVista{ Buscar=buscar,YoutubeConfigurado=_online.YoutubeConfigurado };
        var q=_db.Canciones.AsNoTracking().Where(c=>c.Activa&&c.Fuente=="Local");
        if(!string.IsNullOrWhiteSpace(buscar)) q=q.Where(c=>c.Titulo.Contains(buscar)||c.Artista.Contains(buscar)||c.Genero.Contains(buscar));
        vm.Locales=await q.OrderByDescending(c=>c.Id).Take(30).ToListAsync();
        if(User.Identity?.IsAuthenticated==true)
        {
            int uid=int.Parse(_users.GetUserId(User)!);
            ViewBag.MisMeGusta=await _db.MeGusta.AsNoTracking().Where(x=>x.UsuarioId==uid).Select(x=>x.CancionId).ToListAsync();
            vm.Playlists=await _db.Playlists.AsNoTracking().Where(p=>p.UsuarioId==uid).OrderBy(p=>p.Nombre).ToListAsync();
        }
        if(!string.IsNullOrWhiteSpace(buscar))
        {
            buscar=buscar.Trim();
            if(buscar.Length>90) buscar=buscar[..90];
            var commons=_online.BuscarCommonsAsync(buscar);
            var youtube=_online.BuscarYouTubeAsync(buscar);
            await Task.WhenAll(commons,youtube);
            vm.Libres=commons.Result;vm.Youtube=youtube.Result;
            if(!vm.Libres.Any()&&!vm.Youtube.Any()) vm.Aviso=vm.YoutubeConfigurado
                ? "Sin resultados en YouTube o Wikimedia Commons. Prueba otro título."
                : "Para buscar canciones comerciales y escucharlas aquí, configura YouTube:ApiKey. Las pistas locales siguen funcionando.";
        }
        return View(vm);
    }
    [AllowAnonymous,HttpGet]
    public async Task<IActionResult> ReproducirOnline(int id)
    {
        var song=await _db.Canciones.AsNoTracking().FirstOrDefaultAsync(c=>c.Id==id&&c.Fuente=="Commons"&&c.Activa);
        if(song?.IdExterno is null)return NotFound();
        var track=await _online.ObtenerAsync("Commons",song.IdExterno);
        if(track is null || !track.Reproducible)return NotFound();
        return Redirect(track.UrlEscucha); // Destino validado como upload.wikimedia.org
    }
    [Authorize]
    public async Task<IActionResult> Playlists()
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        return View(await _db.Playlists.AsNoTracking().Where(p=>p.UsuarioId==uid)
            .Include(p=>p.Canciones).ThenInclude(x=>x.Cancion).OrderBy(p=>p.Nombre).ToListAsync());
    }
    // Reproductor continuo: API privada para la pestaña de playlists dentro del lector.
    // Solo entrega canciones de playlists pertenecientes a la sesión actual.
    [HttpGet,AllowAnonymous]
    public async Task<IActionResult> ColaLector()
    {
        Response.Headers.CacheControl = "private, no-store";
        if (User.Identity?.IsAuthenticated != true ||
            !int.TryParse(_users.GetUserId(User), out var uid))
            return Json(new { authenticated = false, playlists = Array.Empty<object>() });

        var listas = await _db.Playlists.AsNoTracking()
            .Where(p => p.UsuarioId == uid)
            .OrderBy(p => p.Nombre)
            .Take(40)
            .Include(p => p.Canciones).ThenInclude(pc => pc.Cancion)
            .ToListAsync();
        var resultado = listas.Select(p => new
        {
            nombre = p.Nombre,
            tracks = p.Canciones.OrderBy(pc => pc.Orden).Take(150)
                .Where(pc => pc.Cancion != null && pc.Cancion.Activa)
                .Select(pc => new
                {
                    titulo = pc.Cancion.Titulo,
                    artista = pc.Cancion.Artista,
                    fuente = pc.Cancion.Fuente,
                    modo = pc.Cancion.Fuente == "YouTube"
                        && System.Text.RegularExpressions.Regex.IsMatch(pc.Cancion.IdExterno ?? "", @"^[A-Za-z0-9_-]{11}$")
                        ? "youtube" : (pc.Cancion.Fuente is "Local" or "Commons" ? "audio" : "no-disponible"),
                    youtubeId = pc.Cancion.Fuente == "YouTube" ? pc.Cancion.IdExterno : null,
                    audioUrl = pc.Cancion.Fuente == "Local" ? Url.Action("Escuchar", "Musica", new { id = pc.CancionId })
                             : pc.Cancion.Fuente == "Commons" ? Url.Action("ReproducirOnline", "Musica", new { id = pc.CancionId })
                             : null
                }).ToArray()
        }).ToArray();
        return Json(new { authenticated = true, playlists = resultado });
    }

    [HttpPost,Authorize,ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearPlaylist(PlaylistForm form)
    {
        if(!ModelState.IsValid){TempData["Error"]="Nombre de playlist no válido.";return RedirectToAction(nameof(Playlists));}
        int uid=int.Parse(_users.GetUserId(User)!);
        _db.Playlists.Add(new Playlist { Nombre=form.Nombre.Trim(),UsuarioId=uid });
        await _db.SaveChangesAsync();
        TempData["Mensaje"]="Playlist creada.";
        return RedirectToAction(nameof(Playlists));
    }
    [HttpPost,Authorize,ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(int playlistId,int cancionId)
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        if(!await _db.Playlists.AnyAsync(p=>p.Id==playlistId&&p.UsuarioId==uid)) return Forbid();
        if(!await _db.Canciones.AnyAsync(c=>c.Id==cancionId&&c.Activa))return NotFound();
        if(!await _db.PlaylistCanciones.AnyAsync(x=>x.PlaylistId==playlistId&&x.CancionId==cancionId))
        { var orden=(await _db.PlaylistCanciones.Where(x=>x.PlaylistId==playlistId).MaxAsync(x=>(int?)x.Orden)??0)+1;
          _db.PlaylistCanciones.Add(new PlaylistCancion{ PlaylistId=playlistId,CancionId=cancionId,Orden=orden,FechaAgregada=DateTime.Today });await _db.SaveChangesAsync(); }
        TempData["Mensaje"]="Canción agregada a tu playlist.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost,Authorize,ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarOnline(int playlistId,string fuente,string idExterno)
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        if(!await _db.Playlists.AnyAsync(p=>p.Id==playlistId&&p.UsuarioId==uid))return Forbid();
        if((fuente!="Commons"&&fuente!="Apple"&&fuente!="YouTube") || string.IsNullOrWhiteSpace(idExterno) || idExterno.Length>255)return BadRequest();
        var pista=await _online.ObtenerAsync(fuente,idExterno);
        if(pista is null){TempData["Error"]="No fue posible verificar la canción y su fuente.";return RedirectToAction(nameof(Index));}
        var cancion=await _db.Canciones.FirstOrDefaultAsync(c=>c.Fuente==fuente&&c.IdExterno==idExterno);
        if(cancion is null)
        {
            cancion=new Cancion { Fuente=fuente,IdExterno=idExterno,
                Titulo=pista.Titulo[..Math.Min(110,pista.Titulo.Length)],
                Artista=pista.Artista[..Math.Min(100,pista.Artista.Length)],
                Genero=fuente=="Apple"?"Comercial":fuente=="YouTube"?"YouTube":"Licencia libre",
                AudioUrl=null,
                EnlaceOrigen=pista.UrlOrigen,Licencia=pista.Licencia[..Math.Min(255,pista.Licencia.Length)] };
            cancion.GeneroId=await CatalogoHelper.GeneroIdAsync(_db,cancion.Genero);
            _db.Canciones.Add(cancion);await _db.SaveChangesAsync();
        }
        if(!await _db.PlaylistCanciones.AnyAsync(x=>x.PlaylistId==playlistId&&x.CancionId==cancion.Id))
        { var orden=(await _db.PlaylistCanciones.Where(x=>x.PlaylistId==playlistId).MaxAsync(x=>(int?)x.Orden)??0)+1;
          _db.PlaylistCanciones.Add(new PlaylistCancion{ PlaylistId=playlistId,CancionId=cancion.Id,Orden=orden,FechaAgregada=DateTime.Today });await _db.SaveChangesAsync(); }
        TempData["Mensaje"]="Pista añadida a la playlist.";
        return RedirectToAction(nameof(Playlists));
    }
    [HttpPost,Authorize,ValidateAntiForgeryToken]
    public async Task<IActionResult> Quitar(int playlistId,int cancionId)
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        if(!await _db.Playlists.AnyAsync(p=>p.Id==playlistId&&p.UsuarioId==uid)) return Forbid();
        var item=await _db.PlaylistCanciones.FirstOrDefaultAsync(x=>x.PlaylistId==playlistId&&x.CancionId==cancionId);
        if(item is not null){_db.PlaylistCanciones.Remove(item);await _db.SaveChangesAsync();}
        return RedirectToAction(nameof(Playlists));
    }
    // Tabla me_gusta del diagrama: un usuario puede activar/desactivar un me gusta.
    [HttpPost,Authorize,ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarMeGusta(int cancionId)
    {
        var uid=int.Parse(_users.GetUserId(User)!);
        if(!await _db.Canciones.AnyAsync(x=>x.Id==cancionId&&x.Activa)) return NotFound();
        var like=await _db.MeGusta.FirstOrDefaultAsync(x=>x.UsuarioId==uid&&x.CancionId==cancionId);
        if(like==null){_db.MeGusta.Add(new MeGusta{UsuarioId=uid,CancionId=cancionId});TempData["Mensaje"]="Añadida a favoritos.";}
        else{_db.MeGusta.Remove(like);TempData["Mensaje"]="Quitada de favoritos.";}
        await _db.SaveChangesAsync();return RedirectToAction(nameof(Index));
    }
    [HttpPost,Authorize,ValidateAntiForgeryToken]
    public async Task<IActionResult> BorrarPlaylist(int id)
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        var p=await _db.Playlists.FirstOrDefaultAsync(p=>p.Id==id&&p.UsuarioId==uid);
        if(p is null)return NotFound();
        _db.Playlists.Remove(p);await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Playlists));
    }
}
