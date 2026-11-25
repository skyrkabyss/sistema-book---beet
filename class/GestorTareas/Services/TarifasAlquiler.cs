namespace GestorTareas.Services;

// El precio del catálogo es la tarifa base para 7 días. Solo opciones permitidas.
public static class TarifasAlquiler
{
    public static readonly int[] OpcionesDias = { 1, 3, 7, 14, 30 };
    public static bool Valida(int dias) => OpcionesDias.Contains(dias);
    public static decimal Calcular(decimal precioSieteDias, int dias)
    {
        if (!Valida(dias) || precioSieteDias <= 0 || precioSieteDias > 1000m)
            throw new ArgumentOutOfRangeException(nameof(dias), "Tarifa o duración inválida.");
        // Precio bancario a centavos, redondeo comercial. No inventar descuento no autorizado.
        return Math.Max(0.01m, Math.Round(precioSieteDias * dias / 7m, 2, MidpointRounding.AwayFromZero));
    }
}
