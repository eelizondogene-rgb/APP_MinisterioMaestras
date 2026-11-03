using Microsoft.EntityFrameworkCore;
using NinosParaCristo.Domain.Entities;

namespace NinosParaCristo.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Nino> Ninos => Set<Nino>();
    public DbSet<Encargado> Encargados => Set<Encargado>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();
    public DbSet<TurnoClase> TurnosClase => Set<TurnoClase>();
    public DbSet<GrupoClase> GruposClase => Set<GrupoClase>();
    public DbSet<AsignacionClaseMensual> AsignacionesClaseMensuales => Set<AsignacionClaseMensual>();
    public DbSet<ProtocoloSemana> Protocolos => Set<ProtocoloSemana>();
    public DbSet<InventarioItem> InventarioItems => Set<InventarioItem>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<Notificacion> Notificaciones => Set<Notificacion>();
    public DbSet<PushSuscripcion> PushSuscripciones => Set<PushSuscripcion>();
    public DbSet<ComunicacionPadre> Comunicaciones => Set<ComunicacionPadre>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Nino>(e =>
        {
            e.ToTable("Ninos");
            e.HasKey(x => x.Id);
            e.Property(x => x.NombreCompleto).IsRequired().HasMaxLength(150);
            e.Property(x => x.Alergias).HasMaxLength(500);
            e.Property(x => x.Comentarios).HasMaxLength(1000);
            e.Property(x => x.Activo).HasDefaultValue(true);
        });

        modelBuilder.Entity<Encargado>(e =>
        {
            e.ToTable("Encargados");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(150);
            e.Property(x => x.Telefono).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Parentesco).HasMaxLength(80);
            e.HasOne(x => x.Nino).WithMany(x => x.Encargados).HasForeignKey(x => x.NinoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasKey(x => x.Id);
            e.Property(x => x.NombreCompleto).IsRequired().HasMaxLength(150);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Rol).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Asistencia>(e =>
        {
            e.ToTable("Asistencias");
            e.HasKey(x => x.Id);
            e.Property(x => x.Observacion).HasMaxLength(500);
            e.HasIndex(x => new { x.NinoId, x.Fecha }).IsUnique();
            e.HasOne(x => x.Nino).WithMany(x => x.Asistencias).HasForeignKey(x => x.NinoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.RegistradaPorUsuario).WithMany().HasForeignKey(x => x.RegistradaPorUsuarioId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TurnoClase>(e =>
        {
            e.ToTable("TurnosClase");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tema).HasMaxLength(200);
            e.Property(x => x.MaterialUrl).HasMaxLength(1000);
            e.Property(x => x.Observaciones).HasMaxLength(1000);
            e.HasOne(x => x.Maestra).WithMany().HasForeignKey(x => x.MaestraId).OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<GrupoClase>(e =>
        {
            e.ToTable("GruposClase");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(120);
            e.HasIndex(x => x.Nombre).IsUnique();
        });

        modelBuilder.Entity<AsignacionClaseMensual>(e =>
        {
            e.ToTable("AsignacionesClaseMensuales");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tema).HasMaxLength(200);
            e.Property(x => x.MaterialUrl).HasMaxLength(1000);
            e.Property(x => x.Observaciones).HasMaxLength(1000);
            e.HasIndex(x => new { x.Anio, x.Mes, x.GrupoClaseId, x.MaestraId }).IsUnique();
            e.HasOne(x => x.GrupoClase).WithMany(x => x.Asignaciones).HasForeignKey(x => x.GrupoClaseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Maestra).WithMany().HasForeignKey(x => x.MaestraId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProtocoloSemana>(e =>
        {
            e.ToTable("Protocolos");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Fecha).IsUnique();
            e.Property(x => x.Merienda).HasMaxLength(300);
            e.Property(x => x.Tareas).HasMaxLength(1000);
            e.Property(x => x.Bitacora).HasMaxLength(2000);
            e.HasOne(x => x.ResponsableMerienda).WithMany().HasForeignKey(x => x.ResponsableMeriendaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.ResponsableProtocolo).WithMany().HasForeignKey(x => x.ResponsableProtocoloId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InventarioItem>(e =>
        {
            e.ToTable("InventarioItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Nombre).IsRequired().HasMaxLength(150);
            e.Property(x => x.Descripcion).HasMaxLength(500);
            e.Property(x => x.Unidad).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<MovimientoInventario>(e =>
        {
            e.ToTable("MovimientosInventario");
            e.HasKey(x => x.Id);
            e.Property(x => x.Tipo).IsRequired().HasMaxLength(20);
            e.Property(x => x.Nota).HasMaxLength(500);
            e.HasOne(x => x.InventarioItem).WithMany().HasForeignKey(x => x.InventarioItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Notificacion>(e =>
        {
            e.ToTable("Notificaciones");
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).IsRequired().HasMaxLength(200);
            e.Property(x => x.Mensaje).IsRequired().HasMaxLength(1000);
            e.Property(x => x.Tipo).IsRequired().HasMaxLength(50);
            e.Property(x => x.ClaveUnica).HasMaxLength(200);
            e.HasIndex(x => x.ClaveUnica).IsUnique();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PushSuscripcion>(e =>
        {
            e.ToTable("PushSuscripciones");
            e.HasKey(x => x.Id);
            e.Property(x => x.Endpoint).IsRequired().HasMaxLength(2000);
            e.Property(x => x.P256dh).IsRequired().HasMaxLength(500);
            e.Property(x => x.Auth).IsRequired().HasMaxLength(500);
            e.Property(x => x.UserAgent).HasMaxLength(1000);
            e.HasIndex(x => x.Endpoint).IsUnique();
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComunicacionPadre>(e =>
        {
            e.ToTable("Comunicaciones");
            e.HasKey(x => x.Id);
            e.Property(x => x.Asunto).IsRequired().HasMaxLength(200);
            e.Property(x => x.Mensaje).IsRequired().HasMaxLength(2000);
            e.Property(x => x.Canal).IsRequired().HasMaxLength(50);
            e.Property(x => x.Destinatario).HasMaxLength(200);
            e.Property(x => x.Estado).IsRequired().HasMaxLength(50);
            e.HasOne(x => x.Nino).WithMany().HasForeignKey(x => x.NinoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Encargado).WithMany().HasForeignKey(x => x.EncargadoId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
        });
    }
}
