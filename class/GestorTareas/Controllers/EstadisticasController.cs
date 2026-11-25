using GestorTareas.Data;
using GestorTareas.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestorTareas.Controllers;

// Todo el módulo queda protegido por el rol REAL de ASP.NET Identity.
[Authorize(Roles="Admin")]
public sealed class EstadisticasController : Controller
{
    private readonly AppDbContext _db;
    public EstadisticasController(AppDbContext db)=>_db=db;

    [HttpGet]
    public async Task<IActionResult> Index(string modelo="binomial")
    {
        modelo = modelo is "binomial" or "geometrica" or "hipergeometrica" or "poisson" ? modelo : "binomial";
        var vm=new EstadisticasVista{ Distribucion=modelo };
        var books=await _db.Libros.AsNoTracking().Select(l=>new {l.Id,l.Titulo,l.Genero,l.Activo}).ToListAsync();
        var rents=await _db.Alquileres.AsNoTracking().Select(a=>new {
            a.Id,a.LibroId,a.UsuarioId,a.Estado,a.FechaAlquilada,a.FechaDevolucion,a.CreadoEn
        }).ToListAsync();
        vm.TotalLibros=books.Count;
        vm.TotalUsuarios=await _db.Users.CountAsync();
        vm.TotalAlquileres=rents.Count;
        vm.Activos=rents.Count(a=>a.Estado=="Activo"&&(!a.FechaDevolucion.HasValue||a.FechaDevolucion.Value.Date>=DateTime.Today));
        vm.TotalPlaylists=await _db.Playlists.CountAsync();
        vm.UsuariosConPlaylist=await _db.Playlists.Select(p=>p.UsuarioId).Distinct().CountAsync();
        var dias=rents.Where(r=>r.FechaDevolucion.HasValue && r.FechaDevolucion>=r.FechaAlquilada)
            .Select(r=>(r.FechaDevolucion!.Value-r.FechaAlquilada).TotalDays).ToList();
        vm.DuracionMedia=dias.Count==0?0:dias.Average();

        vm.Categorias=books.GroupBy(b=>string.IsNullOrWhiteSpace(b.Genero)?"Sin categoría":b.Genero)
            .Select(g=>new EstadisticaItem{Etiqueta=g.Key,Cantidad=g.Count()})
            .OrderByDescending(x=>x.Cantidad).ThenBy(x=>x.Etiqueta).ToList();
        foreach(var item in vm.Categorias) item.Porcentaje=vm.TotalLibros==0?0:100.0*item.Cantidad/vm.TotalLibros;
        vm.Estados=rents.GroupBy(r=>r.Estado)
            .Select(g=>new EstadisticaItem{Etiqueta=g.Key,Cantidad=g.Count()})
            .OrderByDescending(x=>x.Cantidad).ToList();
        foreach(var item in vm.Estados)item.Porcentaje=vm.TotalAlquileres==0?0:100.0*item.Cantidad/vm.TotalAlquileres;
        var top=rents.GroupBy(r=>r.LibroId).OrderByDescending(g=>g.Count()).Take(7);
        vm.LibrosSolicitados=top.Select(g=>new EstadisticaItem{
            Etiqueta=books.FirstOrDefault(b=>b.Id==g.Key)?.Titulo??"Libro eliminado",Cantidad=g.Count()}).ToList();
        var desde=DateTime.Today.AddDays(-13);
        vm.UltimosDias=Enumerable.Range(0,14).Select(i=>desde.AddDays(i)).Select(d=>new EstadisticaItem{
            Etiqueta=d.ToString("dd/MM"),Cantidad=rents.Count(r=>r.CreadoEn.Date==d.Date) }).ToList();

        // Todos los parámetros se basan en conteos reales cuando existen registros.
        // En un catálogo vacío se muestra explícitamente un caso didáctico.
        bool demo=vm.TotalUsuarios==0 || vm.TotalLibros==0 || vm.TotalAlquileres==0;
        var pPlaylist=vm.TotalUsuarios>0 ? (double)vm.UsuariosConPlaylist/vm.TotalUsuarios : 0.5;
        switch(modelo)
        {
            case "geometrica":
                vm.TituloDistribucion="Distribución geométrica";
                vm.ContextoDistribucion="Probabilidad de obtener el primer usuario con playlist en el intento k. Supone ensayos independientes y probabilidad constante.";
                vm.Parametros=$"p = {pPlaylist:0.000} (usuarios con al menos una playlist / usuarios registrados)";
                vm.ParametrosDidacticos=vm.TotalUsuarios==0;
                for(int k=1;k<=14;k++)vm.Probabilidades.Add(new(){K=k,Probabilidad=Math.Pow(1-pPlaylist,k-1)*pPlaylist});
                vm.MediaTeorica=pPlaylist>0?1/pPlaylist:double.PositiveInfinity;
                vm.VarianzaTeorica=pPlaylist>0?(1-pPlaylist)/(pPlaylist*pPlaylist):double.PositiveInfinity;
                if(pPlaylist==0)vm.ContextoDistribucion+=" Con p=0 no se observa éxito; la esperanza no es finita.";
                break;
            case "hipergeometrica":
                vm.TituloDistribucion="Distribución hipergeométrica";
                vm.ContextoDistribucion="Al elegir n libros sin reemplazo, probabilidad de encontrar k de la categoría más representada.";
                var N=vm.TotalLibros>0?vm.TotalLibros:10;
                var K=vm.TotalLibros>0?vm.Categorias.First().Cantidad:3;
                var n=Math.Min(5,N);
                vm.Parametros=$"N = {N} libros · K = {K} en la categoría principal · n = {n} libros extraídos";
                vm.ParametrosDidacticos=vm.TotalLibros==0;
                for(int k=Math.Max(0,n-(N-K));k<=Math.Min(K,n);k++)
                    vm.Probabilidades.Add(new(){K=k,Probabilidad=Math.Exp(LogElegir(K,k)+LogElegir(N-K,n-k)-LogElegir(N,n))});
                vm.MediaTeorica=(double)n*K/N;
                vm.VarianzaTeorica=N>1?n*(K/(double)N)*(1-K/(double)N)*(N-n)/(double)(N-1):0;
                break;
            case "poisson":
                vm.TituloDistribucion="Distribución de Poisson";
                vm.ContextoDistribucion="Estimación de la cantidad de alquileres iniciados en un día suponiendo una tasa estable e independencia.";
                var recientes=rents.Count(r=>r.CreadoEn>=DateTime.Today.AddDays(-29));
                var lambda=vm.TotalAlquileres>0?recientes/30.0:2.5;
                vm.Parametros=$"λ = {lambda:0.000} alquileres/día (últimos 30 días)";
                vm.ParametrosDidacticos=vm.TotalAlquileres==0;
                for(int k=0;k<=Math.Max(10,(int)Math.Ceiling(lambda+4*Math.Sqrt(lambda)));k++)
                    vm.Probabilidades.Add(new(){K=k,Probabilidad=(lambda==0 ? (k==0?1:0) : Math.Exp(-lambda+k*Math.Log(lambda)-LogFactorial(k)))});
                vm.MediaTeorica=lambda;vm.VarianzaTeorica=lambda;
                break;
            default:
                vm.TituloDistribucion="Distribución binomial";
                vm.ContextoDistribucion="En 10 usuarios seleccionados, probabilidad de que k tengan al menos una playlist. Supone independencia y misma p.";
                const int ensayos=10;
                vm.Parametros=$"n = {ensayos} · p = {pPlaylist:0.000} (usuarios con playlist / total)";
                vm.ParametrosDidacticos=vm.TotalUsuarios==0;
                for(int k=0;k<=ensayos;k++)
                {
                    var prob= pPlaylist==0 ? (k==0?1:0) : pPlaylist==1 ? (k==ensayos?1:0) :
                        Math.Exp(LogElegir(ensayos,k)+k*Math.Log(pPlaylist)+(ensayos-k)*Math.Log(1-pPlaylist));
                    vm.Probabilidades.Add(new(){K=k,Probabilidad=prob});
                }
                vm.MediaTeorica=ensayos*pPlaylist;
                vm.VarianzaTeorica=ensayos*pPlaylist*(1-pPlaylist);
                break;
        }
        return View(vm);
    }
    private static double LogFactorial(int n){double v=0;for(int i=2;i<=n;i++)v+=Math.Log(i);return v;}
    private static double LogElegir(int n,int k){if(k<0||k>n)return double.NegativeInfinity;return LogFactorial(n)-LogFactorial(k)-LogFactorial(n-k);}
}
