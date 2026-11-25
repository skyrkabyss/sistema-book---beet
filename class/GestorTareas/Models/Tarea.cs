using System.ComponentModel.DataAnnotations;

namespace GestorTareas.Models;

public class Tarea
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Descripcion { get; set; }

    public bool Completada { get; set; } = false;

    [DataType(DataType.Date)]
    public DateTime? FechaLimite { get; set; }
}
