using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Grupo_negro.Data;
using Grupo_negro.Models;
using Grupo_negro.Services;
using Grupo_negro.Models.MachineLearning;
using Grupo_negro.ViewModels;

namespace Grupo_negro.Controllers
{
    /// <summary>
    /// Controlador para funcionalidades de Machine Learning y predicciones
    /// </summary>
    [Authorize]
    public class MLController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly MLTrainingService _trainingService;
        private readonly MLPredictionService _predictionService;
        private readonly ILogger<MLController> _logger;

        public MLController(
            ApplicationDbContext context,
            MLTrainingService trainingService,
            MLPredictionService predictionService,
            ILogger<MLController> logger)
        {
            _context = context;
            _trainingService = trainingService;
            _predictionService = predictionService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard principal de Machine Learning
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var viewModel = new MLDashboardViewModel();

                // Verificar estado del modelo
                viewModel.EstaModeloEntrenado = _trainingService.EstaModeloListo;
                
                if (!viewModel.EstaModeloEntrenado)
                {
                    viewModel.EstaModeloEntrenado = await _trainingService.CargarModeloAsync();
                }

                // Obtener estadísticas
                viewModel.TotalPartidosHistoricos = await _context.Partidos
                    .Where(p => p.Estado == EstadoPartido.Finalizado)
                    .CountAsync();

                viewModel.PartidosProximos = await _context.Partidos
                    .Where(p => p.FechaHora >= DateTime.Now && p.FechaHora <= DateTime.Now.AddDays(7))
                    .CountAsync();

                // Si el modelo está entrenado, obtener predicciones y rendimiento
                if (viewModel.EstaModeloEntrenado)
                {
                    var recomendaciones = await _predictionService.GenerarRecomendacionesApuestasAsync();
                    viewModel.RecomendacionesRecientes = recomendaciones.Take(5).ToList();

                    var rendimiento = await _predictionService.EvaluarRendimientoAsync(30);
                    viewModel.RendimientoActual = rendimiento;
                    viewModel.AccuracyModelo = rendimiento.AccuracyGeneral;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando dashboard ML");
                TempData["Error"] = "Error cargando el dashboard de Machine Learning";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Entrena el modelo ML con datos históricos
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> EntrenarModelo()
        {
            try
            {
                _logger.LogInformation("Iniciando entrenamiento del modelo ML desde interfaz web");

                var metrics = await _trainingService.EntrenarModeloAsync();

                TempData["Success"] = $"¡Modelo entrenado exitosamente! Accuracy: {metrics.Accuracy:P2}. " +
                                    $"Entrenado con {metrics.PartidosEntrenamiento} partidos en {metrics.TiempoEntrenamiento.TotalSeconds:F1} segundos.";

                return Json(new { 
                    success = true, 
                    message = "Modelo entrenado exitosamente",
                    metrics = new {
                        accuracy = metrics.Accuracy,
                        partidosEntrenamiento = metrics.PartidosEntrenamiento,
                        tiempoEntrenamiento = metrics.TiempoEntrenamiento.TotalSeconds
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error entrenando modelo ML");
                return Json(new { 
                    success = false, 
                    message = "Error entrenando el modelo: " + ex.Message 
                });
            }
        }

        /// <summary>
        /// Muestra predicciones para partidos próximos
        /// </summary>
        public async Task<IActionResult> Predicciones()
        {
            try
            {
                var partidosProximos = await _context.Partidos
                    .Include(p => p.EquipoLocal)
                    .Include(p => p.EquipoVisitante)
                    .Include(p => p.Liga)
                    .Where(p => p.FechaHora >= DateTime.Now && p.FechaHora <= DateTime.Now.AddDays(14))
                    .OrderBy(p => p.FechaHora)
                    .ToListAsync();

                var viewModel = new PrediccionesViewModel
                {
                    PartidosProximos = partidosProximos
                };

                // Generar predicciones si el modelo está disponible
                if (_trainingService.EstaModeloListo || await _trainingService.CargarModeloAsync())
                {
                    var partidoIds = partidosProximos.Select(p => p.Id).ToList();
                    viewModel.Predicciones = await _predictionService.PredecirPartidosAsync(partidoIds);
                    viewModel.ModeloDisponible = true;
                }
                else
                {
                    viewModel.ModeloDisponible = false;
                    TempData["Warning"] = "El modelo ML no está entrenado. Entrena el modelo primero.";
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando predicciones");
                TempData["Error"] = "Error cargando las predicciones";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Genera predicción para un partido específico (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PredecirPartido(int partidoId)
        {
            try
            {
                var prediccion = await _predictionService.PredecirPartidoAsync(partidoId);
                
                if (prediccion == null)
                {
                    return Json(new { success = false, message = "No se pudo generar predicción" });
                }

                return Json(new { 
                    success = true,
                    prediccion = new {
                        resultadoPredicho = prediccion.ResultadoPredicho,
                        confianza = Math.Round(prediccion.ConfianzaPrediccion, 1),
                        probabilidadLocal = Math.Round(prediccion.ProbabilidadLocal * 100, 1),
                        probabilidadEmpate = Math.Round(prediccion.ProbabilidadEmpate * 100, 1),
                        probabilidadVisitante = Math.Round(prediccion.ProbabilidadVisitante * 100, 1),
                        recomendacion = prediccion.RecomendacionApuesta,
                        valorApuesta = Math.Round(prediccion.ValorApuesta, 1)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error prediciendo partido {partidoId}");
                return Json(new { success = false, message = "Error generando predicción" });
            }
        }

        /// <summary>
        /// Muestra recomendaciones de apuestas basadas en ML
        /// </summary>
        public async Task<IActionResult> Recomendaciones()
        {
            try
            {
                if (!_trainingService.EstaModeloListo && !await _trainingService.CargarModeloAsync())
                {
                    TempData["Warning"] = "El modelo ML no está disponible. Entrena el modelo primero.";
                    return RedirectToAction("Dashboard");
                }

                var recomendaciones = await _predictionService.GenerarRecomendacionesApuestasAsync();
                
                var viewModel = new RecomendacionesViewModel
                {
                    Recomendaciones = recomendaciones,
                    TotalRecomendaciones = recomendaciones.Count,
                    RecomendacionesFuertes = recomendaciones.Count(r => r.ConfianzaML >= 80),
                    RecomendacionesModeradas = recomendaciones.Count(r => r.ConfianzaML >= 65 && r.ConfianzaML < 80),
                    FechaGeneracion = DateTime.Now
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando recomendaciones");
                TempData["Error"] = "Error cargando las recomendaciones";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// Muestra análisis de rendimiento del modelo
        /// </summary>
        public async Task<IActionResult> Rendimiento(int dias = 30)
        {
            try
            {
                if (!_trainingService.EstaModeloListo && !await _trainingService.CargarModeloAsync())
                {
                    TempData["Warning"] = "El modelo ML no está disponible.";
                    return RedirectToAction("Dashboard");
                }

                var rendimiento = await _predictionService.EvaluarRendimientoAsync(dias);
                
                var viewModel = new RendimientoViewModel
                {
                    Rendimiento = rendimiento,
                    DiasAnalisis = dias
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analizando rendimiento");
                TempData["Error"] = "Error analizando el rendimiento";
                return RedirectToAction("Dashboard");
            }
        }

        /// <summary>
        /// API endpoint para obtener predicción rápida (para usar en otras vistas)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPrediccionRapida(int partidoId)
        {
            try
            {
                if (!_trainingService.EstaModeloListo && !await _trainingService.CargarModeloAsync())
                {
                    return Json(new { disponible = false, mensaje = "Modelo no disponible" });
                }

                var prediccion = await _predictionService.PredecirPartidoAsync(partidoId);
                
                if (prediccion == null)
                {
                    return Json(new { disponible = false, mensaje = "No se pudo predecir" });
                }

                return Json(new {
                    disponible = true,
                    resultado = prediccion.ResultadoPredicho,
                    confianza = Math.Round(prediccion.ConfianzaPrediccion, 0),
                    recomendacion = prediccion.ConfianzaPrediccion >= 65 ? "RECOMENDADO" : "NO_RECOMENDADO",
                    icono = prediccion.ResultadoPredicho switch {
                        "Local" => "🏠",
                        "Visitante" => "✈️", 
                        "Empate" => "⚖️",
                        _ => "❓"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en predicción rápida para partido {partidoId}");
                return Json(new { disponible = false, mensaje = "Error interno" });
            }
        }

        /// <summary>
        /// Exporta recomendaciones a CSV
        /// </summary>
        public async Task<IActionResult> ExportarRecomendaciones()
        {
            try
            {
                var recomendaciones = await _predictionService.GenerarRecomendacionesApuestasAsync();
                
                var csv = "Fecha,Partido,Resultado Recomendado,Confianza,Probabilidad Local,Probabilidad Empate,Probabilidad Visitante,Tipo Apuesta,Monto Sugerido\n";
                
                foreach (var rec in recomendaciones)
                {
                    csv += $"{rec.FechaPartido:dd/MM/yyyy HH:mm}," +
                          $"{rec.EquipoLocal} vs {rec.EquipoVisitante}," +
                          $"{rec.ResultadoRecomendado}," +
                          $"{rec.ConfianzaML:F1}%," +
                          $"{rec.ProbabilidadLocal:F1}%," +
                          $"{rec.ProbabilidadEmpate:F1}%," +
                          $"{rec.ProbabilidadVisitante:F1}%," +
                          $"{rec.TipoApuesta}," +
                          $"${rec.MontoSugerido:F2}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"recomendaciones_ml_{DateTime.Now:yyyyMMdd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exportando recomendaciones");
                TempData["Error"] = "Error exportando las recomendaciones";
                return RedirectToAction("Recomendaciones");
            }
        }
    }
}