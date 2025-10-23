using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Grupo_negro.Data;
using Grupo_negro.Models;

namespace Grupo_negro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
            var estadisticas = new AdminDashboardViewModel
            {
                TotalUsuarios = await _userManager.Users.CountAsync(),
                TotalApuestas = await _context.Apuestas.CountAsync(),
                ApuestasActivas = await _context.Apuestas.CountAsync(a => a.Estado == EstadoApuesta.Activa),
                ApuestasGanadas = await _context.Apuestas.CountAsync(a => a.Estado == EstadoApuesta.Ganada),
                ApuestasPerdidas = await _context.Apuestas.CountAsync(a => a.Estado == EstadoApuesta.Perdida),
                TotalMontoApostado = await _context.Apuestas.SumAsync(a => a.MontoApostado),
                ApuestasRecientes = await _context.Apuestas
                    .Include(a => a.Usuario)
                    .Include(a => a.Partido)
                        .ThenInclude(p => p.EquipoLocal)
                    .Include(a => a.Partido)
                        .ThenInclude(p => p.EquipoVisitante)
                    .Include(a => a.Partido)
                        .ThenInclude(p => p.Liga)
                    .OrderByDescending(a => a.FechaApuesta)
                    .Take(10)
                    .ToListAsync()
            };

            return View(estadisticas);
        }

        // GET: Admin/Apuestas
        public async Task<IActionResult> Apuestas()
        {
            var apuestas = await _context.Apuestas
                .Include(a => a.Usuario)
                .Include(a => a.Partido)
                    .ThenInclude(p => p.EquipoLocal)
                .Include(a => a.Partido)
                    .ThenInclude(p => p.EquipoVisitante)
                .Include(a => a.Partido)
                    .ThenInclude(p => p.Liga)
                .OrderByDescending(a => a.FechaApuesta)
                .ToListAsync();

            return View(apuestas);
        }

        // GET: Admin/Partidos
        public async Task<IActionResult> Partidos()
        {
            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Include(p => p.Liga)
                .Include(p => p.Apuestas)
                .OrderByDescending(p => p.FechaHora)
                .ToListAsync();

            return View(partidos);
        }

        // POST: Admin/FinalizarPartido/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizarPartido(int id, int golesLocal, int golesVisitante)
        {
            var partido = await _context.Partidos
                .Include(p => p.Apuestas)
                    .ThenInclude(a => a.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (partido == null)
            {
                TempData["Error"] = "Partido no encontrado.";
                return RedirectToAction(nameof(Partidos));
            }

            // Actualizar el resultado del partido
            partido.GolesLocal = golesLocal;
            partido.GolesVisitante = golesVisitante;
            partido.Estado = EstadoPartido.Finalizado;

            // Determinar el resultado del partido
            TipoApuesta resultadoPartido;
            if (golesLocal > golesVisitante)
                resultadoPartido = TipoApuesta.GanaLocal;
            else if (golesLocal < golesVisitante)
                resultadoPartido = TipoApuesta.GanaVisitante;
            else
                resultadoPartido = TipoApuesta.Empate;

            // Procesar todas las apuestas de este partido
            foreach (var apuesta in partido.Apuestas.Where(a => a.Estado == EstadoApuesta.Activa))
            {
                if (apuesta.TipoApuesta == resultadoPartido)
                {
                    // Apuesta ganada
                    apuesta.Estado = EstadoApuesta.Ganada;
                    apuesta.FechaResolucion = DateTime.Now;
                    
                    // Agregar las ganancias al saldo del usuario
                    apuesta.Usuario.Saldo += apuesta.PosibleGanancia;
                    await _userManager.UpdateAsync(apuesta.Usuario);
                }
                else
                {
                    // Apuesta perdida
                    apuesta.Estado = EstadoApuesta.Perdida;
                    apuesta.FechaResolucion = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Partido finalizado. Resultado: {golesLocal}-{golesVisitante}. Se procesaron {partido.Apuestas.Count} apuestas.";
            return RedirectToAction(nameof(Partidos));
        }

        // GET: Admin/Usuarios
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _userManager.Users.ToListAsync();
            usuarios = usuarios.OrderByDescending(u => u.Saldo).ToList();

            return View(usuarios);
        }

        // GET: Admin/UsuariosNuevos
        public async Task<IActionResult> UsuariosNuevos()
        {
            var primerDiaMes = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var usuarios = await _userManager.Users
                .Where(u => u.FechaRegistro >= primerDiaMes)
                .OrderByDescending(u => u.FechaRegistro)
                .ToListAsync();

            ViewBag.TotalUsuariosNuevos = usuarios.Count;
            return View(usuarios);
        }

        // GET: Admin/DetalleUsuario/{id}
        public async Task<IActionResult> DetalleUsuario(string id)
        {
            var usuario = await _userManager.Users
                .Include(u => u.Bonos)
                    .ThenInclude(b => b.AsignadoPor)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(Usuarios));
            }

            var apuestas = await _context.Apuestas
                .Include(a => a.Partido)
                    .ThenInclude(p => p.EquipoLocal)
                .Include(a => a.Partido)
                    .ThenInclude(p => p.EquipoVisitante)
                .Where(a => a.UsuarioId == id)
                .OrderByDescending(a => a.FechaApuesta)
                .Take(10)
                .ToListAsync();

            ViewBag.Apuestas = apuestas;
            return View(usuario);
        }

        // GET: Admin/AsignarBonos
        public async Task<IActionResult> AsignarBonos()
        {
            var usuarios = await _userManager.Users
                .OrderBy(u => u.Nombres)
                .ThenBy(u => u.Apellidos)
                .ToListAsync();

            return View(usuarios);
        }

        // POST: Admin/AsignarBono
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarBono(string usuarioId, string concepto, decimal monto)
        {
            var usuario = await _userManager.FindByIdAsync(usuarioId);
            if (usuario == null)
            {
                TempData["Error"] = "Usuario no encontrado.";
                return RedirectToAction(nameof(AsignarBonos));
            }

            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null)
            {
                TempData["Error"] = "Error al obtener el administrador.";
                return RedirectToAction(nameof(AsignarBonos));
            }

            // Crear el bono
            var bono = new BonoUsuario
            {
                UsuarioId = usuarioId,
                Concepto = concepto,
                Monto = monto,
                AsignadoPorId = adminUser.Id,
                FechaAsignacion = DateTime.Now,
                Aplicado = true
            };

            // Agregar el monto al saldo del usuario
            usuario.Saldo += monto;

            _context.BonosUsuarios.Add(bono);
            await _userManager.UpdateAsync(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Bono de ${monto:N2} asignado exitosamente a {usuario.NombreCompleto}.";
            return RedirectToAction(nameof(AsignarBonos));
        }

        // GET: Admin/CrearPartido
        public async Task<IActionResult> CrearPartido()
        {
            ViewBag.Ligas = await _context.Ligas.OrderBy(l => l.Nombre).ToListAsync();
            ViewBag.Equipos = await _context.Equipos.OrderBy(e => e.Nombre).ToListAsync();
            return View();
        }

        // POST: Admin/CrearPartido
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearPartido(Partido partido)
        {
            if (partido.EquipoLocalId == partido.EquipoVisitanteId)
            {
                TempData["Error"] = "El equipo local y visitante no pueden ser el mismo.";
                ViewBag.Ligas = await _context.Ligas.OrderBy(l => l.Nombre).ToListAsync();
                ViewBag.Equipos = await _context.Equipos.OrderBy(e => e.Nombre).ToListAsync();
                return View(partido);
            }

            partido.Estado = EstadoPartido.Programado;
            _context.Partidos.Add(partido);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Partido creado exitosamente.";
            return RedirectToAction(nameof(Partidos));
        }
    }
}