namespace GestorTareas.Models;

public sealed class EstadisticasVista
{
    public int TotalLibros { get; set; }
    public int TotalUsuarios { get; set; }
    public int TotalAlquileres { get; set; }
    public int Activos { get; set; }
    public int TotalPlaylists { get; set; }
    public int UsuariosConPlaylist { get; set; }
    public double DuracionMedia { get; set; }
    public double PorcentajePlaylists => TotalUsuarios==0?0:100.0*UsuariosConPlaylist/TotalUsuarios;
    public List<EstadisticaItem> Categorias { get; set; }=new();
    public List<EstadisticaItem> Estados { get; set; }=new();
    public List<EstadisticaItem> LibrosSolicitados { get; set; }=new();
    public List<EstadisticaItem> UltimosDias { get; set; }=new();
    public string Distribucion { get; set; }="binomial";
    public string TituloDistribucion { get; set; }="";
    public string ContextoDistribucion { get; set; }="";
    public string Parametros { get; set; }="";
    public bool ParametrosDidacticos { get; set; }
    public List<ProbabilidadPunto> Probabilidades { get; set; }=new();
    public double MediaTeorica { get; set; }
    public double VarianzaTeorica { get; set; }
}
public sealed class EstadisticaItem
{
    public string Etiqueta { get; set; }="";
    public int Cantidad { get; set; }
    public double Porcentaje { get; set; }
}
public sealed class ProbabilidadPunto
{
    public int K { get; set; }
    public double Probabilidad { get; set; }
}
