using System.Data;
using Microsoft.EntityFrameworkCore;
namespace GestorTareas.Data;
public static class SchemaUpgrade
{
    public static async Task VerifyAsync(AppDbContext db)
    {
        var con = db.Database.GetDbConnection();
        await con.OpenAsync();
        try
        {
            foreach (var (table,column) in new [] { ("usuarios","PasswordHash"),("libros","precio_renta"),
                     ("alquiler_libros","duracion_dias"),("libros","url_lectura"),("libros","archivo_pdf"),("canciones","fuente"),("alquiler_libros","creado_en"),("playlist_cancion","fecha_agregada") })
            {
                await using var cmd=con.CreateCommand();
                cmd.CommandText="SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@t AND COLUMN_NAME=@c";
                var t=cmd.CreateParameter();t.ParameterName="@t";t.Value=table;cmd.Parameters.Add(t);
                var c=cmd.CreateParameter();c.ParameterName="@c";c.Value=column;cmd.Parameters.Add(c);
                if (Convert.ToInt64(await cmd.ExecuteScalarAsync())==0)
                    throw new InvalidOperationException($"Falta {table}.{column}. Ejecuta database/01_ADAPTAR_BIBLIOBEAT.sql y database/03_BIBLIOBEAT_4_LIBROS.sql en bibliobeat_diagrama desde phpMyAdmin antes de iniciar.");
            }
        }
        finally { await con.CloseAsync(); }
    }
}
