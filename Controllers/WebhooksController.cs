using Microsoft.AspNetCore.Mvc;
using Grupo_negro.Services;

namespace Grupo_negro.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly MercadoPagoService _mercadoPagoService;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            MercadoPagoService mercadoPagoService,
            ILogger<WebhooksController> logger)
        {
            _mercadoPagoService = mercadoPagoService;
            _logger = logger;
        }

        /// <summary>
        /// Webhook para recibir notificaciones de MercadoPago
        /// </summary>
        [HttpPost("mercadopago")]
        public async Task<IActionResult> MercadoPagoWebhook([FromQuery] string? type, [FromQuery] long? data_id)
        {
            try
            {
                _logger.LogInformation($"MercadoPago Webhook recibido: type={type}, data_id={data_id}");

                if (string.IsNullOrEmpty(type) || !data_id.HasValue)
                {
                    _logger.LogWarning("MercadoPago Webhook: Parámetros inválidos");
                    return BadRequest("Parámetros inválidos");
                }

                // Procesar el webhook
                var resultado = await _mercadoPagoService.ProcesarWebhookAsync(type, data_id);

                if (resultado)
                {
                    _logger.LogInformation($"MercadoPago Webhook procesado exitosamente: {type}/{data_id}");
                    return Ok("Webhook procesado correctamente");
                }
                else
                {
                    _logger.LogWarning($"MercadoPago Webhook: Error al procesar {type}/{data_id}");
                    return BadRequest("Error al procesar webhook");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en MercadoPago Webhook: type={type}, data_id={data_id}");
                return StatusCode(500, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Endpoint de prueba para verificar que los webhooks funcionan
        /// </summary>
        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { 
                message = "Webhooks endpoint funcionando correctamente", 
                timestamp = DateTime.Now,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
            });
        }
    }
}