using System.ComponentModel.DataAnnotations;
namespace GestorTareas.Models;
public class Alquiler
{
    public int Id { get; set; }
    public DateTime FechaAlquilada { get; set; } = DateTime.Today;
    public DateTime? FechaDevolucion { get; set; }
    [StringLength(20)] public string Estado { get; set; } = "PendientePago";
    public int UsuarioId { get; set; }
    public AppUser Usuario { get; set; } = null!;
    public int LibroId { get; set; }
    public LibroRenta Libro { get; set; } = null!;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? InicioEn { get; set; }
    public DateTime? VenceEn { get; set; }
    public int DuracionDias { get; set; } = 7; // Valor histórico para alquileres previos a la actualización.
    public Pago? Pago { get; set; }
}
