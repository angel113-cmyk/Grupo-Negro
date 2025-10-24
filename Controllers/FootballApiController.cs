using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Grupo_negro.Services;
using Grupo_negro.Models;

namespace Grupo_negro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FootballApiController : Controller
    {
        private readonly IFootballApiService _footballApiService;
        private readonly ILogger<FootballApiController> _logger;

        public FootballApiController(IFootballApiService footballApiService, ILogger<FootballApiController> logger)
        {
            _footballApiService = footballApiService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCompetitions()
        {
            try
            {
                var competitions = await _footballApiService.GetCompetitionsAsync();
                return Json(new { success = true, data = competitions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener competiciones");
                return Json(new { success = false, message = "Error al obtener competiciones" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTeams(int competitionId)
        {
            try
            {
                var teams = await _footballApiService.GetTeamsByCompetitionAsync(competitionId);
                return Json(new { success = true, data = teams });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener equipos de competición {competitionId}");
                return Json(new { success = false, message = "Error al obtener equipos" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMatches(int competitionId, int days = 7)
        {
            try
            {
                var matches = await _footballApiService.GetMatchesByCompetitionAsync(competitionId, days);
                return Json(new { success = true, data = matches });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener partidos de competición {competitionId}");
                return Json(new { success = false, message = "Error al obtener partidos" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTodayMatches()
        {
            try
            {
                var matches = await _footballApiService.GetTodayMatchesAsync();
                return Json(new { success = true, data = matches });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener partidos de hoy");
                return Json(new { success = false, message = "Error al obtener partidos de hoy" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SyncData()
        {
            try
            {
                await _footballApiService.SyncDataToLocalDatabaseAsync();
                TempData["SuccessMessage"] = "Datos sincronizados exitosamente";
                return Json(new { success = true, message = "Sincronización completada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la sincronización");
                return Json(new { success = false, message = "Error durante la sincronización" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var competitions = await _footballApiService.GetCompetitionsAsync();
                var isConnected = competitions.Any();
                
                return Json(new { 
                    success = isConnected, 
                    message = isConnected ? "Conexión exitosa" : "Sin datos disponibles",
                    count = competitions.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al probar conexión");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}