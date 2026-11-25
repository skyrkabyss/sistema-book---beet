using System.ComponentModel.DataAnnotations;
namespace GestorTareas.Models;
public class Cancion
{
    public int Id { get; set; }
    [Required, StringLength(110)] public string Titulo { get; set; } = "";
    [Required, StringLength(100)] public string Artista { get; set; } = "";
    [Required, StringLength(50)] public string Genero { get; set; } = "";
    public int GeneroId { get; set; }
    public GeneroMusica? GeneroRegistro { get; set; }
    public TimeSpan? Duracion { get; set; }
    public int? AnioLanzamiento { get; set; }
    public string? Letra { get; set; }
    [StringLength(255)] public string? AudioUrl { get; set; }
    [StringLength(20)] public string Fuente { get; set; } = "Local";
    [StringLength(255)] public string? IdExterno { get; set; }
    [StringLength(800)] public string? EnlaceOrigen { get; set; }
    [StringLength(255)] public string? Licencia { get; set; }
    public bool Activa { get; set; } = true;
    public ICollection<PlaylistCancion> EnPlaylists { get; set; } = new List<PlaylistCancion>();
}
