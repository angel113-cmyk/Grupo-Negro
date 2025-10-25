using Grupo_negro.Models;
using Grupo_negro.Data;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace Grupo_negro.Services
{
    // Modelos para la API de MercadoPago
    public class MercadoPagoPreference
    {
        public string? Id { get; set; }
        public List<MercadoPagoItem> Items { get; set; } = new();
        public MercadoPagoPayer? Payer { get; set; }
        public MercadoPagoBackUrls? BackUrls { get; set; }
        public string? ExternalReference { get; set; }
        public string? NotificationUrl { get; set; }
        public string? AutoReturn { get; set; } = "approved";
        public DateTime? ExpirationDateTime { get; set; }
        public string? StatementDescriptor { get; set; }
    }

    public class MercadoPagoItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public string CurrencyId { get; set; } = "PEN";
    }

    public class MercadoPagoPayer
    {
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
    }

    public class MercadoPagoBackUrls
    {
        public string? Success { get; set; }
        public string? Failure { get; set; }
        public string? Pending { get; set; }
    }

    public class MercadoPagoPayment
    {
        public long? Id { get; set; }
        public string? Status { get; set; }
        public string? ExternalReference { get; set; }
        public decimal? TransactionAmount { get; set; }
    }

    public class MercadoPagoService
    {
        private readonly ApplicationDbContext _context;
        private readonly MercadoPagoSettings _settings;
        private readonly ILogger<MercadoPagoService> _logger;
        private readonly HttpClient _httpClient;

        public MercadoPagoService(
            ApplicationDbContext context, 
            IOptions<MercadoPagoSettings> settings,
            ILogger<MercadoPagoService> logger,
            HttpClient httpClient)
        {
            _context = context;
            _settings = settings.Value;
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Crea una preferencia de pago en MercadoPago para depósito
        /// </summary>
        public async Task<(bool Success, string? PreferenceId, string? ErrorMessage)> CrearPreferenciaPagoAsync(
            string usuarioId, decimal monto, string descripcion = "Depósito de saldo")
        {
            try
            {
                // Validar configuración - solo simular si no hay token
                if (string.IsNullOrEmpty(_settings.AccessToken))
                {
                    _logger.LogWarning("MercadoPago: Access Token no configurado, usando simulación");
                    return await CrearPreferenciaSimuladaAsync(usuarioId, monto, descripcion);
                }

                // Buscar usuario
                var usuario = await _context.Users.FindAsync(usuarioId);
                if (usuario == null)
                {
                    return (false, null, "Usuario no encontrado");
                }

                // Crear transacción pendiente
                var transaccion = new Transaccion
                {
                    UsuarioId = usuarioId,
                    Tipo = TipoTransaccion.Deposito,
                    MetodoPago = MetodoPago.MercadoPago,
                    Monto = monto,
                    Estado = EstadoTransaccion.Pendiente,
                    Descripcion = descripcion,
                    FechaCreacion = DateTime.Now
                };

                _context.Transacciones.Add(transaccion);
                await _context.SaveChangesAsync();

                // Crear preferencia usando HttpClient
                var preferenceRequest = new MercadoPagoPreference
                {
                    Items = new List<MercadoPagoItem>
                    {
                        new MercadoPagoItem
                        {
                            Title = "Depósito de Saldo - Grupo Negro",
                            Description = descripcion,
                            Quantity = 1,
                            CurrencyId = "PEN", // Soles peruanos
                            UnitPrice = monto
                        }
                    },
                    Payer = new MercadoPagoPayer
                    {
                        Email = usuario.Email,
                        Name = usuario.UserName, // Usar UserName ya que no hay Nombre
                        Surname = usuario.Email // Usar email como apellido por ahora
                    },
                    BackUrls = new MercadoPagoBackUrls
                    {
                        Success = $"{GetBaseUrl()}/Saldo/ConfirmarMercadoPago?transaccionId={transaccion.Id}&status=approved",
                        Failure = $"{GetBaseUrl()}/Saldo/ConfirmarMercadoPago?transaccionId={transaccion.Id}&status=failure",
                        Pending = $"{GetBaseUrl()}/Saldo/ConfirmarMercadoPago?transaccionId={transaccion.Id}&status=pending"
                    },
                    AutoReturn = "approved",
                    ExternalReference = transaccion.Id.ToString(),
                    StatementDescriptor = "GRUPO-NEGRO",
                    ExpirationDateTime = DateTime.Now.AddMinutes(30), // Expira en 30 minutos
                    NotificationUrl = $"{GetBaseUrl()}/api/webhooks/mercadopago"
                };

                // Configurar headers para MercadoPago
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken}");

                // Crear el objeto con la estructura exacta que espera MercadoPago
                var mercadopagoRequest = new
                {
                    items = new[]
                    {
                        new
                        {
                            title = "Depósito de Saldo - Grupo Negro",
                            description = descripcion,
                            quantity = 1,
                            currency_id = "PEN",
                            unit_price = monto
                        }
                    },
                    payer = new
                    {
                        email = usuario.Email,
                        name = usuario.UserName,
                        surname = usuario.Email
                    },
                    back_urls = new
                    {
                        success = $"{GetBaseUrl()}/Saldo/ConfirmarMercadoPago?transaccionId={transaccion.Id}&status=approved",
                        failure = $"{GetBaseUrl()}/Saldo/ConfirmarMercadoPago?transaccionId={transaccion.Id}&status=failure",
                        pending = $"{GetBaseUrl()}/Saldo/ConfirmarMercadoPago?transaccionId={transaccion.Id}&status=pending"
                    },
                    auto_return = "approved",
                    external_reference = transaccion.Id.ToString(),
                    statement_descriptor = "GRUPO-NEGRO",
                    notification_url = $"{GetBaseUrl()}/api/webhooks/mercadopago"
                };

                // Serializar request
                var jsonContent = JsonSerializer.Serialize(mercadopagoRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.LogInformation($"MercadoPago Request JSON: {jsonContent}");
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Hacer llamada a MercadoPago API
                var response = await _httpClient.PostAsync("https://api.mercadopago.com/checkout/preferences", content);
                
                _logger.LogInformation($"MercadoPago API Response: Status={response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"MercadoPago API Response Body: {responseJson}");
                    
                    var preference = JsonSerializer.Deserialize<MercadoPagoPreference>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (preference != null && !string.IsNullOrEmpty(preference.Id))
                    {
                        // Actualizar transacción con ID de preferencia
                        transaccion.MercadoPagoPreferenceId = preference.Id;
                        transaccion.ReferenciaPago = preference.Id;
                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"MercadoPago: Preferencia creada - {preference.Id} para usuario {usuarioId}");
                        return (true, preference.Id, null);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"MercadoPago API Error: {response.StatusCode} - {errorContent}");
                    
                    // Manejo específico para errores de autenticación
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        return (false, null, "❌ Credenciales de MercadoPago inválidas. Ve a MERCADOPAGO_SETUP.md para obtener credenciales reales. Usando simulación mientras tanto...");
                    }
                    
                    return (false, null, $"Error de MercadoPago API: {response.StatusCode} - {errorContent}");
                }

                return (false, null, "Error al crear la preferencia de pago");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear preferencia de MercadoPago");
                
                // Si hay error con la API, usar simulación como fallback
                _logger.LogWarning("MercadoPago: Error en API, usando simulación como fallback");
                return await CrearPreferenciaSimuladaAsync(usuarioId, monto, descripcion + " (API falló, usando simulación)");
            }
        }

        /// <summary>
        /// Procesa la confirmación de pago desde MercadoPago
        /// </summary>
        public async Task<(bool Success, string Message)> ProcesarConfirmacionPagoAsync(
            int transaccionId, string status, string? paymentId = null, string? collectionId = null)
        {
            try
            {
                var transaccion = await _context.Transacciones
                    .Include(t => t.Usuario)
                    .FirstOrDefaultAsync(t => t.Id == transaccionId);

                if (transaccion == null)
                {
                    return (false, "Transacción no encontrada");
                }

                // Actualizar datos de MercadoPago
                transaccion.MercadoPagoPaymentId = paymentId;
                transaccion.MercadoPagoCollectionId = collectionId;
                transaccion.MercadoPagoStatus = status;

                switch (status?.ToLower())
                {
                    case "approved":
                        transaccion.Estado = EstadoTransaccion.Completada;
                        transaccion.FechaCompletado = DateTime.Now;

                        // Agregar saldo al usuario
                        transaccion.Usuario.Saldo += transaccion.Monto;

                        _logger.LogInformation($"MercadoPago: Pago aprobado - Transacción {transaccionId}, Monto: {transaccion.Monto}");
                        break;

                    case "pending":
                        transaccion.Estado = EstadoTransaccion.Pendiente;
                        _logger.LogInformation($"MercadoPago: Pago pendiente - Transacción {transaccionId}");
                        break;

                    case "rejected":
                    case "failure":
                        transaccion.Estado = EstadoTransaccion.Fallida;
                        _logger.LogWarning($"MercadoPago: Pago fallido - Transacción {transaccionId}");
                        break;

                    default:
                        _logger.LogWarning($"MercadoPago: Estado desconocido '{status}' para transacción {transaccionId}");
                        break;
                }

                await _context.SaveChangesAsync();

                var mensaje = status?.ToLower() switch
                {
                    "approved" => $"¡Pago exitoso! Se han agregado S/.{transaccion.Monto} a tu saldo.",
                    "pending" => "Tu pago está siendo procesado. Te notificaremos cuando se complete.",
                    "rejected" or "failure" => "El pago fue rechazado. Por favor, intenta con otro método.",
                    _ => "Estado de pago desconocido. Contacta con soporte."
                };

                return (true, mensaje);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al procesar confirmación de MercadoPago para transacción {transaccionId}");
                return (false, "Error interno al procesar el pago");
            }
        }

        /// <summary>
        /// Obtiene información detallada de un pago desde MercadoPago
        /// </summary>
        public async Task<MercadoPagoPayment?> ObtenerInformacionPagoAsync(long paymentId)
        {
            try
            {
                if (string.IsNullOrEmpty(_settings.AccessToken))
                {
                    _logger.LogWarning("MercadoPago: No se puede obtener información real sin Access Token válido");
                    return null;
                }

                // Configurar headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.AccessToken}");

                // Hacer llamada a la API de pagos
                var response = await _httpClient.GetAsync($"https://api.mercadopago.com/v1/payments/{paymentId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var payment = JsonSerializer.Deserialize<MercadoPagoPayment>(responseJson, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });
                    return payment;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener información de pago MercadoPago: {paymentId}");
                return null;
            }
        }

        /// <summary>
        /// Procesa webhook de notificaciones de MercadoPago
        /// </summary>
        public async Task<bool> ProcesarWebhookAsync(string tipo, long? dataId)
        {
            try
            {
                if (tipo == "payment" && dataId.HasValue)
                {
                    var payment = await ObtenerInformacionPagoAsync(dataId.Value);
                    
                    if (payment != null && !string.IsNullOrEmpty(payment.ExternalReference))
                    {
                        if (int.TryParse(payment.ExternalReference, out int transaccionId))
                        {
                            var resultado = await ProcesarConfirmacionPagoAsync(
                                transaccionId, 
                                payment.Status ?? "unknown",
                                payment.Id?.ToString(),
                                payment.Id?.ToString()
                            );

                            _logger.LogInformation($"MercadoPago Webhook: Procesado pago {dataId} para transacción {transaccionId}");
                            return resultado.Success;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al procesar webhook MercadoPago: {tipo}, {dataId}");
                return false;
            }
        }

        /// <summary>
        /// Crear preferencia simulada cuando no hay configuración real
        /// </summary>
        private async Task<(bool Success, string? PreferenceId, string? ErrorMessage)> CrearPreferenciaSimuladaAsync(
            string usuarioId, decimal monto, string descripcion)
        {
            try
            {
                var usuario = await _context.Users.FindAsync(usuarioId);
                if (usuario == null)
                {
                    return (false, null, "Usuario no encontrado");
                }

                // Crear transacción simulada
                var transaccion = new Transaccion
                {
                    UsuarioId = usuarioId,
                    Tipo = TipoTransaccion.Deposito,
                    MetodoPago = MetodoPago.MercadoPago,
                    Monto = monto,
                    Estado = EstadoTransaccion.Pendiente,
                    Descripcion = $"{descripcion} (SIMULADO)",
                    FechaCreacion = DateTime.Now,
                    MercadoPagoPreferenceId = $"SIMULATED-PREF-{Guid.NewGuid():N}",
                    ReferenciaPago = $"SIM-MP-{DateTime.Now:yyyyMMddHHmmss}"
                };

                _context.Transacciones.Add(transaccion);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"MercadoPago SIMULADO: Preferencia creada para usuario {usuarioId}, monto {monto}");

                return (true, transaccion.MercadoPagoPreferenceId, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en simulación de MercadoPago");
                return (false, null, "Error en simulación de pago");
            }
        }

        /// <summary>
        /// Obtiene la URL base de la aplicación
        /// </summary>
        private string GetBaseUrl()
        {
            // En producción, esto debería venir de configuración
            return _settings.Environment == "sandbox" 
                ? "http://localhost:5244" 
                : "https://grupo-negro-apuestas.onrender.com";
        }

        /// <summary>
        /// Obtiene la URL de checkout de MercadoPago
        /// </summary>
        public string GetCheckoutUrl(string preferenceId)
        {
            // Si es una preferencia simulada, devolver URL local
            if (preferenceId.StartsWith("SIMULATED-PREF-"))
            {
                return $"{GetBaseUrl()}/Saldo/CheckoutMercadoPagoSimulado?preferenceId={preferenceId}";
            }

            // Para preferencias reales de MercadoPago
            return _settings.Environment == "sandbox"
                ? $"https://sandbox.mercadopago.com.pe/checkout/v1/redirect?pref_id={preferenceId}"
                : $"https://mercadopago.com.pe/checkout/v1/redirect?pref_id={preferenceId}";
        }
    }
}