using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Grupo_negro.Models;

namespace Grupo_negro.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets para el sistema de apuestas deportivas
        public DbSet<Liga> Ligas { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Partido> Partidos { get; set; }
        public DbSet<Apuesta> Apuestas { get; set; }
        public DbSet<ApuestaCombinada> ApuestasCombinadas { get; set; }
        public DbSet<DetalleApuestaCombinada> DetallesApuestasCombinadas { get; set; }
        public DbSet<Comentario> Comentarios { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuraciones para el sistema de apuestas
            builder.Entity<Partido>(entity =>
            {
                entity.HasOne(p => p.EquipoLocal)
                    .WithMany(e => e.PartidosLocal)
                    .HasForeignKey(p => p.EquipoLocalId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.EquipoVisitante)
                    .WithMany(e => e.PartidosVisitante)
                    .HasForeignKey(p => p.EquipoVisitanteId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Liga)
                    .WithMany(l => l.Partidos)
                    .HasForeignKey(p => p.LigaId);
            });

            builder.Entity<Equipo>(entity =>
            {
                entity.HasOne(e => e.Liga)
                    .WithMany(l => l.Equipos)
                    .HasForeignKey(e => e.LigaId);
            });

            builder.Entity<Apuesta>(entity =>
            {
                entity.HasOne(a => a.Partido)
                    .WithMany(p => p.Apuestas)
                    .HasForeignKey(a => a.PartidoId);
            });

            // Configuración para apuestas combinadas
            builder.Entity<ApuestaCombinada>(entity =>
            {
                entity.HasOne(ac => ac.Usuario)
                    .WithMany()
                    .HasForeignKey(ac => ac.UsuarioId);
            });

            builder.Entity<DetalleApuestaCombinada>(entity =>
            {
                entity.HasOne(dac => dac.ApuestaCombinada)
                    .WithMany(ac => ac.Detalles)
                    .HasForeignKey(dac => dac.ApuestaCombinadadId);

                entity.HasOne(dac => dac.Partido)
                    .WithMany()
                    .HasForeignKey(dac => dac.PartidoId);
            });

            // Configuración para Comentario
            builder.Entity<Comentario>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Contenido).HasMaxLength(1000).IsRequired();
                entity.Property(c => c.FechaCreacion).IsRequired();
                
                // Relación con Usuario
                entity.HasOne(c => c.Usuario)
                      .WithMany()
                      .HasForeignKey(c => c.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade);
                      
                // Relación auto-referencial para respuestas
                entity.HasOne(c => c.ComentarioPadre)
                      .WithMany(c => c.Respuestas)
                      .HasForeignKey(c => c.ComentarioPadreId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}