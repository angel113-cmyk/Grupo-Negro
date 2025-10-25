using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Grupo_negro.Data;
using Grupo_negro.Models;
using Grupo_negro.Services;

namespace Grupo_negro.Controllers
{
    [Authorize]
    public class ComentariosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CookieService _cookieService;

        public ComentariosController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            CookieService cookieService)
        {
            _context = context;
            _userManager = userManager;
            _cookieService = cookieService;
        }

        // GET: Comentarios
        public async Task<IActionResult> Index()
        {
            // Obtener tema preferido del usuario desde cookies
            var temaPreferido = _cookieService.GetCookie("TemaPreferido") ?? "claro";
            ViewBag.TemaPreferido = temaPreferido;

            // Obtener comentarios principales (sin padre) con sus respuestas
            var comentarios = await _context.Comentarios
                .Where(c => c.ComentarioPadreId == null)
                .Include(c => c.Usuario)
                .Include(c => c.Respuestas)
                    .ThenInclude(r => r.Usuario)
                .OrderByDescending(c => c.FechaCreacion)
                .ToListAsync();

            var totalComentarios = await _context.Comentarios.CountAsync();

            var viewModel = new ListaComentariosViewModel
            {
                Comentarios = comentarios,
                TotalComentarios = totalComentarios,
                PuedeComentarUsuario = User.Identity?.IsAuthenticated == true
            };

            return View(viewModel);
        }

        // POST: Crear comentario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ListaComentariosViewModel modelo)
        {
            // Validar solo el NuevoComentario
            if (!string.IsNullOrWhiteSpace(modelo.NuevoComentario?.Contenido))
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Debes iniciar sesión para comentar.";
                    return RedirectToAction("Login", "Account");
                }

                var comentario = new Comentario
                {
                    Contenido = modelo.NuevoComentario.Contenido.Trim(),
                    UsuarioId = usuario.Id,
                    ComentarioPadreId = modelo.NuevoComentario.ComentarioPadreId,
                    FechaCreacion = DateTime.Now
                };

                _context.Comentarios.Add(comentario);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Comentario publicado exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error al publicar el comentario. Verifica que el contenido no esté vacío.";
            }

            return RedirectToAction("Index");
        }

        // POST: Responder a un comentario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Responder(int comentarioPadreId, string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido) || contenido.Length > 1000)
            {
                TempData["ErrorMessage"] = "La respuesta debe tener entre 1 y 1000 caracteres.";
                return RedirectToAction("Index");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Verificar que el comentario padre existe
            var comentarioPadre = await _context.Comentarios.FindAsync(comentarioPadreId);
            if (comentarioPadre == null)
            {
                TempData["ErrorMessage"] = "El comentario al que intentas responder no existe.";
                return RedirectToAction("Index");
            }

            var respuesta = new Comentario
            {
                Contenido = contenido.Trim(),
                UsuarioId = usuario.Id,
                ComentarioPadreId = comentarioPadreId,
                FechaCreacion = DateTime.Now
            };

            _context.Comentarios.Add(respuesta);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Respuesta publicada exitosamente.";
            return RedirectToAction("Index");
        }

        // GET: Editar comentario
        public async Task<IActionResult> Editar(int id)
        {
            var comentario = await _context.Comentarios
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comentario == null)
            {
                TempData["ErrorMessage"] = "Comentario no encontrado.";
                return RedirectToAction("Index");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null || comentario.UsuarioId != usuario.Id)
            {
                TempData["ErrorMessage"] = "No tienes permisos para editar este comentario.";
                return RedirectToAction("Index");
            }

            var viewModel = new ComentarioViewModel
            {
                Id = comentario.Id,
                Contenido = comentario.Contenido,
                ComentarioPadreId = comentario.ComentarioPadreId,
                EsEdicion = true
            };

            return View(viewModel);
        }

        // POST: Actualizar comentario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, ComentarioViewModel modelo)
        {
            if (id != modelo.Id)
            {
                TempData["ErrorMessage"] = "Error en los datos del comentario.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                var comentario = await _context.Comentarios.FindAsync(id);
                if (comentario == null)
                {
                    TempData["ErrorMessage"] = "Comentario no encontrado.";
                    return RedirectToAction("Index");
                }

                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null || comentario.UsuarioId != usuario.Id)
                {
                    TempData["ErrorMessage"] = "No tienes permisos para editar este comentario.";
                    return RedirectToAction("Index");
                }

                comentario.Contenido = modelo.Contenido.Trim();
                comentario.FechaModificacion = DateTime.Now;

                _context.Update(comentario);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Comentario actualizado exitosamente.";
                return RedirectToAction("Index");
            }

            return View(modelo);
        }

        // POST: Eliminar comentario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var comentario = await _context.Comentarios
                .Include(c => c.Respuestas)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (comentario == null)
            {
                TempData["ErrorMessage"] = "Comentario no encontrado.";
                return RedirectToAction("Index");
            }

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null || comentario.UsuarioId != usuario.Id)
            {
                TempData["ErrorMessage"] = "No tienes permisos para eliminar este comentario.";
                return RedirectToAction("Index");
            }

            // Si tiene respuestas, cambiar el contenido en lugar de eliminar
            if (comentario.Respuestas.Any())
            {
                comentario.Contenido = "[Comentario eliminado por el usuario]";
                comentario.FechaModificacion = DateTime.Now;
                _context.Update(comentario);
            }
            else
            {
                _context.Comentarios.Remove(comentario);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Comentario eliminado exitosamente.";
            return RedirectToAction("Index");
        }
    }
}