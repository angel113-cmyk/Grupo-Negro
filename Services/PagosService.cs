using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using Grupo_negro.Models;
using Grupo_negro.Data;
using Microsoft.EntityFrameworkCore;

namespace Grupo_negro.Services
{
    public interface IPagosService
    {
        Task<string> CrearPagoPayPalAsync(decimal monto, string usuarioId, TipoTransaccion tipo, string returnUrl, string cancelUrl);
        Task<bool> ConfirmarPagoPayPalAsync(string orderId, string usuarioId);
        Task<(bool Success, string? PreferenceId, string? ErrorMessage)> CrearPagoMercadoPagoAsync(decimal monto, string usuarioId, string descripcion = "Depósito de saldo");
        Task<(bool Success, string Message)> ConfirmarPagoMercadoPagoAsync(int transaccionId, string status, string? paymentId = null, string? collectionId = null);
        Task<bool> ProcesarPagoYapeAsync(decimal monto, string usuarioId, string numeroOperacion);
        Task<bool> ProcesarPagoPlinAsync(decimal monto, string usuarioId, string numeroOperacion);
        Task<bool> ProcesarRetiroAsync(string usuarioId, decimal monto, MetodoPago metodo, string cuentaDestino);
        Task<List<Transaccion>> ObtenerHistorialPagosAsync(string usuarioId);
    }

    public class PagosService : IPagosService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PagosService> _logger;
        private readonly PayPalSettings _paypalSettings;
        private readonly PayPalEnvironment _paypalEnvironment;
        private readonly MercadoPagoService _mercadoPagoService;

        public PagosService(
            ApplicationDbContext context,
            ILogger<PagosService> logger,
            IConfiguration configuration,
            MercadoPagoService mercadoPagoService)
        {
            _context = context;
            _logger = logger;
            _mercadoPagoService = mercadoPagoService;
            
            _paypalSettings = new PayPalSettings
            {
                ClientId = configuration["PayPal:ClientId"] ?? "sandbox_client_id",
                ClientSecret = configuration["PayPal:ClientSecret"] ?? "sandbox_client_secret",
                Environment = configuration["PayPal:Environment"] ?? "sandbox"
            };

            _paypalEnvironment = _paypalSettings.Environment == "sandbox"
                ? new SandboxEnvironment(_paypalSettings.ClientId, _paypalSettings.ClientSecret)
                : new LiveEnvironment(_paypalSettings.ClientId, _paypalSettings.ClientSecret);
        }

        public async Task<string> CrearPagoPayPalAsync(decimal monto, string usuarioId, TipoTransaccion tipo, string returnUrl, string cancelUrl)
        {
            try
            {
                // Verificar si tenemos credenciales válidas de PayPal
                if (_paypalSettings.ClientSecret == "sandbox_client_secret_here" || 
                    _paypalSettings.ClientId == "sandbox_client_id" ||
                    string.IsNullOrEmpty(_paypalSettings.ClientSecret) ||
                    string.IsNullOrEmpty(_paypalSettings.ClientId))
                {
                    // Modo simulación cuando no hay credenciales válidas
                    _logger.LogWarning($"PayPal configurado en modo simulación para usuario {usuarioId}");
                    
                    var simulatedOrderId = $"SIMULATED_ORDER_{Guid.NewGuid().ToString("N")[..10].ToUpper()}";
                    
                    // Crear transacción simulada
                    var transaccionSimulada = new Transaccion
                    {
                        UsuarioId = usuarioId,
                        Tipo = tipo,
                        MetodoPago = MetodoPago.PayPal,
                        Monto = monto,
                        Estado = EstadoTransaccion.Pendiente,
                        PayPalOrderId = simulatedOrderId,
                        Descripcion = $"{tipo} vía PayPal (Simulado)"
                    };

                    _context.Transacciones.Add(transaccionSimulada);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Orden PayPal simulada creada: {simulatedOrderId} para usuario {usuarioId}");
                    return simulatedOrderId;
                }

                var client = new PayPalHttpClient(_paypalEnvironment);
                
                var orderRequest = new OrdersCreateRequest();
                orderRequest.Prefer("return=representation");
                orderRequest.RequestBody(new OrderRequest()
                {
                    CheckoutPaymentIntent = "CAPTURE",
                    PurchaseUnits = new List<PurchaseUnitRequest>()
                    {
                        new PurchaseUnitRequest()
                        {
                            AmountWithBreakdown = new AmountWithBreakdown()
                            {
                                CurrencyCode = "USD",
                                Value = (monto / 3.8m).ToString("F2") // Convertir PEN a USD aproximadamente
                            },
                            Description = tipo == TipoTransaccion.Deposito ? "Depósito de saldo" : "Pago de apuesta"
                        }
                    },
                    ApplicationContext = new ApplicationContext()
                    {
                        ReturnUrl = returnUrl,
                        CancelUrl = cancelUrl
                    }
                });

                var response = await client.Execute(orderRequest);
                var order = response.Result<Order>();

                // Crear transacción pendiente
                var transaccionReal = new Transaccion
                {
                    UsuarioId = usuarioId,
                    Tipo = tipo,
                    MetodoPago = MetodoPago.PayPal,
                    Monto = monto,
                    Estado = EstadoTransaccion.Pendiente,
                    PayPalOrderId = order.Id,
                    Descripcion = $"{tipo} vía PayPal"
                };

                _context.Transacciones.Add(transaccionReal);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Orden PayPal creada: {order.Id} para usuario {usuarioId}");
                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear pago PayPal para usuario {usuarioId}");
                throw;
            }
        }

