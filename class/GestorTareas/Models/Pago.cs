using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace GestorTareas.Models;
public class Pago
{
    public int Id { get; set; }
    public int AlquilerId { get; set; }
    public Alquiler Alquiler { get; set; } = null!;
    [Required, StringLength(20)] public string Metodo { get; set; } = "QR";
    [StringLength(30)] public string Estado { get; set; } = "Pendiente";
    [StringLength(100)] public string? Referencia { get; set; }
    [Column(TypeName = "decimal(10,2)")] public decimal Monto { get; set; }
    [StringLength(20)] public string? Proveedor { get; set; }
    [StringLength(120)] public string? TransaccionExterna { get; set; }
    [StringLength(80)] public string? QrId { get; set; }
    // El QR no es un QR inventado: esta imagen solo la entrega el proveedor real.
    [Column(TypeName = "longtext")] public string? QrImagenBase64 { get; set; }
    public DateTime? QrVenceEn { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public DateTime? VerificadoEn { get; set; }
}
