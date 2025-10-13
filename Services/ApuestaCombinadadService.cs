using Grupo_negro.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Grupo_negro.Services
{
    public class ApuestaCombinadadService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string CARRITO_KEY = "CarritoApuestas";

        public ApuestaCombinadadService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public List<CarritoApuesta> ObtenerCarrito()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var carritoJson = session?.GetString(CARRITO_KEY);
            
            if (string.IsNullOrEmpty(carritoJson))
                return new List<CarritoApuesta>();

            try
            {
                return JsonSerializer.Deserialize<List<CarritoApuesta>>(carritoJson) ?? new List<CarritoApuesta>();
            }
            catch
            {
                return new List<CarritoApuesta>();
            }
        }

        public void GuardarCarrito(List<CarritoApuesta> carrito)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var carritoJson = JsonSerializer.Serialize(carrito);
            session?.SetString(CARRITO_KEY, carritoJson);
        }

        public bool AgregarAlCarrito(CarritoApuesta nuevaApuesta)
        {
            var carrito = ObtenerCarrito();

            // Verificar si ya existe una apuesta del mismo partido
            var existente = carrito.FirstOrDefault(c => c.PartidoId == nuevaApuesta.PartidoId);
            if (existente != null)
            {
                // Actualizar la apuesta existente
                existente.TipoApuesta = nuevaApuesta.TipoApuesta;
                existente.DescripcionApuesta = nuevaApuesta.DescripcionApuesta;
                existente.Cuota = nuevaApuesta.Cuota;
            }
            else
            {
                // Agregar nueva apuesta
                carrito.Add(nuevaApuesta);
            }

            GuardarCarrito(carrito);
            return true;
        }

        public bool EliminarDelCarrito(int partidoId)
        {
            var carrito = ObtenerCarrito();
            var apuestaAEliminar = carrito.FirstOrDefault(c => c.PartidoId == partidoId);
            
            if (apuestaAEliminar != null)
            {
                carrito.Remove(apuestaAEliminar);
                GuardarCarrito(carrito);
                return true;
            }

            return false;
        }

        public void LimpiarCarrito()
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.Remove(CARRITO_KEY);
        }

        public ApuestaCombinadadViewModel ObtenerResumenCarrito()
        {
            var carrito = ObtenerCarrito();
            return new ApuestaCombinadadViewModel
            {
                Selecciones = carrito
            };
        }

        public decimal CalcularCuotaTotal(List<CarritoApuesta> selecciones)
        {
            if (!selecciones.Any()) return 0;
            return selecciones.Select(s => s.Cuota).Aggregate((a, b) => a * b);
        }

        public decimal CalcularPosibleGanancia(List<CarritoApuesta> selecciones, decimal montoApostado)
        {
            var cuotaTotal = CalcularCuotaTotal(selecciones);
            return montoApostado * cuotaTotal;
        }

        public int ContarSelecciones()
        {
            return ObtenerCarrito().Count;
        }

        public bool TieneSeleccionesSuficientes()
        {
            return ContarSelecciones() >= 2;
        }
    }
}