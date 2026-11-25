using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GestorTareas.Data;
using GestorTareas.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Controllers;
[ApiController]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly WizPayService _gateway;
    private readonly ILogger<WebhooksController> _log;
    public WebhooksController(AppDbContext db,WizPayService gateway,ILogger<WebhooksController> log)
        => (_db,_gateway,_log)=(db,gateway,log);

    [HttpPost("webhooks/wizpay")]
    [IgnoreAntiforgeryToken] // Comunicación servidor-servidor: usar secreto Bearer, no cookies.
    public async Task<IActionResult> WizPay()
    {
        if (!_gateway.Disponible || string.IsNullOrWhiteSpace(_gateway.WebhookSecret)) return StatusCode(503);
        var expected=Encoding.UTF8.GetBytes("Bearer "+_gateway.WebhookSecret);
        var received=Encoding.UTF8.GetBytes(Request.Headers.Authorization.ToString());
        if(received.Length!=expected.Length || !CryptographicOperations.FixedTimeEquals(received,expected)) return Unauthorized();
        if (Request.ContentLength is > 12000) return BadRequest();
        JsonDocument doc;
        try { doc=await JsonDocument.ParseAsync(Request.Body); }
        catch(JsonException){return BadRequest();}
        using(doc)
        {
            var root=doc.RootElement;
            string field(string name) => root.TryGetProperty(name,out var e) ? e.ToString() : "";
            if (field("event")!="payment.qr.success") return Ok();
            if (field("environment")!=_gateway.Modo) return BadRequest("Ambiente inesperado");
            var qrId=field("qr_id");var order=field("order_id");var reference=field("reference");
            if (qrId.Length==0 || reference.Length==0) return BadRequest();
            if(!root.TryGetProperty("amount",out var amountEl) || !amountEl.TryGetDecimal(out decimal amount)) return BadRequest();
            if(field("currency")!="BOB") return BadRequest();
            var pago=await _db.Pagos.Include(x=>x.Alquiler).FirstOrDefaultAsync(x=>x.QrId==qrId && x.Proveedor=="WizPay");
            if (pago is null) { _log.LogWarning("QR desconocido en webhook");return NotFound(); }
            if (pago.TransaccionExterna!=reference || order!=$"ALQ-{pago.AlquilerId}" || pago.Monto!=amount) return BadRequest("El pago no coincide con la orden");
            if (pago.Estado=="Pagado" && pago.Alquiler.Estado=="Activo") return Ok(); // idempotente
            if (pago.Estado!="Pendiente" || pago.Alquiler.Estado!="PendientePago") return Conflict();
            // Confirmación auténtica del proveedor, NO por el navegador o comprobante del cliente.
            var now=DateTime.UtcNow;
            pago.Estado="Pagado";
            pago.VerificadoEn=now;
            pago.Alquiler.Estado="Activo";
            pago.Alquiler.InicioEn=now;
            pago.Alquiler.VenceEn=now.AddDays(TarifasAlquiler.Valida(pago.Alquiler.DuracionDias) ? pago.Alquiler.DuracionDias : 7);
            await _db.SaveChangesAsync();
            _log.LogInformation("Pago QR confirmado para alquiler {Id}",pago.AlquilerId);
            return Ok();
        }
    }
}
