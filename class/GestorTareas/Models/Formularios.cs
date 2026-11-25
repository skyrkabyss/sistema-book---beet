using System.ComponentModel.DataAnnotations;
namespace GestorTareas.Models;
public class LoginForm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool Recordarme { get; set; }
    public string? ReturnUrl { get; set; }
}
public class RegistroForm
{
    [Required, StringLength(50)] public string Nombre { get; set; } = "";
    [Required, StringLength(50)] public string Apellido { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password)), DataType(DataType.Password)] public string ConfirmarPassword { get; set; } = "";
}
public class AlquilerForm
{
    [Range(1, int.MaxValue)] public int LibroId { get; set; }
    [Required, RegularExpression("^(QR|Tarjeta)$")] public string Metodo { get; set; } = "QR";
    [StringLength(100)] public string? Referencia { get; set; }
    [Range(1, 30)] public int DuracionDias { get; set; } = 7;
}
public class PlaylistForm
{
    [Required, StringLength(100, MinimumLength=1)] public string Nombre { get; set; } = "";
}
