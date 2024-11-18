using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Evento> Eventos { get; set; }
    public DbSet<EventoUsuario> EventoUsuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Configuración de la relación muchos a muchos
    modelBuilder.Entity<EventoUsuario>()
        .HasKey(eu => new { eu.EventoId, eu.ApplicationUserId }); // Clave compuesta

    modelBuilder.Entity<EventoUsuario>()
        .HasOne(eu => eu.Evento)
        .WithMany(e => e.Participantes)
        .HasForeignKey(eu => eu.EventoId);

    modelBuilder.Entity<EventoUsuario>()
        .HasOne(eu => eu.ApplicationUser)
        .WithMany(au => au.Eventos)
        .HasForeignKey(eu => eu.ApplicationUserId);
}

}
