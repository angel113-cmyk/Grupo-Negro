using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Grupo_negro.Data;
using Grupo_negro.Models;
using Grupo_negro.Services;
using Microsoft.EntityFrameworkCore;

namespace Grupo_negro.Controllers
{
    [Authorize]
    public class SaldoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPagosService _pagosService;

        public SaldoController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IPagosService pagosService)
        {
            _context = context;
            _userManager = userManager;
            _pagosService = pagosService;
        }

        // GET: Saldo
        public async Task<IActionResult> Index()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            // Obtener historial reciente
            var historial = await _pagosService.ObtenerHistorialPagosAsync(usuario.Id);
            ViewBag.HistorialTransacciones = historial.Take(5).ToList();
            
            return View(usuario);
        }

        // GET: Saldo/Depositar
        public IActionResult Depositar()
        {
            // Verificar si PayPal está en modo demo
            var paypalClientSecret = HttpContext.RequestServices.GetService<IConfiguration>()?["PayPal:ClientSecret"];
            ViewBag.PayPalDemoMode = paypalClientSecret == "sandbox_client_secret_here" || string.IsNullOrEmpty(paypalClientSecret);
            
            return View(new DepositoViewModel());
        }

        // POST: Saldo/Depositar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Depositar(DepositoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            try
            {
                bool exito = false;
                string mensaje = "";

                switch (model.MetodoPago)
                {
                    case MetodoPago.PayPal:
                        var returnUrl = Url.Action("ConfirmarPayPal", "Saldo", null, Request.Scheme);
                        var cancelUrl = Url.Action("Depositar", "Saldo", null, Request.Scheme);
                        var orderId = await _pagosService.CrearPagoPayPalAsync(
                            model.Monto, usuario.Id, TipoTransaccion.Deposito, returnUrl!, cancelUrl!);
                        
                        // Verificar si es una orden simulada
                        if (orderId.StartsWith("SIMULATED_ORDER_"))
                        {
                            // Para órdenes simuladas, redirigir a una página de confirmación local
                            return RedirectToAction("ConfirmarPayPalSimulado", new { orderId = orderId });
                        }
                        else
                        {
                            // Redirigir a PayPal real
                            var approvalUrl = $"https://www.sandbox.paypal.com/checkoutnow?token={orderId}";
                            return Redirect(approvalUrl);
                        }

                    case MetodoPago.MercadoPago:
                        var resultadoMP = await _pagosService.CrearPagoMercadoPagoAsync(
                            model.Monto, usuario.Id, model.Descripcion ?? "Depósito de saldo");

                        if (resultadoMP.Success && !string.IsNullOrEmpty(resultadoMP.PreferenceId))
                        {
                            // Obtener MercadoPagoService para generar URL de checkout
                            var mpService = HttpContext.RequestServices.GetService<MercadoPagoService>();
                            if (mpService != null)
                            {
                                var checkoutUrl = mpService.GetCheckoutUrl(resultadoMP.PreferenceId);
                                return Redirect(checkoutUrl);
                            }
                        }

                        // Si contiene mensaje sobre credenciales inválidas, mostrar como warning y continuar con simulación
                        if (!string.IsNullOrEmpty(resultadoMP.ErrorMessage) && resultadoMP.ErrorMessage.Contains("Credenciales de MercadoPago inválidas"))
                        {
                            TempData["WarningMessage"] = resultadoMP.ErrorMessage;
                            // Intentar de nuevo, el sistema debería usar simulación automáticamente
                            var resultadoSimulado = await _pagosService.CrearPagoMercadoPagoAsync(model.Monto, usuario.Id, model.Descripcion ?? "Depósito de saldo");
                            if (resultadoSimulado.Success && !string.IsNullOrEmpty(resultadoSimulado.PreferenceId))
                            {
                                var mpService = HttpContext.RequestServices.GetService<MercadoPagoService>();
                                if (mpService != null)
                                {
                                    var checkoutUrl = mpService.GetCheckoutUrl(resultadoSimulado.PreferenceId);
                                    return Redirect(checkoutUrl);
                                }
                            }
                        }

                        TempData["ErrorMessage"] = resultadoMP.ErrorMessage ?? "Error al crear pago con MercadoPago";
                        return View(model);

                    case MetodoPago.Yape:
                        if (string.IsNullOrEmpty(model.NumeroOperacion))
                        {
                            ModelState.AddModelError("NumeroOperacion", "El número de operación es requerido para Yape");
                            return View(model);
                        }
                        exito = await _pagosService.ProcesarPagoYapeAsync(model.Monto, usuario.Id, model.NumeroOperacion);
                        mensaje = exito ? "Depósito vía Yape procesado exitosamente" : "Error al procesar el pago con Yape";
                        break;

                    case MetodoPago.Plin:
                        if (string.IsNullOrEmpty(model.NumeroOperacion))
                        {
                            ModelState.AddModelError("NumeroOperacion", "El número de operación es requerido para Plin");
                            return View(model);
                        }
                        exito = await _pagosService.ProcesarPagoPlinAsync(model.Monto, usuario.Id, model.NumeroOperacion);
                        mensaje = exito ? "Depósito vía Plin procesado exitosamente" : "Error al procesar el pago con Plin";
                        break;

                    default:
                        ModelState.AddModelError("", "Método de pago no válido");
                        return View(model);
                }

                if (exito)
                {
                    TempData["SuccessMessage"] = mensaje;
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = mensaje;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al procesar el depósito: {ex.Message}";
            }

            return View(model);
        }

        // GET: Callback de PayPal
        public async Task<IActionResult> ConfirmarPayPal(string token, string PayerID)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            try
            {
                var exito = await _pagosService.ConfirmarPagoPayPalAsync(token, usuario.Id);
                if (exito)
                {
                    TempData["SuccessMessage"] = "Depósito vía PayPal procesado exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al confirmar el pago con PayPal";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error al procesar el pago con PayPal";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Callback de MercadoPago
        public async Task<IActionResult> ConfirmarMercadoPago(int transaccionId, string status, string? payment_id = null, string? collection_id = null)
        {
            try
            {
                var resultado = await _pagosService.ConfirmarPagoMercadoPagoAsync(transaccionId, status, payment_id, collection_id);
                
                if (resultado.Success)
                {
                    TempData["SuccessMessage"] = resultado.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = resultado.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al procesar confirmación de MercadoPago: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Saldo/CheckoutMercadoPagoSimulado
        public IActionResult CheckoutMercadoPagoSimulado(string preferenceId)
        {
            if (string.IsNullOrEmpty(preferenceId) || !preferenceId.StartsWith("SIMULATED-PREF-"))
            {
                TempData["ErrorMessage"] = "Preferencia de pago inválida";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PreferenceId = preferenceId;
            return View();
        }

        // GET: Saldo/ConfirmarMercadoPagoSimulado
        public async Task<IActionResult> ConfirmarMercadoPagoSimulado(string preferenceId)
        {
            if (string.IsNullOrEmpty(preferenceId) || !preferenceId.StartsWith("SIMULATED-PREF-"))
            {
                TempData["ErrorMessage"] = "Preferencia de pago inválida";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Buscar la transacción por preferenceId
                var transaccion = await _context.Transacciones
                    .FirstOrDefaultAsync(t => t.MercadoPagoPreferenceId == preferenceId);

                if (transaccion == null)
                {
                    TempData["ErrorMessage"] = "Transacción no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                // Simular aprobación automática
                var resultado = await _pagosService.ConfirmarPagoMercadoPagoAsync(
                    transaccion.Id, 
                    "approved", 
                    $"SIM-PAY-{transaccion.Id}", 
                    $"SIM-COL-{transaccion.Id}"
                );

                if (resultado.Success)
                {
                    TempData["SuccessMessage"] = "¡Pago simulado exitoso! " + resultado.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = "Error en simulación: " + resultado.Message;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al procesar pago simulado: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Saldo/ConfirmarPayPalSimulado
        public async Task<IActionResult> ConfirmarPayPalSimulado(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                TempData["ErrorMessage"] = "ID de orden inválido";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            try
            {
                var exito = await _pagosService.ConfirmarPagoPayPalAsync(orderId, usuario.Id);
                if (exito)
                {
                    TempData["SuccessMessage"] = "Depósito vía PayPal (simulado) procesado exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al confirmar el pago simulado";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error al procesar el pago simulado";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Saldo/Retirar
        public IActionResult Retirar()
        {
            return View(new RetiroViewModel());
        }

        // POST: Saldo/Retirar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retirar(RetiroViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            if (usuario.Saldo < model.Monto)
            {
                ModelState.AddModelError("Monto", "Saldo insuficiente para realizar el retiro");
                return View(model);
            }

            if (string.IsNullOrEmpty(model.CuentaDestino))
            {
                ModelState.AddModelError("CuentaDestino", "La cuenta destino es requerida");
                return View(model);
            }

            try
            {
                var exito = await _pagosService.ProcesarRetiroAsync(
                    usuario.Id, model.Monto, model.MetodoPago, model.CuentaDestino);

                if (exito)
                {
                    TempData["SuccessMessage"] = $"Solicitud de retiro por ${model.Monto:N2} enviada. Se procesará en 24-48 horas.";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al procesar la solicitud de retiro";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error al procesar el retiro. Intente nuevamente.";
            }

            return View(model);
        }

        // GET: Saldo/Historial
        public async Task<IActionResult> Historial()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return NotFound();

            var transacciones = await _pagosService.ObtenerHistorialPagosAsync(usuario.Id);
            return View(transacciones);
        }
    }
}