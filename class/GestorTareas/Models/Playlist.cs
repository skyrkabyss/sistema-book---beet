using System.ComponentModel.DataAnnotations;
namespace GestorTareas.Models;
public class Playlist
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Today;
    [StringLength(20)] public string Privacidad { get; set; } = "Privada";
    public int UsuarioId { get; set; }
    public AppUser Usuario { get; set; } = null!;
    public ICollection<PlaylistCancion> Canciones { get; set; } = new List<PlaylistCancion>();
}
public class PlaylistCancion
{
    public int Id { get; set; }
    public int Orden { get; set; }
    public DateTime FechaAgregada { get; set; } = DateTime.Today;
    public int PlaylistId { get; set; }
    public Playlist Playlist { get; set; } = null!;
    public int CancionId { get; set; }
    public Cancion Cancion { get; set; } = null!;
}
