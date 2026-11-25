using System.Text.Json;

namespace GestorTareas.Services;

public sealed class DatosQrBanco
{
    public string Titular { get; set; } = "";
    public string Banco { get; set; } = "";
}

// QR estático emitido por el banco: NO genera ni comprueba transacciones.
// Únicamente un administrador autorizado puede configurar la imagen.
public sealed class PagoManualQrService
{
    private readonly IWebHostEnvironment _env;
    public PagoManualQrService(IWebHostEnvironment env) => _env=env;
    private string Carpeta => Path.Combine(_env.ContentRootPath, "PrivatePayments");
    private string Imagen => Path.Combine(Carpeta, "qr_banco.png");
    private string Metadata => Path.Combine(Carpeta, "datos_banco.json");

    public DatosQrBanco? Datos
    {
        get
        {
            if (!File.Exists(Imagen) || !File.Exists(Metadata)) return null;
            try
            {
                var datos=JsonSerializer.Deserialize<DatosQrBanco>(File.ReadAllText(Metadata));
                return datos is not null && !string.IsNullOrWhiteSpace(datos.Banco) &&
                       !string.IsNullOrWhiteSpace(datos.Titular) ? datos : null;
            }
            catch (IOException) { return null; }
            catch (JsonException) { return null; }
        }
    }
    public bool Disponible => Datos is not null;
    public string? RutaImagen => Disponible ? Imagen : null;

    public async Task ConfigurarAsync(IFormFile archivo, string titular, string banco)
    {
        if (string.IsNullOrWhiteSpace(titular) || titular.Trim().Length>120 ||
            string.IsNullOrWhiteSpace(banco) || banco.Trim().Length>100)
            throw new ArgumentException("Escribe el nombre del titular y la entidad bancaria.");
        if (archivo is null || archivo.Length<8 || archivo.Length>1_000_000 ||
            !string.Equals(Path.GetExtension(archivo.FileName), ".png", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El QR debe ser una imagen PNG de hasta 1 MB.");
        var firma=new byte[] {137,80,78,71,13,10,26,10};
        Directory.CreateDirectory(Carpeta);
        var temporal=Path.Combine(Carpeta,Guid.NewGuid().ToString("N")+".tmp");
        try
        {
            await using (var entrada=archivo.OpenReadStream())
            {
                var encabezado=new byte[8];
                await entrada.ReadExactlyAsync(encabezado);
                if (!encabezado.SequenceEqual(firma))
                    throw new ArgumentException("El archivo no tiene una firma PNG válida.");
                await using var salida=File.Create(temporal);
                await salida.WriteAsync(encabezado);
                await entrada.CopyToAsync(salida);
            }
            File.Move(temporal, Imagen, true);
            File.WriteAllText(Metadata,JsonSerializer.Serialize(new DatosQrBanco
            {
                Titular=titular.Trim(), Banco=banco.Trim()
            }));
        }
        finally { if (File.Exists(temporal)) File.Delete(temporal); }
    }
}
