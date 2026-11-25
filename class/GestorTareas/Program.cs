using GestorTareas.Data;
using GestorTareas.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("catalogos", c => { c.Timeout = TimeSpan.FromSeconds(12); });
builder.Services.AddHttpClient("libros", c => { c.Timeout = TimeSpan.FromSeconds(25); });
builder.Services.AddHttpClient("wizpay", c => { c.BaseAddress = new Uri("https://api.wizpay.app/"); c.Timeout = TimeSpan.FromSeconds(20); });
builder.Services.AddScoped<GestorTareas.Services.MusicaOnlineService>();
builder.Services.AddScoped<GestorTareas.Services.LibrosOnlineService>();
builder.Services.AddScoped<GestorTareas.Services.WizPayService>();
builder.Services.AddScoped<GestorTareas.Services.PagoManualQrService>();
var cs = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configura ConnectionStrings:DefaultConnection para MySQL.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(cs, ServerVersion.AutoDetect(cs)));

builder.Services.AddIdentity<AppUser, IdentityRole<int>>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false; // DEMO local: confirmar email para producción.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/Login";
    options.AccessDeniedPath = "/Cuenta/Denegado";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Producción: Always sobre HTTPS.
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider, app.Configuration);
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
