using GestorTareas.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace GestorTareas.Data;
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services,IConfiguration config)
    {
        var db=services.GetRequiredService<AppDbContext>();
        // No EnsureCreated: bibliobeat_diagrama YA EXISTE. Aplicar SQL de actualización manualmente.
        await SchemaUpgrade.VerifyAsync(db);
        var roles=services.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach(var role in new[]{"Admin","Usuario"})
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole<int>(role));
        var email=config["AdminBootstrap:Email"];var password=config["AdminBootstrap:Password"];
        if (!string.IsNullOrWhiteSpace(email)&&!string.IsNullOrWhiteSpace(password))
        {
            var manager=services.GetRequiredService<UserManager<AppUser>>();
            var user=await manager.FindByEmailAsync(email);
            if(user!=null && !await manager.IsInRoleAsync(user,"Admin"))
                throw new InvalidOperationException("Ese correo ya existe como usuario normal. Usa otro para AdminBootstrap.");
            if(user==null)
            {
                user=new AppUser { UserName=email,Email=email,Nombre="Administrador",Apellido="Sistema",FechaRegistro=DateTime.Today,Rol="Admin" };
                var created=await manager.CreateAsync(user,password);
                if(!created.Succeeded) throw new InvalidOperationException("AdminBootstrap: "+string.Join(", ",created.Errors.Select(e=>e.Description)));
            }
            if(!await manager.IsInRoleAsync(user,"Admin"))
            {
                var assigned=await manager.AddToRoleAsync(user,"Admin");
                if(!assigned.Succeeded) throw new InvalidOperationException("No se pudo asignar el rol Admin.");
            }
            if(user.Rol!="Admin") {user.Rol="Admin"; await manager.UpdateAsync(user);}
        }
        // Semillas solo si no existen. Nunca borrar datos de la base importada.
        async Task AddBook(string title,string author,string genre,string desc,decimal price,string? file=null,string? rights=null)
        {
            if(await db.Libros.AnyAsync(x=>x.Titulo==title))return;
            var keys=await CatalogoHelper.LibroClavesAsync(db,author,genre);
            db.Libros.Add(new LibroRenta{Titulo=title,Autor=author,Genero=genre,Descripcion=desc,PrecioRenta=price,
                RutaLectura=file,Derechos=rights,AutorId=keys.AutorId,CategoriaId=keys.CategoriaId,Estado="Disponible"});
            await db.SaveChangesAsync();
        }
        if(!await db.Libros.AnyAsync())
        {
            await AddBook("Don Quijote de la Mancha","Miguel de Cervantes","Clásico","Un caballero idealista y su fiel compañero.",0);
            await AddBook("1984","George Orwell","Distopía","Novela sobre vigilancia y libertad. Ficha descriptiva, sin alquiler digital.",0);
        }
        // Fichas bibliográficas de los tres PDF recibidos: no reproducimos ni alojamos
        // traducciones/ediciones comerciales sin permiso comprobado.
        await AddBook("Noches blancas", "Fiódor Dostoyevski", "Novela corta",
            "Ficha de la obra (1848). Para activar el PDF de una edición concreta, verificar también los derechos de esa traducción.", 0);
        await AddBook("Las penas del joven Werther", "Johann Wolfgang von Goethe", "Clásico",
            "Ficha de la obra de 1774. El PDF recibido contiene una traducción contemporánea que requiere autorización.", 0);
        await AddBook("Flipped", "Wendelin Van Draanen", "Novela juvenil",
            "Ficha bibliográfica. El PDF recibido no se distribuye con BiblioBeat; requiere permiso del titular.", 0);
        await AddBook("Relatos de otoño — BiblioBeat","BiblioBeat (obra original)","Relatos",
            "Relato original. El acceso digital se habilita únicamente cuando el proveedor confirma un pago QR auténtico.",
            8m,"relatos-de-otono.txt","Texto original de BiblioBeat");
        // PDF original incluido: muestra comprobable del nuevo lector interno.
        if(!await db.Libros.AnyAsync(x=>x.Titulo=="Relatos de otoño — PDF gratuito"))
        {
            var claves=await CatalogoHelper.LibroClavesAsync(db,"BiblioBeat (obra original)","Relatos");
            var demoPdf=new LibroRenta{Titulo="Relatos de otoño — PDF gratuito",Autor="BiblioBeat (obra original)",Genero="Relatos",
                Descripcion="Edición PDF de muestra para probar el lector; texto original del proyecto.",
                PrecioRenta=0,Estado="Disponible",Derechos="Obra original de demostración BiblioBeat",
                ArchivoPdf="8d7342ee14cad71de5a11abda3ab6d44.pdf",AutorId=claves.AutorId,CategoriaId=claves.CategoriaId,Activo=true};
            db.Libros.Add(demoPdf);await db.SaveChangesAsync();
            demoPdf.UrlLectura=$"/Libros/ArchivoPdf/{demoPdf.Id}";await db.SaveChangesAsync();
        }
                async Task AddSong(string titulo,string genero,string archivo)
        {
            if(await db.Canciones.AnyAsync(x=>x.Titulo==titulo&&x.Fuente=="Local"))return;
            var genId=await CatalogoHelper.GeneroIdAsync(db,genero);
            db.Canciones.Add(new Cancion{Titulo=titulo,Artista="BiblioBeat Original",Genero=genero,
                GeneroId=genId,AudioUrl=archivo,Fuente="Local"});await db.SaveChangesAsync();
        }
        await AddSong("Hojas al viento","Ambient","hojas-al-viento.wav");
        await AddSong("Tarde de lluvia","Lo-fi","tarde-de-lluvia.wav");
        await AddSong("Café de otoño","Piano","cafe-de-otono.wav");
        await AddSong("Páginas de ámbar","Ambient","paginas-de-ambar.wav");
    }
}
