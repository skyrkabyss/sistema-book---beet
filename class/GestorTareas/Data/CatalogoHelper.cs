using GestorTareas.Models;
using Microsoft.EntityFrameworkCore;
namespace GestorTareas.Data;
public static class CatalogoHelper
{
    public static async Task<int> GeneroIdAsync(AppDbContext db,string genero)
    {
        var nombre=string.IsNullOrWhiteSpace(genero)?"Sin clasificar":genero.Trim()[..Math.Min(genero.Trim().Length,45)];
        var existing=await db.GenerosMusica.FirstOrDefaultAsync(x=>x.Nombre==nombre);
        if (existing!=null) return existing.Id;
        var nuevo=new GeneroMusica{Nombre=nombre};db.GenerosMusica.Add(nuevo);await db.SaveChangesAsync();return nuevo.Id;
    }
    public static async Task<(int AutorId,int CategoriaId)> LibroClavesAsync(AppDbContext db,string autor,string genero)
    {
        var cat=string.IsNullOrWhiteSpace(genero)?"General":genero.Trim()[..Math.Min(genero.Trim().Length,45)];
        var category=await db.CategoriasLibro.FirstOrDefaultAsync(x=>x.Nombre==cat);
        if (category==null){category=new CategoriaLibro{Nombre=cat};db.CategoriasLibro.Add(category);await db.SaveChangesAsync();}
        var editorial=await db.Editoriales.FirstOrDefaultAsync(x=>x.Nombre=="Editorial no especificada");
        if(editorial==null){editorial=new Editorial{Nombre="Editorial no especificada"};db.Editoriales.Add(editorial);await db.SaveChangesAsync();}
        var parts=(autor??"").Trim().Split(' ',2,StringSplitOptions.RemoveEmptyEntries);
        var n=(parts.Length>0?parts[0]:"Desconocido"); n=n[..Math.Min(n.Length,45)];
        var a=(parts.Length>1?parts[1]:"Sin apellido"); a=a[..Math.Min(a.Length,45)];
        var record=await db.Autores.FirstOrDefaultAsync(x=>x.Nombre==n && x.Apellido==a);
        if (record==null){record=new AutorLibro{Nombre=n,Apellido=a,EditorialId=editorial.Id};db.Autores.Add(record);await db.SaveChangesAsync();}
        return (record.Id,category.Id);
    }
}
