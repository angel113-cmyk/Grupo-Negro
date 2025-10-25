using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Grupo_negro.Data;
using Grupo_negro.Models;
using Grupo_negro.Services;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace Grupo_negro.Controllers
{
    [Authorize] // Requiere autenticación para todas las acciones
    public class ApuestasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly DatosSimuladosService _datosService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CookieService _cookieService;
        private readonly ApuestaCombinadadService _apuestaCombinadadService;

        public ApuestasController(
            ApplicationDbContext context, 
            DatosSimuladosService datosService,
            UserManager<ApplicationUser> userManager,
            CookieService cookieService,
            ApuestaCombinadadService apuestaCombinadadService)
        {
            _context = context;
            _datosService = datosService;
            _userManager = userManager;
            _cookieService = cookieService;
            _apuestaCombinadadService = apuestaCombinadadService;
        }

        // GET: /Apuestas
        public async Task<IActionResult> Index()
        {
            // Inicializar datos simulados si no existen
            await _datosService.InicializarDatosAsync();

            // Verificar si hay partidos con fechas futuras, si no, regenerar
            var partidosConFechaFutura = await _context.Partidos.CountAsync(p => p.FechaHora > DateTime.Now);
            if (partidosConFechaFutura == 0)
            {
                await _datosService.RegenerarPartidosAsync();
            }

            // Guardar última visita a apuestas
            _cookieService.SetCookie("UltimaVisitaApuestas", DateTime.Now.ToString("dd/MM/yyyy HH:mm"), 30);
            
            // Obtener tema preferido del usuario desde cookies
            var temaPreferido = _cookieService.GetCookie("TemaPreferido") ?? "claro";
            ViewBag.TemaPreferido = temaPreferido;
            
            // Obtener liga favorita del usuario
            var ligaFavorita = _cookieService.GetCookie("LigaFavorita");
            ViewBag.LigaFavorita = ligaFavorita;

            // Obtener todas las ligas para el filtro
            var ligas = await _context.Ligas.ToListAsync();
            ViewBag.Ligas = ligas;

            // Obtener partidos próximos (sin filtro inicial)
            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Include(p => p.Liga)
                .Where(p => p.Estado == EstadoPartido.Programado && p.FechaHora > DateTime.Now)
                .OrderBy(p => p.FechaHora)
                .ToListAsync();

            return View(partidos);
        }

        // GET: /Apuestas/PorLiga/{ligaId}
        [HttpGet]
        public async Task<IActionResult> PorLiga(int ligaId)
        {
            // Inicializar datos simulados si no existen
            await _datosService.InicializarDatosAsync();

            // Verificar si hay partidos futuros, si no regenerar
            var partidosConFechaFutura = await _context.Partidos.CountAsync(p => p.FechaHora > DateTime.Now);
            if (partidosConFechaFutura == 0)
            {
                await _datosService.RegenerarPartidosAsync();
            }

            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Include(p => p.Liga)
                .Where(p => p.LigaId == ligaId && p.Estado == EstadoPartido.Programado && p.FechaHora > DateTime.Now)
                .OrderBy(p => p.FechaHora)
                .ToListAsync();

            // Debug info
            Console.WriteLine($"Filtro por Liga ID: {ligaId}");
            Console.WriteLine($"Partidos encontrados: {partidos.Count}");

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_PartidosPartial", partidos);
            }

            // Si no es AJAX, devolver vista completa con filtro aplicado
            ViewBag.Ligas = await _context.Ligas.ToListAsync();
            ViewBag.LigaSeleccionada = ligaId;
            return View("Index", partidos);
        }

        // GET: /Apuestas/Apostar/{partidoId}
        public async Task<IActionResult> Apostar(int partidoId)
        {
            // Optimización: consulta más específica
            var partido = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Include(p => p.Liga)
                .Where(p => p.Id == partidoId && p.Estado == EstadoPartido.Programado)
                .FirstOrDefaultAsync();

            if (partido == null || partido.Estado != EstadoPartido.Programado)
            {
                TempData["Error"] = "El partido no está disponible para apostar.";
                return RedirectToAction(nameof(Index));
            }

            // Obtener el saldo del usuario
            var usuario = await _userManager.GetUserAsync(User);
            ViewBag.SaldoUsuario = usuario?.Saldo ?? 0m;

            var viewModel = new ApuestaViewModel
            {
                PartidoId = partido.Id,
                NombreEquipoLocal = partido.EquipoLocal.Nombre,
                NombreEquipoVisitante = partido.EquipoVisitante.Nombre,
                FechaHora = partido.FechaHora,
                Liga = partido.Liga.Nombre,
                CuotaLocal = partido.CuotaLocal,
                CuotaEmpate = partido.CuotaEmpate,
                CuotaVisitante = partido.CuotaVisitante
            };

            return View(viewModel);
        }

        // POST: /Apuestas/Apostar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apostar(ApuestaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Recargar datos del partido para la vista
                var partidoData = await _context.Partidos
                    .Include(p => p.EquipoLocal)
                    .Include(p => p.EquipoVisitante)
                    .Include(p => p.Liga)
                    .FirstOrDefaultAsync(p => p.Id == model.PartidoId);

                if (partidoData != null)
                {
                    model.NombreEquipoLocal = partidoData.EquipoLocal.Nombre;
                    model.NombreEquipoVisitante = partidoData.EquipoVisitante.Nombre;
                    model.FechaHora = partidoData.FechaHora;
                    model.Liga = partidoData.Liga.Nombre;
                    model.CuotaLocal = partidoData.CuotaLocal;
                    model.CuotaEmpate = partidoData.CuotaEmpate;
                    model.CuotaVisitante = partidoData.CuotaVisitante;
                }
                return View(model);
            }

            var partido = await _context.Partidos
                .FirstOrDefaultAsync(p => p.Id == model.PartidoId && p.Estado == EstadoPartido.Programado);

            if (partido == null)
            {
                TempData["Error"] = "El partido no está disponible para apostar.";
                return RedirectToAction(nameof(Index));
            }

            // Verificar el saldo del usuario
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "Error al obtener datos del usuario.";
                return RedirectToAction(nameof(Index));
            }

            if (usuario.Saldo < model.MontoApostado)
            {
                TempData["Error"] = $"Saldo insuficiente. Tu saldo actual es ${usuario.Saldo:F2}. Necesitas depositar más dinero para realizar esta apuesta.";
                return RedirectToAction(nameof(Index));
            }

            // Obtener la cuota según el tipo de apuesta
            decimal cuota = model.TipoApuesta switch
            {
                TipoApuesta.GanaLocal => partido.CuotaLocal,
                TipoApuesta.Empate => partido.CuotaEmpate,
                TipoApuesta.GanaVisitante => partido.CuotaVisitante,
                _ => 1.0m
            };

            // Descontar el monto apostado del saldo del usuario
            usuario.Saldo -= model.MontoApostado;
            await _userManager.UpdateAsync(usuario);

            var apuesta = new Apuesta
            {
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
                PartidoId = model.PartidoId,
                TipoApuesta = model.TipoApuesta,
                MontoApostado = model.MontoApostado,
                CuotaAplicada = cuota,
                PosibleGanancia = model.MontoApostado * cuota,
                FechaApuesta = DateTime.Now,
                Estado = EstadoApuesta.Activa
            };

            _context.Apuestas.Add(apuesta);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"¡Apuesta realizada exitosamente! Posible ganancia: ${apuesta.PosibleGanancia:F2}";
            return RedirectToAction(nameof(MisApuestas));
        }

        // GET: /Apuestas/MisApuestas
        public async Task<IActionResult> MisApuestas()
        {
            var usuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var apuestas = await _context.Apuestas
                .Include(a => a.Partido)
                    .ThenInclude(p => p.EquipoLocal)
                .Include(a => a.Partido)
                    .ThenInclude(p => p.EquipoVisitante)
                .Include(a => a.Partido)
                    .ThenInclude(p => p.Liga)
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.FechaApuesta)
                .ToListAsync();

            return View(apuestas);
        }

        [HttpPost]
        public IActionResult GuardarLigaFavorita(string ligaId)
        {
            if (!string.IsNullOrEmpty(ligaId))
            {
                _cookieService.SetCookie("LigaFavorita", ligaId, 365); // Cookie por 1 año
                TempData["Success"] = "Liga favorita guardada correctamente";
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult LimpiarPreferencias()
        {
            _cookieService.DeleteCookie("LigaFavorita");
            _cookieService.DeleteCookie("UltimaVisitaApuestas");
            TempData["Success"] = "Preferencias limpiadas correctamente";
            
            return RedirectToAction("Index");
        }

        // Acciones para apuestas combinadas
        [HttpPost]
        public IActionResult AgregarACarrito(int partidoId, TipoApuesta tipoApuesta, decimal cuota)
        {
            var partido = _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Include(p => p.Liga)
                .FirstOrDefault(p => p.Id == partidoId);

            if (partido == null)
            {
                return Json(new { success = false, message = "Partido no encontrado" });
            }

            var descripcionApuesta = tipoApuesta switch
            {
                TipoApuesta.GanaLocal => $"Victoria {partido.EquipoLocal?.Nombre}",
                TipoApuesta.Empate => "Empate",
                TipoApuesta.GanaVisitante => $"Victoria {partido.EquipoVisitante?.Nombre}",
                _ => "Apuesta desconocida"
            };

            var carritoApuesta = new CarritoApuesta
            {
                PartidoId = partidoId,
                NombrePartido = $"{partido.EquipoLocal?.Nombre} vs {partido.EquipoVisitante?.Nombre}",
                TipoApuesta = tipoApuesta,
                DescripcionApuesta = descripcionApuesta,
                Cuota = cuota,
                FechaPartido = partido.FechaHora,
                Liga = partido.Liga?.Nombre ?? "Liga desconocida"
            };

            var agregado = _apuestaCombinadadService.AgregarAlCarrito(carritoApuesta);

            if (agregado)
            {
                var cantidadSelecciones = _apuestaCombinadadService.ContarSelecciones();
                return Json(new { 
                    success = true, 
                    message = "Apuesta agregada al carrito",
                    cantidadSelecciones = cantidadSelecciones
                });
            }

            return Json(new { success = false, message = "Error al agregar la apuesta" });
        }

        [HttpPost]
        public IActionResult EliminarDeCarrito(int partidoId)
        {
            var eliminado = _apuestaCombinadadService.EliminarDelCarrito(partidoId);
            
            if (eliminado)
            {
                var cantidadSelecciones = _apuestaCombinadadService.ContarSelecciones();
                return Json(new { 
                    success = true, 
                    message = "Apuesta eliminada del carrito",
                    cantidadSelecciones = cantidadSelecciones
                });
            }

            return Json(new { success = false, message = "Error al eliminar la apuesta" });
        }

        public IActionResult CarritoApuestas()
        {
            var resumen = _apuestaCombinadadService.ObtenerResumenCarrito();
            return View(resumen);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarApuestaCombinada(decimal montoApostado)
        {
            if (montoApostado <= 0)
            {
                TempData["Error"] = "El monto de apuesta debe ser mayor a 0";
                return RedirectToAction("CarritoApuestas");
            }

            var carrito = _apuestaCombinadadService.ObtenerCarrito();
            if (carrito.Count < 2)
            {
                TempData["Error"] = "Debe seleccionar al menos 2 apuestas para una apuesta combinada";
                return RedirectToAction("CarritoApuestas");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado";
                return RedirectToAction("CarritoApuestas");
            }

            if (usuario.Saldo < montoApostado)
            {
                TempData["Error"] = $"Saldo insuficiente. Tu saldo actual es: ${usuario.Saldo:F2}";
                return RedirectToAction("CarritoApuestas");
            }

            var cuotaTotal = _apuestaCombinadadService.CalcularCuotaTotal(carrito);
            var posibleGanancia = _apuestaCombinadadService.CalcularPosibleGanancia(carrito, montoApostado);

            // Crear la apuesta combinada
            var apuestaCombinada = new ApuestaCombinada
            {
                UsuarioId = usuario.Id,
                MontoApostado = montoApostado,
                CuotaTotal = cuotaTotal,
                PosibleGanancia = posibleGanancia,
                FechaApuesta = DateTime.Now,
                Estado = EstadoApuesta.Activa,
                Detalles = carrito.Select(c => new DetalleApuestaCombinada
                {
                    PartidoId = c.PartidoId,
                    TipoApuesta = c.TipoApuesta,
                    CuotaSeleccionada = c.Cuota,
                    Estado = EstadoApuesta.Activa
                }).ToList()
            };

            try
            {
                // Descontar saldo del usuario
                usuario.Saldo -= montoApostado;
                _context.Users.Update(usuario);

                // Guardar la apuesta combinada
                _context.ApuestasCombinadas.Add(apuestaCombinada);
                await _context.SaveChangesAsync();

                // Limpiar el carrito
                _apuestaCombinadadService.LimpiarCarrito();

                TempData["Success"] = $"¡Apuesta combinada realizada exitosamente! Posible ganancia: ${posibleGanancia:F2}";
                return RedirectToAction("MisApuestasCombinadas");
            }
            catch (Exception)
            {
                TempData["Error"] = "Error al procesar la apuesta. Intente nuevamente.";
                return RedirectToAction("CarritoApuestas");
            }
        }

        public async Task<IActionResult> MisApuestasCombinadas()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var apuestasCombinadas = await _context.ApuestasCombinadas
                .Include(ac => ac.Detalles)
                    .ThenInclude(d => d.Partido)
                        .ThenInclude(p => p.EquipoLocal)
                .Include(ac => ac.Detalles)
                    .ThenInclude(d => d.Partido)
                        .ThenInclude(p => p.EquipoVisitante)
                .Include(ac => ac.Detalles)
                    .ThenInclude(d => d.Partido)
                        .ThenInclude(p => p.Liga)
                .Where(ac => ac.UsuarioId == usuario.Id)
                .OrderByDescending(ac => ac.FechaApuesta)
                .ToListAsync();

            return View(apuestasCombinadas);
        }

        // GET: /Apuestas/ApuestaCombinada
        public async Task<IActionResult> ApuestaCombinada()
        {
            // Obtener partidos disponibles para combinadas
            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Include(p => p.Liga)
                .Where(p => p.Estado == EstadoPartido.Programado && p.FechaHora > DateTime.Now)
                .OrderBy(p => p.FechaHora)
                .Take(20) // Limitar a 20 partidos para mejor performance
                .ToListAsync();

            var usuario = await _userManager.GetUserAsync(User);
            ViewBag.SaldoUsuario = usuario?.Saldo ?? 0m;
            ViewBag.Ligas = await _context.Ligas.ToListAsync();

            return View(partidos);
        }

        // POST: /Apuestas/CrearApuestaCombinada
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearApuestaCombinada([FromForm] List<int> partidosSeleccionados, 
                                                               [FromForm] List<int> tiposApuesta, 
                                                               [FromForm] decimal montoTotal)
        {
            if (partidosSeleccionados == null || !partidosSeleccionados.Any() || partidosSeleccionados.Count < 2)
            {
                TempData["Error"] = "Debes seleccionar al menos 2 partidos para una apuesta combinada.";
                return RedirectToAction("ApuestaCombinada");
            }

            if (montoTotal <= 0)
            {
                TempData["Error"] = "El monto debe ser mayor a cero.";
                return RedirectToAction("ApuestaCombinada");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                TempData["Error"] = "Error al obtener datos del usuario.";
                return RedirectToAction("ApuestaCombinada");
            }

            if (usuario.Saldo < montoTotal)
            {
                TempData["Error"] = $"Saldo insuficiente. Tu saldo actual es ${usuario.Saldo:F2}.";
                return RedirectToAction("ApuestaCombinada");
            }

            // Obtener los partidos seleccionados
            var partidos = await _context.Partidos
                .Where(p => partidosSeleccionados.Contains(p.Id) && p.Estado == EstadoPartido.Programado)
                .ToListAsync();

            if (partidos.Count != partidosSeleccionados.Count)
            {
                TempData["Error"] = "Algunos partidos seleccionados no están disponibles.";
                return RedirectToAction("ApuestaCombinada");
            }

            // Crear la apuesta combinada
            var apuestaCombinada = new ApuestaCombinada
            {
                UsuarioId = usuario.Id,
                MontoApostado = montoTotal,
                FechaApuesta = DateTime.Now,
                Estado = EstadoApuesta.Activa
            };

            _context.ApuestasCombinadas.Add(apuestaCombinada);
            await _context.SaveChangesAsync();

            // Crear los detalles de la apuesta
            decimal cuotaTotal = 1.0m;
            for (int i = 0; i < partidosSeleccionados.Count; i++)
            {
                var partido = partidos.First(p => p.Id == partidosSeleccionados[i]);
                var tipoApuesta = (TipoApuesta)tiposApuesta[i];

                decimal cuota = tipoApuesta switch
                {
                    TipoApuesta.GanaLocal => partido.CuotaLocal,
                    TipoApuesta.Empate => partido.CuotaEmpate,
                    TipoApuesta.GanaVisitante => partido.CuotaVisitante,
                    _ => 1.0m
                };

                cuotaTotal *= cuota;

                var detalle = new DetalleApuestaCombinada
                {
                    ApuestaCombinada = apuestaCombinada,
                    PartidoId = partidosSeleccionados[i],
                    TipoApuesta = tipoApuesta,
                    CuotaSeleccionada = cuota
                };

                _context.Add(detalle);
            }

            // Actualizar cuota total y ganancia potencial
            apuestaCombinada.CuotaTotal = cuotaTotal;
            apuestaCombinada.PosibleGanancia = montoTotal * cuotaTotal;

            // Descontar saldo del usuario
            usuario.Saldo -= montoTotal;
            await _userManager.UpdateAsync(usuario);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Apuesta combinada creada exitosamente. Ganancia potencial: ${apuestaCombinada.PosibleGanancia:F2}";
            return RedirectToAction("MisApuestasCombinadas");
        }

        [HttpGet]
        public IActionResult ObtenerEstadoCarrito()
        {
            var cantidadSelecciones = _apuestaCombinadadService.ContarSelecciones();
            var carrito = _apuestaCombinadadService.ObtenerCarrito();
            
            return Json(new { 
                cantidadSelecciones = cantidadSelecciones,
                selecciones = carrito.Select(c => new {
                    partidoId = c.PartidoId,
                    nombrePartido = c.NombrePartido,
                    descripcionApuesta = c.DescripcionApuesta,
                    cuota = c.Cuota
                })
            });
        }
    }
}