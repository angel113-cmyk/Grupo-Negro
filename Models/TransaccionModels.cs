using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Grupo_negro.Models
{
    public enum TipoTransaccion
    {
        Deposito,
        Retiro,
        Apuesta,
        Ganancia,
        Bono
    }

    public enum MetodoPago
    {
        PayPal,
        MercadoPago,
        Yape,
        Plin,
        Efectivo,
        Transferencia
    }

    public enum EstadoTransaccion
    {
        Pendiente,
        Completada,
        Fallida,
        Cancelada
    }

    public class Transaccion
    {
        public int Id { get; set; }

        [Required]
        public string UsuarioId { get; set; } = string.Empty;
        public ApplicationUser Usuario { get; set; } = null!;

        [Required]
        public TipoTransaccion Tipo { get; set; }

        [Required]
        public MetodoPago MetodoPago { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        [Required]
        public EstadoTransaccion Estado { get; set; } = EstadoTransaccion.Pendiente;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [StringLength(100)]
        public string? ReferenciaPago { get; set; } // Para PayPal, Yape, Plin

        [StringLength(20)]
        public string? NumeroOperacion { get; set; } // Para Yape/Plin

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaCompletado { get; set; }

        // Para PayPal
        [StringLength(100)]
        public string? PayPalOrderId { get; set; }
        
        [StringLength(100)]
        public string? PayPalPaymentId { get; set; }

        // Para MercadoPago
        [StringLength(100)]
        public string? MercadoPagoPreferenceId { get; set; }
        
        [StringLength(100)]
        public string? MercadoPagoPaymentId { get; set; }
        
        [StringLength(100)]
        public string? MercadoPagoCollectionId { get; set; }
        
        [StringLength(50)]
        public string? MercadoPagoStatus { get; set; }

        // Relación con apuesta (opcional)
        public int? ApuestaId { get; set; }
        public Apuesta? Apuesta { get; set; }
    }

    // ViewModels para pagos
    public class DepositoViewModel
    {
        [Required]
        [Range(10, 1000, ErrorMessage = "El monto debe estar entre S/.10 y S/.1000")]
        public decimal Monto { get; set; }

        [Required]
        public MetodoPago MetodoPago { get; set; }

        // Para Yape/Plin
        [StringLength(20)]
        public string? NumeroOperacion { get; set; }

        [StringLength(500)]
        public string? Descripcion { get; set; }
    }

    public class RetiroViewModel
    {
        [Required]
        [Range(20, 500, ErrorMessage = "El monto debe estar entre S/.20 y S/.500")]
        public decimal Monto { get; set; }

        [Required]
        public MetodoPago MetodoPago { get; set; }

        [Required]
        [Display(Name = "Método de Retiro")]
        public string MetodoRetiro { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Información de Cuenta")]
        [StringLength(200)]
        public string InformacionCuenta { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CuentaDestino { get; set; } // Email PayPal o teléfono Yape/Plin

        [StringLength(200)]
        [Display(Name = "Concepto")]
        public string Concepto { get; set; } = "Retiro de fondos";

        [StringLength(500)]
        public string? Descripcion { get; set; }
    }

    public class PayPalSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Environment { get; set; } = "sandbox"; // sandbox o live
        public string BaseUrl => Environment == "sandbox" 
            ? "https://api.sandbox.paypal.com" 
            : "https://api.paypal.com";
    }

    public class MercadoPagoSettings
    {
        public string AccessToken { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string Environment { get; set; } = "sandbox"; // sandbox o production
        public string WebhookUrl { get; set; } = string.Empty;
        public string BaseUrl => Environment == "sandbox" 
            ? "https://api.mercadopago.com" 
            : "https://api.mercadopago.com";
    }
}