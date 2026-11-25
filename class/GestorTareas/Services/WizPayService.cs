using System.Net.Http.Json;
using System.Text.Json;

namespace GestorTareas.Services;
public sealed class QrGenerado
{
    public string Reference { get; init; } = "";
    public string QrId { get; init; } = "";
    public string PngBase64 { get; init; } = "";
    public DateTime ExpiresUtc { get; init; }
}
public sealed class WizPayService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _factory;
    public WizPayService(IConfiguration config, IHttpClientFactory factory)
        => (_config,_factory)=(config,factory);
    public string Modo => _config["WizPay:Mode"]?.ToUpperInvariant()=="PRODUCCION" ? "PRODUCCION" : "SANDBOX";
    public bool Disponible => _config.GetValue<bool>("WizPay:Enabled") &&
        !string.IsNullOrWhiteSpace(_config["WizPay:ApiKey"]) &&
        !string.IsNullOrWhiteSpace(_config["WizPay:WebhookSecret"]);
    public string? WebhookSecret => _config["WizPay:WebhookSecret"];

    public async Task<QrGenerado> GenerarQrAsync(int alquilerId, decimal importe, string titulo)
    {
        if (!Disponible) throw new InvalidOperationException("Configura WizPay:Enabled, ApiKey y WebhookSecret antes de cobrar.");
        var client=_factory.CreateClient("wizpay");
        using var request=new HttpRequestMessage(HttpMethod.Post,
            Modo=="SANDBOX" ? "https://api-test.wizpay.app/qr/generate" : "https://api.wizpay.app/qr/generate") {
            Content=JsonContent.Create(new {
                amount=importe, order_id=$"ALQ-{alquilerId}", name=$"BiblioBeat #{alquilerId}",
                description=$"Alquiler digital: {titulo[..Math.Min(titulo.Length,70)]}",
                id_currency=1, moneda="BOB" }) };
        request.Headers.Add("x-wpay-key",_config["WizPay:ApiKey"]);
        using var response=await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root=doc.RootElement;
        var qr=root.GetProperty("qr");
        var reference=root.GetProperty("reference").GetString()??"";
        var number=qr.GetProperty("numero_referencia").ToString();
        var image=qr.GetProperty("imagen").GetString()??"";
        if (!reference.StartsWith("WP-",StringComparison.Ordinal) || number.Length is < 1 or > 80 ||
            image.Length is < 100 or > 250000 || !Convert.TryFromBase64String(image,new Span<byte>(new byte[image.Length]),out _))
            throw new InvalidOperationException("El proveedor respondió sin un QR de pago válido.");
        return new QrGenerado { Reference=reference,QrId=number,PngBase64=image,
            ExpiresUtc=DateTime.UtcNow.AddMinutes(10) };
    }
}
