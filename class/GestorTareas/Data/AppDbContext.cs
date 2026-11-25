using GestorTareas.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace GestorTareas.Data;
public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<LibroRenta> Libros => Set<LibroRenta>();
    public DbSet<Alquiler> Alquileres => Set<Alquiler>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Cancion> Canciones => Set<Cancion>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistCancion> PlaylistCanciones => Set<PlaylistCancion>();
    public DbSet<GeneroMusica> GenerosMusica => Set<GeneroMusica>();
    public DbSet<CategoriaLibro> CategoriasLibro => Set<CategoriaLibro>();
    public DbSet<Editorial> Editoriales => Set<Editorial>();
    public DbSet<AutorLibro> Autores => Set<AutorLibro>();
    public DbSet<MeGusta> MeGusta => Set<MeGusta>();
    public DbSet<ComentarioLibro> Comentarios => Set<ComentarioLibro>();
    public DbSet<ValoracionLibro> Valoraciones => Set<ValoracionLibro>();
    public DbSet<ReservaLibro> Reservas => Set<ReservaLibro>();
    public DbSet<LibroReserva> LibrosReservas => Set<LibroReserva>();
    public DbSet<Tarea> Tareas => Set<Tarea>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);
        // Identity: misma PK que usuarios.id; las FK del diagrama funcionan sin duplicar cuentas.
        m.Entity<AppUser>(e => {
            e.ToTable("usuarios");
            e.Property(x=>x.Id).HasColumnName("id");
            e.Property(x=>x.Nombre).HasColumnName("nombre").HasMaxLength(50).IsRequired();
            e.Property(x=>x.Apellido).HasColumnName("apellido").HasMaxLength(50).IsRequired();
            e.Property(x=>x.Email).HasColumnName("correo").HasMaxLength(100).IsRequired();
            e.Property(x=>x.PhoneNumber).HasColumnName("telefono").HasMaxLength(20);
            e.Property(x=>x.FechaRegistro).HasColumnName("fecha_registro").HasColumnType("date");
            e.Property(x=>x.Rol).HasColumnName("rol").HasMaxLength(20);
            e.HasIndex(x=>x.Email).IsUnique();
        });
        // Tablas de credenciales y roles de ASP.NET Core (auxiliares, no reemplazan usuarios).
        m.Entity<IdentityRole<int>>().ToTable("AspNetRoles");
        m.Entity<IdentityUserRole<int>>().ToTable("AspNetUserRoles");
        m.Entity<IdentityUserClaim<int>>().ToTable("AspNetUserClaims");
        m.Entity<IdentityUserLogin<int>>().ToTable("AspNetUserLogins");
        m.Entity<IdentityUserToken<int>>().ToTable("AspNetUserTokens");
        m.Entity<IdentityRoleClaim<int>>().ToTable("AspNetRoleClaims");

        m.Entity<GeneroMusica>(e=>{e.ToTable("generos_musica");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Nombre).HasColumnName("nombre");e.Property(x=>x.Descripcion).HasColumnName("descripcion").HasColumnType("text");});
        m.Entity<CategoriaLibro>(e=>{e.ToTable("categorias_libro");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Nombre).HasColumnName("nombre");e.Property(x=>x.Descripcion).HasColumnName("descripcion").HasColumnType("text");});
        m.Entity<Editorial>(e=>{
            e.ToTable("editoriales");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Nombre).HasColumnName("nombre");
            e.Property(x=>x.Pais).HasColumnName("pais").HasMaxLength(45);
            e.Property(x=>x.Correo).HasColumnName("correo").HasMaxLength(100);
            e.Property(x=>x.Telefono).HasColumnName("telefono").HasMaxLength(20);
        });
        m.Entity<AutorLibro>(e=>{
            e.ToTable("autores");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Nombre).HasColumnName("nombre");e.Property(x=>x.Apellido).HasColumnName("apellido");
            e.Property(x=>x.Nacionalidad).HasColumnName("nacionalidad").HasMaxLength(45);
            e.Property(x=>x.FechaNacimiento).HasColumnName("fecha_nacimiento").HasColumnType("date");
            e.Property(x=>x.EditorialId).HasColumnName("editorial_id");
            e.HasOne(x=>x.Editorial).WithMany().HasForeignKey(x=>x.EditorialId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<LibroRenta>(e=>{
            e.ToTable("libros");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Titulo).HasColumnName("titulo");
            e.Property(x=>x.Descripcion).HasColumnName("descripcion").HasColumnType("text");
            e.Property(x=>x.AnioPublicacion).HasColumnName("anio_publicacion");e.Property(x=>x.Estado).HasColumnName("estado");
            e.Property(x=>x.CategoriaId).HasColumnName("categoria_id");e.Property(x=>x.AutorId).HasColumnName("autor_id");
            e.Property(x=>x.Autor).HasColumnName("autor");e.Property(x=>x.Genero).HasColumnName("genero");
            e.Property(x=>x.PrecioRenta).HasColumnName("precio_renta").HasColumnType("decimal(10,2)");
            e.Property(x=>x.RutaLectura).HasColumnName("ruta_lectura");
            e.Property(x=>x.UrlLectura).HasColumnName("url_lectura").HasMaxLength(2048);
            e.Property(x=>x.ArchivoPdf).HasColumnName("archivo_pdf").HasMaxLength(160);
            e.Property(x=>x.Derechos).HasColumnName("derechos");
            e.Property(x=>x.Activo).HasColumnName("activo");
            e.HasOne(x=>x.Categoria).WithMany().HasForeignKey(x=>x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.AutorRegistro).WithMany().HasForeignKey(x=>x.AutorId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<Cancion>(e=>{
            e.ToTable("canciones");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Titulo).HasColumnName("titulo");
            e.Property(x=>x.Duracion).HasColumnName("duracion").HasColumnType("time");
            e.Property(x=>x.AnioLanzamiento).HasColumnName("anio_lanzamiento");
            e.Property(x=>x.Letra).HasColumnName("letra").HasColumnType("text");
            e.Property(x=>x.GeneroId).HasColumnName("genero_id");
            e.Property(x=>x.Artista).HasColumnName("artista");e.Property(x=>x.Genero).HasColumnName("genero");
            e.Property(x=>x.AudioUrl).HasColumnName("audio_url");e.Property(x=>x.Fuente).HasColumnName("fuente");
            e.Property(x=>x.IdExterno).HasColumnName("id_externo");e.Property(x=>x.EnlaceOrigen).HasColumnName("enlace_origen");
            e.Property(x=>x.Licencia).HasColumnName("licencia");e.Property(x=>x.Activa).HasColumnName("activa");
            e.HasOne(x=>x.GeneroRegistro).WithMany().HasForeignKey(x=>x.GeneroId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<Playlist>(e=>{
            e.ToTable("playlist");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Nombre).HasColumnName("nombre");
            e.Property(x=>x.Descripcion).HasColumnName("descripcion").HasColumnType("text");
            e.Property(x=>x.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("date");
            e.Property(x=>x.Privacidad).HasColumnName("privacidad");e.Property(x=>x.UsuarioId).HasColumnName("usuario_id");
            e.HasOne(x=>x.Usuario).WithMany(x=>x.Playlists).HasForeignKey(x=>x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });
        m.Entity<PlaylistCancion>(e=>{
            e.ToTable("playlist_cancion");e.HasKey(x=>x.Id);
            e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Orden).HasColumnName("orden");
            e.Property(x=>x.FechaAgregada).HasColumnName("fecha_agregada").HasColumnType("date");
            e.Property(x=>x.PlaylistId).HasColumnName("playlist_id");e.Property(x=>x.CancionId).HasColumnName("cancion_id");
            e.HasOne(x=>x.Playlist).WithMany(x=>x.Canciones).HasForeignKey(x=>x.PlaylistId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x=>x.Cancion).WithMany(x=>x.EnPlaylists).HasForeignKey(x=>x.CancionId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<MeGusta>(e=>{
            e.ToTable("me_gusta");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Fecha).HasColumnName("fecha");
            e.Property(x=>x.UsuarioId).HasColumnName("usuario_id");e.Property(x=>x.CancionId).HasColumnName("cancion_id");
            e.HasOne(x=>x.Usuario).WithMany().HasForeignKey(x=>x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x=>x.Cancion).WithMany().HasForeignKey(x=>x.CancionId).OnDelete(DeleteBehavior.Cascade);
        });
        m.Entity<Alquiler>(e=>{
            e.ToTable("alquiler_libros");e.Property(x=>x.Id).HasColumnName("id");
            e.Property(x=>x.FechaAlquilada).HasColumnName("fecha_alquilada").HasColumnType("date");
            e.Property(x=>x.FechaDevolucion).HasColumnName("fecha_devolucion").HasColumnType("date");
            e.Property(x=>x.Estado).HasColumnName("estado");e.Property(x=>x.UsuarioId).HasColumnName("usuario_id");
            e.Property(x=>x.LibroId).HasColumnName("libro_id");e.Property(x=>x.CreadoEn).HasColumnName("creado_en");
            e.Property(x=>x.InicioEn).HasColumnName("inicio_en");e.Property(x=>x.VenceEn).HasColumnName("vence_en");
            e.Property(x=>x.DuracionDias).HasColumnName("duracion_dias").HasDefaultValue(7);
            e.HasOne(x=>x.Usuario).WithMany(x=>x.Alquileres).HasForeignKey(x=>x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x=>x.Libro).WithMany(x=>x.Alquileres).HasForeignKey(x=>x.LibroId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<Pago>(e=>{
            e.ToTable("pagos");e.HasKey(x=>x.Id);
            e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.AlquilerId).HasColumnName("alquiler_id");
            e.Property(x=>x.Metodo).HasColumnName("metodo");e.Property(x=>x.Estado).HasColumnName("estado");
            e.Property(x=>x.Referencia).HasColumnName("referencia");e.Property(x=>x.Monto).HasColumnName("monto").HasColumnType("decimal(10,2)");
            e.Property(x=>x.Proveedor).HasColumnName("proveedor");e.Property(x=>x.TransaccionExterna).HasColumnName("transaccion_externa");
            e.Property(x=>x.QrId).HasColumnName("qr_id");e.Property(x=>x.QrImagenBase64).HasColumnName("qr_imagen_base64").HasColumnType("longtext");
            e.Property(x=>x.QrVenceEn).HasColumnName("qr_vence_en");e.Property(x=>x.Fecha).HasColumnName("fecha");
            e.Property(x=>x.VerificadoEn).HasColumnName("verificado_en");
            e.HasOne(x=>x.Alquiler).WithOne(x=>x.Pago).HasForeignKey<Pago>(x=>x.AlquilerId).OnDelete(DeleteBehavior.Cascade);
        });
        m.Entity<ComentarioLibro>(e=>{
            e.ToTable("comentarios");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Texto).HasColumnName("texto").HasColumnType("text");
            e.Property(x=>x.Fecha).HasColumnName("fecha");e.Property(x=>x.UsuarioId).HasColumnName("usuario_id");e.Property(x=>x.LibroId).HasColumnName("libro_id");
            e.HasOne(x=>x.Usuario).WithMany().HasForeignKey(x=>x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x=>x.Libro).WithMany().HasForeignKey(x=>x.LibroId).OnDelete(DeleteBehavior.Cascade);
        });
        m.Entity<ValoracionLibro>(e=>{
            e.ToTable("valoraciones");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.Puntuacion).HasColumnName("puntuacion");
            e.Property(x=>x.Comentario).HasColumnName("comentario").HasColumnType("text");
            e.Property(x=>x.Fecha).HasColumnName("fecha").HasColumnType("date");
            e.Property(x=>x.UsuarioId).HasColumnName("usuario_id");e.Property(x=>x.LibroId).HasColumnName("libro_id");
            e.HasOne(x=>x.Usuario).WithMany().HasForeignKey(x=>x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x=>x.Libro).WithMany().HasForeignKey(x=>x.LibroId).OnDelete(DeleteBehavior.Cascade);
        });
        m.Entity<ReservaLibro>(e=>{
            e.ToTable("reservas");e.Property(x=>x.Id).HasColumnName("id");e.Property(x=>x.FechaReserva).HasColumnName("fecha_reserva").HasColumnType("date");
            e.Property(x=>x.UsuarioId).HasColumnName("usuario_id");
            e.HasOne(x=>x.Usuario).WithMany().HasForeignKey(x=>x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<LibroReserva>(e=>{
            e.ToTable("libros_has_reservas");e.HasKey(x=>new{x.LibroId,x.ReservaId});
            e.Property(x=>x.LibroId).HasColumnName("libro_id");e.Property(x=>x.ReservaId).HasColumnName("reserva_id");
            e.HasOne(x=>x.Libro).WithMany().HasForeignKey(x=>x.LibroId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x=>x.Reserva).WithMany(x=>x.Libros).HasForeignKey(x=>x.ReservaId).OnDelete(DeleteBehavior.Cascade);
        });
        m.Entity<Tarea>().ToTable("Tareas");
    }
}