        public async Task<bool> ConfirmarPagoPayPalAsync(string orderId, string usuarioId)
        {
            try
            {
                // Verificar si es una orden simulada
                if (orderId.StartsWith("SIMULATED_ORDER_"))
                {
                    _logger.LogInformation($"Confirmando pago PayPal simulado: {orderId} para usuario {usuarioId}");
                    
                    var transaccion = await _context.Transacciones
                        .FirstOrDefaultAsync(t => t.PayPalOrderId == orderId && t.UsuarioId == usuarioId);

                    if (transaccion != null)
                    {
                        transaccion.Estado = EstadoTransaccion.Completada;
                        transaccion.FechaCompletado = DateTime.Now;
                        transaccion.PayPalPaymentId = orderId;

                        // Actualizar saldo del usuario
                        var usuario = await _context.Users.FindAsync(usuarioId);
                        if (usuario != null && transaccion.Tipo == TipoTransaccion.Deposito)
                        {
                            usuario.Saldo += transaccion.Monto;
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Pago PayPal simulado confirmado: {orderId} para usuario {usuarioId}");
                        return true;
                    }
                    return false;
                }

                var client = new PayPalHttpClient(_paypalEnvironment);
                var request = new OrdersCaptureRequest(orderId);
                request.RequestBody(new OrderActionRequest());

                var response = await client.Execute(request);
                var order = response.Result<Order>();

                if (order.Status == "COMPLETED")
                {
                    var transaccion = await _context.Transacciones
                        .FirstOrDefaultAsync(t => t.PayPalOrderId == orderId && t.UsuarioId == usuarioId);

                    if (transaccion != null)
                    {
                        transaccion.Estado = EstadoTransaccion.Completada;
                        transaccion.FechaCompletado = DateTime.Now;
                        transaccion.PayPalPaymentId = order.Id;

                        // Actualizar saldo del usuario
                        var usuario = await _context.Users.FindAsync(usuarioId);
                        if (usuario != null && transaccion.Tipo == TipoTransaccion.Deposito)
                        {
                            usuario.Saldo += transaccion.Monto;
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Pago PayPal confirmado: {orderId} para usuario {usuarioId}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al confirmar pago PayPal {orderId}");
                return false;
            }
        }

        public async Task<(bool Success, string? PreferenceId, string? ErrorMessage)> CrearPagoMercadoPagoAsync(decimal monto, string usuarioId, string descripcion = "Depósito de saldo")
        {
            try
            {
                _logger.LogInformation($"Creando pago MercadoPago: Usuario {usuarioId}, Monto {monto}");
                return await _mercadoPagoService.CrearPreferenciaPagoAsync(usuarioId, monto, descripcion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear pago MercadoPago para usuario {usuarioId}");
                return (false, null, $"Error interno: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ConfirmarPagoMercadoPagoAsync(int transaccionId, string status, string? paymentId = null, string? collectionId = null)
        {
            try
            {
                _logger.LogInformation($"Confirmando pago MercadoPago: Transacción {transaccionId}, Status {status}");
                return await _mercadoPagoService.ProcesarConfirmacionPagoAsync(transaccionId, status, paymentId, collectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al confirmar pago MercadoPago para transacción {transaccionId}");
                return (false, $"Error interno: {ex.Message}");
            }
        }

        public async Task<bool> ProcesarPagoYapeAsync(decimal monto, string usuarioId, string numeroOperacion)
        {
            try
            {
                // Simular validación de Yape
                await Task.Delay(2000); // Simular tiempo de procesamiento

                var transaccion = new Transaccion
                {
                    UsuarioId = usuarioId,
                    Tipo = TipoTransaccion.Deposito,
                    MetodoPago = MetodoPago.Yape,
                    Monto = monto,
                    Estado = EstadoTransaccion.Completada,
                    NumeroOperacion = numeroOperacion,
                    Descripcion = "Depósito vía Yape",
                    FechaCompletado = DateTime.Now
                };

                _context.Transacciones.Add(transaccion);

                // Actualizar saldo del usuario
                var usuario = await _context.Users.FindAsync(usuarioId);
                if (usuario != null)
                {
                    usuario.Saldo += monto;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Pago Yape procesado: {numeroOperacion} para usuario {usuarioId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al procesar pago Yape para usuario {usuarioId}");
                return false;
            }
        }

        public async Task<bool> ProcesarPagoPlinAsync(decimal monto, string usuarioId, string numeroOperacion)
        {
            try
            {
                // Simular validación de Plin
                await Task.Delay(2000); // Simular tiempo de procesamiento

                var transaccion = new Transaccion
                {
                    UsuarioId = usuarioId,
                    Tipo = TipoTransaccion.Deposito,
                    MetodoPago = MetodoPago.Plin,
                    Monto = monto,
                    Estado = EstadoTransaccion.Completada,
                    NumeroOperacion = numeroOperacion,
                    Descripcion = "Depósito vía Plin",
                    FechaCompletado = DateTime.Now
                };

                _context.Transacciones.Add(transaccion);

                // Actualizar saldo del usuario
                var usuario = await _context.Users.FindAsync(usuarioId);
                if (usuario != null)
                {
                    usuario.Saldo += monto;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Pago Plin procesado: {numeroOperacion} para usuario {usuarioId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al procesar pago Plin para usuario {usuarioId}");
                return false;
            }
        }

        public async Task<bool> ProcesarRetiroAsync(string usuarioId, decimal monto, MetodoPago metodo, string cuentaDestino)
        {
            try
            {
                var usuario = await _context.Users.FindAsync(usuarioId);
                if (usuario == null || usuario.Saldo < monto)
                {
                    return false;
                }

                var transaccion = new Transaccion
                {
                    UsuarioId = usuarioId,
                    Tipo = TipoTransaccion.Retiro,
                    MetodoPago = metodo,
                    Monto = monto,
                    Estado = EstadoTransaccion.Pendiente, // Los retiros requieren aprobación manual
                    ReferenciaPago = cuentaDestino,
                    Descripcion = $"Retiro vía {metodo} a {cuentaDestino}"
                };

                _context.Transacciones.Add(transaccion);

                // Bloquear el saldo del usuario
                usuario.Saldo -= monto;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Retiro creado para usuario {usuarioId}: {monto} vía {metodo}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al procesar retiro para usuario {usuarioId}");
                return false;
            }
        }

        public async Task<List<Transaccion>> ObtenerHistorialPagosAsync(string usuarioId)
        {
            return await _context.Transacciones
                .Where(t => t.UsuarioId == usuarioId)
                .OrderByDescending(t => t.FechaCreacion)
                .Take(20)
                .ToListAsync();
        }
    }
}