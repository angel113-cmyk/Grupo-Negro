using System.ComponentModel.DataAnnotations;

namespace Grupo_negro.Models
{
    public class ApuestaCombinada
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        public decimal MontoApostado { get; set; }

        public decimal CuotaTotal { get; set; }

        public decimal PosibleGanancia { get; set; }

        public DateTime FechaApuesta { get; set; } = DateTime.Now;

        public EstadoApuesta Estado { get; set; } = EstadoApuesta.Activa;

        // Navegación
        public virtual ApplicationUser? Usuario { get; set; }
        public virtual List<DetalleApuestaCombinada> Detalles { get; set; } = new List<DetalleApuestaCombinada>();
    }

    public class DetalleApuestaCombinada
    {
        public int Id { get; set; }

        public int ApuestaCombinadadId { get; set; }

        public int PartidoId { get; set; }

        [Required]
        public TipoApuesta TipoApuesta { get; set; }

        public decimal CuotaSeleccionada { get; set; }

        public EstadoApuesta Estado { get; set; } = EstadoApuesta.Activa;

        // Navegación
        public virtual ApuestaCombinada? ApuestaCombinada { get; set; }
        public virtual Partido? Partido { get; set; }
    }

    public class CarritoApuesta
    {
        public int PartidoId { get; set; }
        public string NombrePartido { get; set; } = string.Empty;
        public TipoApuesta TipoApuesta { get; set; }
        public string DescripcionApuesta { get; set; } = string.Empty;
        public decimal Cuota { get; set; }
        public DateTime FechaPartido { get; set; }
        public string Liga { get; set; } = string.Empty;
    }

    public class ApuestaCombinadadViewModel
    {
        public List<CarritoApuesta> Selecciones { get; set; } = new List<CarritoApuesta>();
        public decimal MontoApostado { get; set; }
        public decimal CuotaTotal => Selecciones.Any() ? Selecciones.Select(s => s.Cuota).Aggregate((a, b) => a * b) : 0;
        public decimal PosibleGanancia => MontoApostado * CuotaTotal;
        public int CantidadSelecciones => Selecciones.Count;
        public bool PuedeApostar => Selecciones.Count >= 2 && MontoApostado > 0;
    }
}