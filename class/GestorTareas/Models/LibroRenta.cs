using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace GestorTareas.Models;
public class LibroRenta
{
    public int Id { get; set; }
    [Required, StringLength(120)] public string Titulo { get; set; } = "";
    [Required, StringLength(100)] public string Autor { get; set; } = "";
    [Required, StringLength(50)] public string Genero { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int? AnioPublicacion { get; set; }
    [Required, StringLength(20)] public string Estado { get; set; } = "Disponible";
    public int CategoriaId { get; set; }
    public CategoriaLibro? Categoria { get; set; }
    public int AutorId { get; set; }
    public AutorLibro? AutorRegistro { get; set; }
    [Range(typeof(decimal), "0", "1000")]
    [Column(TypeName = "decimal(10,2)")] public decimal PrecioRenta { get; set; } = 12;
    [StringLength(160)] public string? RutaLectura { get; set; }
    // URL https de lectura externa o ruta interna /Libros/ArchivoPdf/{id}.
    [StringLength(2048)] public string? UrlLectura { get; set; }
    // Nombre aleatorio, nunca aportado por el navegador, almacenado fuera de wwwroot.
    [StringLength(160)] public string? ArchivoPdf { get; set; }
    [StringLength(100)] public string? Derechos { get; set; }
    public bool Activo { get; set; } = true;
    public ICollection<Alquiler> Alquileres { get; set; } = new List<Alquiler>();
}
