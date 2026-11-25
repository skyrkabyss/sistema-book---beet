using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GestorTareas.Data;
using GestorTareas.Models;

namespace GestorTareas.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class TareasController : Controller
{
    private readonly AppDbContext _context;

    public TareasController(AppDbContext context)
    {
        _context = context;
    }

    // GET: Tareas (RF01, CA01)
    public async Task<IActionResult> Index()
    {
        var tareas = await _context.Tareas.AsNoTracking().ToListAsync();
        return View(tareas);
    }

    // GET: Tareas/Create (RF02)
    public IActionResult Create()
    {
        return View();
    }

    // POST: Tareas/Create (RF02, RF05, CA02, CA03, CA04, CA08, CA09, CA10)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Titulo,Descripcion,FechaLimite")] Tarea tarea)
    {
        if (ModelState.IsValid)
        {
            tarea.Completada = false;
            _context.Add(tarea);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(tarea);
    }

    // POST: Tareas/Completar/5 (RF03, CA05, CA08)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Completar(int id)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea != null)
        {
            tarea.Completada = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // POST: Tareas/Eliminar/5 (RF04, CA06, CA08)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea != null)
        {
            _context.Tareas.Remove(tarea);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
