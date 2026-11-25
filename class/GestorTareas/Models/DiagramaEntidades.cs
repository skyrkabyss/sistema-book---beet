using System.ComponentModel.DataAnnotations;
namespace GestorTareas.Models;
// Tablas normalizadas de la primera imagen. Son entidades reales de EF / MySQL.
public class GeneroMusica { public int Id {get;set;} [StringLength(45)] public string Nombre {get;set;}=""; public string? Descripcion {get;set;} }
public class CategoriaLibro { public int Id {get;set;} [StringLength(45)] public string Nombre {get;set;}=""; public string? Descripcion {get;set;} }
public class Editorial { public int Id {get;set;} [StringLength(100)] public string Nombre {get;set;}=""; public string? Pais {get;set;} public string? Correo {get;set;} public string? Telefono {get;set;} }
public class AutorLibro
{
    public int Id {get;set;}
    [StringLength(45)] public string Nombre {get;set;}="";
    [StringLength(45)] public string Apellido {get;set;}="";
    public string? Nacionalidad {get;set;}
    public DateTime? FechaNacimiento {get;set;}
    public int EditorialId {get;set;}
    public Editorial? Editorial {get;set;}
}
public class MeGusta
{
    public int Id {get;set;}
    public DateTime Fecha {get;set;}=DateTime.UtcNow;
    public int UsuarioId {get;set;}
    public int CancionId {get;set;}
    public AppUser Usuario {get;set;}=null!;
    public Cancion Cancion {get;set;}=null!;
}
public class ComentarioLibro
{
    public int Id {get;set;}
    [Required] public string Texto {get;set;}="";
    public DateTime Fecha {get;set;}=DateTime.UtcNow;
    public int UsuarioId {get;set;}
    public int LibroId {get;set;}
    public AppUser Usuario {get;set;}=null!;
    public LibroRenta Libro {get;set;}=null!;
}
public class ValoracionLibro
{
    public int Id {get;set;}
    [Range(1,5)] public int Puntuacion {get;set;}
    public string? Comentario {get;set;}
    public DateTime Fecha {get;set;}=DateTime.Today;
    public int UsuarioId {get;set;}
    public int LibroId {get;set;}
    public AppUser Usuario {get;set;}=null!;
    public LibroRenta Libro {get;set;}=null!;
}
public class ReservaLibro
{
    public int Id {get;set;}
    public DateTime FechaReserva {get;set;}=DateTime.Today;
    public int UsuarioId {get;set;}
    public AppUser Usuario {get;set;}=null!;
    public ICollection<LibroReserva> Libros {get;set;}=new List<LibroReserva>();
}
public class LibroReserva
{
    public int LibroId {get;set;}
    public int ReservaId {get;set;}
    public LibroRenta Libro {get;set;}=null!;
    public ReservaLibro Reserva {get;set;}=null!;
}
