using GestorTareas.Data;
using GestorTareas.Models;
using GestorTareas.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Controllers;
[Authorize]
public class AlquileresController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _users;
    private readonly WizPayService _pay;
    private readonly PagoManualQrService _manual;
    private readonly ILogger<AlquileresController> _log;
    public AlquileresController(AppDbContext db, UserManager<AppUser> users,WizPayService pay, PagoManualQrService manual, ILogger<AlquileresController> log)
        => (_db,_users,_pay,_manual,_log)=(db,users,pay,manual,log);
    public async Task<IActionResult> Index()
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        var list=await _db.Alquileres.AsNoTracking().Include(a=>a.Libro).Include(a=>a.Pago)
            .Where(a=>a.UsuarioId==uid).OrderByDescending(a=>a.CreadoEn).ToListAsync();
        return View(list);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Solicitar(AlquilerForm form)
    {
        if (!ModelState.IsValid || form.Metodo!="QR")
        {TempData["Error"]="Solo se permite el QR integrado para este alquiler.";return RedirectToAction("Index","Libros");}
        if (!TarifasAlquiler.Valida(form.DuracionDias))
        { TempData["Error"]="Elige un período permitido: 1, 3, 7, 14 o 30 días.";return RedirectToAction("Detalle","Libros",new{id=form.LibroId}); }
        if (!_manual.Disponible && !_pay.Disponible)
        {TempData["Error"]="Aún no hay un QR bancario autorizado configurado ni una pasarela habilitada.";return RedirectToAction("Index","Libros");}
        var libro=await _db.Libros.FirstOrDefaultAsync(l=>l.Id==form.LibroId && l.Activo);
        if (libro is null || (string.IsNullOrWhiteSpace(libro.RutaLectura) &&
            string.IsNullOrWhiteSpace(libro.ArchivoPdf) && string.IsNullOrWhiteSpace(libro.UrlLectura)) ||
            string.IsNullOrWhiteSpace(libro.Derechos) || libro.PrecioRenta<=0)
        {TempData["Error"]="Este libro no tiene una edición digital autorizada para alquilar.";return RedirectToAction("Index","Libros");}
        // Para alquiler de duración limitada, solo documentos protegidos por este servidor.
        // Una URL externa no permite revocar el acceso al vencer.
        if (string.IsNullOrWhiteSpace(libro.ArchivoPdf) && string.IsNullOrWhiteSpace(libro.RutaLectura))
        {TempData["Error"]="Este libro solo tiene enlace externo, que no permite controlar el vencimiento. El administrador debe subir un PDF o texto autorizado.";return RedirectToAction("Detalle","Libros",new{id=libro.Id});}
        decimal monto=TarifasAlquiler.Calcular(libro.PrecioRenta, form.DuracionDias);
        int uid=int.Parse(_users.GetUserId(User)!);
        // El QR bancario proviene de una cuenta autorizada; nunca se confirma por una captura.
        if (_manual.Disponible)
        {
            var existente=await _db.Alquileres.Include(a=>a.Pago)
                .Where(a=>a.LibroId==libro.Id && a.UsuarioId==uid &&
                    a.Estado=="PendientePago" && a.Pago!=null && a.Pago.Proveedor=="BancoManual")
                .OrderByDescending(a=>a.CreadoEn).FirstOrDefaultAsync();
            if(existente is not null)
            {
                if (existente.DuracionDias!=form.DuracionDias)
                    TempData["Error"]="Ya tienes una solicitud pendiente para este libro con otra duración. Cancélala (si aún no notificaste el pago) antes de cambiarla.";
                return RedirectToAction(nameof(PagarQr),new{id=existente.Id});
            }
            var pedido=new Alquiler { LibroId=libro.Id, UsuarioId=uid, Estado="PendientePago", DuracionDias=form.DuracionDias };
            pedido.Pago=new Pago { Metodo="QR",Monto=monto,
                Estado="Pendiente", Proveedor="BancoManual" };
            _db.Alquileres.Add(pedido);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(PagarQr),new{id=pedido.Id});
        }
        // Flujo anterior de pasarela WizPay, cuando no hay QR manual.
        var alquiler=new Alquiler { LibroId=libro.Id,UsuarioId=uid,Estado="PendientePago",DuracionDias=form.DuracionDias };
        alquiler.Pago=new Pago { Metodo="QR", Monto=monto, Estado="Pendiente", Proveedor="WizPay" };
        _db.Alquileres.Add(alquiler);
        await _db.SaveChangesAsync();
        try
        {
            var qr=await _pay.GenerarQrAsync(alquiler.Id,monto,libro.Titulo);
            alquiler.Pago.TransaccionExterna=qr.Reference;
            alquiler.Pago.QrId=qr.QrId;
            alquiler.Pago.QrImagenBase64=qr.PngBase64;
            alquiler.Pago.QrVenceEn=qr.ExpiresUtc;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(PagarQr),new{id=alquiler.Id});
        }
        catch(Exception ex)
        {
            _log.LogError(ex,"No se generó QR para alquiler {Id}",alquiler.Id);
            alquiler.Estado="Cancelado";
            alquiler.Pago.Estado="Error";
            await _db.SaveChangesAsync();
            TempData["Error"]="No fue posible generar el QR con la pasarela. No hay ningún cobro confirmado.";
            return RedirectToAction(nameof(Index));
        }
    }
    [HttpGet]
    public async Task<IActionResult> PagarQr(int id)
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        var alquiler=await _db.Alquileres.AsNoTracking().Include(a=>a.Libro).Include(a=>a.Pago)
            .FirstOrDefaultAsync(a=>a.Id==id && a.UsuarioId==uid);
        if (alquiler is null) return NotFound();
        if (alquiler.Pago?.Proveedor=="BancoManual")
        {
            ViewBag.Modo="MANUAL";
            ViewBag.DatosBanco=_manual.Datos;
            return View(alquiler);
        }
        if (alquiler.Pago?.Proveedor!="WizPay" || string.IsNullOrWhiteSpace(alquiler.Pago.QrImagenBase64)) return RedirectToAction(nameof(Index));
        ViewBag.Modo=_pay.Modo;
        return View(alquiler);
    }
    [HttpGet]
    [ResponseCache(Location=ResponseCacheLocation.None, NoStore=true)]
    public IActionResult ImagenQrManual()
    {
        var ruta=_manual.RutaImagen;
        if (ruta is null) return NotFound();
        Response.Headers["X-Content-Type-Options"]="nosniff";
        return PhysicalFile(ruta,"image/png");
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclararPagoManual(int id, string? referencia)
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        var alquiler=await _db.Alquileres.Include(x=>x.Pago)
            .FirstOrDefaultAsync(x=>x.Id==id && x.UsuarioId==uid);
        if (alquiler?.Pago?.Proveedor!="BancoManual") return NotFound();
        if (alquiler.Estado!="PendientePago" || alquiler.Pago.Estado!="Pendiente") return BadRequest();
        if (string.IsNullOrWhiteSpace(referencia) || referencia.Trim().Length is < 4 or > 100)
        { TempData["Error"]="Escribe la referencia de la transferencia (4 a 100 caracteres).";
          return RedirectToAction(nameof(PagarQr),new{id}); }
        // La referencia ingresada NO prueba que se recibió el dinero.
        alquiler.Pago.Referencia=referencia.Trim();
        alquiler.Pago.Estado="EnRevision";
        await _db.SaveChangesAsync();
        TempData["Mensaje"]="Aviso recibido. Tu solicitud espera revisión bancaria; el libro sigue bloqueado.";
        return RedirectToAction(nameof(PagarQr),new{id});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Devolver(int id)
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        var alquiler=await _db.Alquileres.FirstOrDefaultAsync(a=>a.Id==id && a.UsuarioId==uid);
        if (alquiler is null) return NotFound();
        if (alquiler.Estado!="Activo") return BadRequest();
        alquiler.Estado="Devuelto";
        alquiler.FechaDevolucion=DateTime.Today;
        await _db.SaveChangesAsync();
        TempData["Mensaje"]="Alquiler devuelto.";
        return RedirectToAction(nameof(Index));
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        int uid=int.Parse(_users.GetUserId(User)!);
        var alquiler=await _db.Alquileres.Include(a=>a.Pago)
            .FirstOrDefaultAsync(a=>a.Id==id && a.UsuarioId==uid);
        if (alquiler is null) return NotFound();
        // No cancelar una operación bancaria emitida hasta conciliarla con el proveedor.
        if (alquiler.Estado!="PendientePago" || alquiler.Pago?.Estado!="Pendiente" ||
            !string.IsNullOrWhiteSpace(alquiler.Pago?.QrId))
            return BadRequest("El pago ya está en revisión o existe un QR emitido. Solicita asistencia para cancelar.");
        alquiler.Estado="Cancelado";
        if(alquiler.Pago is not null) alquiler.Pago.Estado="Cancelado";
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
