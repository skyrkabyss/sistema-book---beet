using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace GestorTareas.Models;
// La entidad Identity se almacena DIRECTAMENTE en la tabla `usuarios` del diagrama.
public class AppUser : IdentityUser<int>
{
    [Required, StringLength(50)] public string Nombre { get; set; } = "";
    [Required, StringLength(50)] public string Apellido { get; set; } = "";
    public DateTime FechaRegistro { get; set; } = DateTime.Today;
    [Required, StringLength(20)] public string Rol { get; set; } = "Usuario";
    public ICollection<Alquiler> Alquileres { get; set; } = new List<Alquiler>();
    public ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();
}
