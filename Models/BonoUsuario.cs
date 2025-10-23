using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grupo_negro.Models
{
    public class BonoUsuario
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [ForeignKey("UsuarioId")]
        public ApplicationUser Usuario { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Concepto { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        public DateTime FechaAsignacion { get; set; } = DateTime.Now;

        [Required]
        public string AsignadoPorId { get; set; } = string.Empty;

        [ForeignKey("AsignadoPorId")]
        public ApplicationUser AsignadoPor { get; set; } = null!;

        public bool Aplicado { get; set; } = false;
    }
}
