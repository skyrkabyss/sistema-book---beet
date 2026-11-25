using System.Diagnostics;
using GestorTareas.Data;
using GestorTareas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace GestorTareas.Controllers;
public class HomeController : Controller
{
    private readonly AppDbContext _db;
    public HomeController(AppDbContext db) => _db=db;
    public async Task<IActionResult> Index() => View(await _db.Libros.AsNoTracking().Where(x=>x.Activo).Take(4).ToListAsync());
    public IActionResult Privacy() => View();
    [ResponseCache(Duration=0, Location=ResponseCacheLocation.None, NoStore=true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId=Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
