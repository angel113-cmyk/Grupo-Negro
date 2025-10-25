using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grupo_negro.Models
{
    public enum EstadoPartido
    {
        Programado,
        EnJuego,
        Finalizado,
        Suspendido
    }

    public class Partido
    {
        public int Id { get; set; }

        [Required]
        public DateTime FechaHora { get; set; }

        public EstadoPartido Estado { get; set; } = EstadoPartido.Programado;

        // Equipos
        public int EquipoLocalId { get; set; }
        public Equipo EquipoLocal { get; set; } = null!;

        public int EquipoVisitanteId { get; set; }
        public Equipo EquipoVisitante { get; set; } = null!;

        // Resultados
        public int? GolesLocal { get; set; }
        public int? GolesVisitante { get; set; }

        // Liga
        public int LigaId { get; set; }
        public Liga Liga { get; set; } = null!;

        // Cuotas para apuestas
        [Column(TypeName = "decimal(4,2)")]
        public decimal CuotaLocal { get; set; } = 1.50m;

        [Column(TypeName = "decimal(4,2)")]
        public decimal CuotaEmpate { get; set; } = 3.00m;

        [Column(TypeName = "decimal(4,2)")]
        public decimal CuotaVisitante { get; set; } = 2.25m;

        [StringLength(20)]
        public string? Jornada { get; set; }

        // Resultado del partido (para ML)
        [StringLength(50)]
        public string? Resultado { get; set; }

        // Relaciones
        public ICollection<Apuesta> Apuestas { get; set; } = new List<Apuesta>();
    }
}