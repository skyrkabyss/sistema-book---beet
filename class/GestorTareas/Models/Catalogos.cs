namespace GestorTareas.Models;
public sealed class PistaEnLinea
{
    public string Fuente { get; set; } = "";
    public string IdExterno { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Artista { get; set; } = "";
    public string UrlEscucha { get; set; } = "";
    public string UrlOrigen { get; set; } = "";
    public string Licencia { get; set; } = "";
    public bool Reproducible => Fuente == "Commons" && !string.IsNullOrWhiteSpace(UrlEscucha);
}
public sealed class LibroEnLinea
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public string Idioma { get; set; } = "";
    public string Portada { get; set; } = "";
    public string Pagina { get; set; } = "";
    public string? TextoUrl { get; set; }
    public bool Libre { get; set; }
}
public sealed class MusicaVista
{
    public string? Buscar { get; set; }
    public List<Cancion> Locales { get; set; } = new();
    public List<PistaEnLinea> Libres { get; set; } = new();
    public List<PistaEnLinea> Comerciales { get; set; } = new();
    public List<PistaEnLinea> Youtube { get; set; } = new();
    public bool YoutubeConfigurado { get; set; }
    public List<Playlist> Playlists { get; set; } = new();
    public string? Aviso { get; set; }
}
public sealed class LibrosLibresVista
{
    public string? Buscar { get; set; }
    // Todos los libros locales de precio cero, incluso sin Internet.
    public List<LibroRenta> LocalesGratis { get; set; } = new();
    public List<LibroEnLinea> Libros { get; set; } = new();
    public bool MostrarGutenberg { get; set; }
    public string? Aviso { get; set; }
}
public sealed class LecturaVista
{
    public string Titulo { get; set; } = "";
    public string Autor { get; set; } = "";
    public string Texto { get; set; } = "";
    public string Origen { get; set; } = "";
}
