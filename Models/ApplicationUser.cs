using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grupo_negro.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string Nombres { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? DNI { get; set; }

        [MaxLength(20)]
        public string? Celular { get; set; }

        [MaxLength(200)]
        public string? Negocio { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Saldo { get; set; } = 0.00m;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Relación con bonos
        public ICollection<BonoUsuario> Bonos { get; set; } = new List<BonoUsuario>();

        // Relación con transacciones
        public ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();

        public string NombreCompleto => $"{Nombres} {Apellidos}";
    }
}