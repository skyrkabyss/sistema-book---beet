using System.Security.Claims;
using GestorTareas.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace GestorTareas.Controllers;
public class CuentaController : Controller
{
    private readonly UserManager<AppUser> _users;
    private readonly SignInManager<AppUser> _signIn;
    public CuentaController(UserManager<AppUser> users, SignInManager<AppUser> signIn)
    { _users = users; _signIn = signIn; }

    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginForm { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.FindByEmailAsync(model.Email.Trim());
        if (user != null)
        {
            var result = await _signIn.PasswordSignInAsync(user, model.Password, model.Recordarme, lockoutOnFailure: true);
            if (result.Succeeded) return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : Url.Action("Index", "Home")!);
            if (result.IsLockedOut) ModelState.AddModelError("", "Demasiados intentos. Prueba más tarde.");
            else ModelState.AddModelError("", "Correo o contraseña incorrectos.");
        }
        else ModelState.AddModelError("", "Correo o contraseña incorrectos.");
        return View(model);
    }

    [HttpGet, AllowAnonymous] public IActionResult Registro() => View(new RegistroForm());

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Registro(RegistroForm model)
    {
        if (!ModelState.IsValid) return View(model);
        var email = model.Email.Trim();
        var user = new AppUser { UserName = email, Email = email, Nombre = model.Nombre.Trim(), Apellido = model.Apellido.Trim(), FechaRegistro = DateTime.Today, Rol = "Usuario" };
        var result = await _users.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(model);
        }
        await _users.AddToRoleAsync(user, "Usuario");
        await _signIn.SignInAsync(user, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    { await _signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }

    public IActionResult Denegado() => View();
}
