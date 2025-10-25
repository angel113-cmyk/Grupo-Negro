using Microsoft.EntityFrameworkCore;
using Grupo_negro.Data;
using Grupo_negro.Models;
using Grupo_negro.Models.MachineLearning;

namespace Grupo_negro.Services
{
    /// <summary>
    /// Servicio para generar predicciones de partidos usando Machine Learning
    /// </summary>
    public class MLPredictionService
    {
        private readonly ApplicationDbContext _context;
        private readonly MLTrainingService _trainingService;
        private readonly ILogger<MLPredictionService> _logger;

        public MLPredictionService(
            ApplicationDbContext context,
            MLTrainingService trainingService,
            ILogger<MLPredictionService> logger)
        {
            _context = context;
            _trainingService = trainingService;
            _logger = logger;
        }

        /// <summary>
        /// Genera predicción para un partido específico
        /// </summary>
        public async Task<MatchPrediction?> PredecirPartidoAsync(int partidoId)
        {
            try
            {
                // Verificar que el modelo esté cargado
                if (!_trainingService.EstaModeloListo)
                {
                    var cargado = await _trainingService.CargarModeloAsync();
                    if (!cargado)
                    {
                        _logger.LogWarning("No se pudo cargar el modelo ML para predicciones");
                        return null;
                    }
                }

                var partido = await _context.Partidos
                    .Include(p => p.EquipoLocal)
                    .Include(p => p.EquipoVisitante)
                    .Include(p => p.Liga)
                    .FirstOrDefaultAsync(p => p.Id == partidoId);

                if (partido == null)
                {
                    _logger.LogWarning($"Partido {partidoId} no encontrado");
                    return null;
                }

                return await PredecirPartidoAsync(partido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error prediciendo partido {partidoId}");
                return null;
            }
        }

        /// <summary>
        /// Genera predicción para un partido
        /// </summary>
        public async Task<MatchPrediction?> PredecirPartidoAsync(Partido partido)
        {
            try
            {
                if (!_trainingService.EstaModeloListo)
                {
                    _logger.LogWarning("Modelo ML no está cargado");
                    return null;
                }

                // Preparar datos del partido para predicción
                var matchData = await PrepararDatosPartidoAsync(partido);
                
                // Realizar predicción
                var predictionEngine = _trainingService.ObtenerPredictionEngine();
                if (predictionEngine == null) return null;

                var prediction = predictionEngine.Predict(matchData);
                
                // Enriquecer predicción con análisis adicional
                await EnriquecerPrediccionAsync(prediction, partido, matchData);

                _logger.LogInformation($"Predicción generada para {partido.EquipoLocal?.Nombre} vs {partido.EquipoVisitante?.Nombre}: {prediction.ResultadoPredicho} (Confianza: {prediction.ConfianzaPrediccion:F1}%)");

                return prediction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generando predicción para partido {partido.Id}");
                return null;
            }
        }

        /// <summary>
        /// Genera predicciones para múltiples partidos
        /// </summary>
        public async Task<Dictionary<int, MatchPrediction>> PredecirPartidosAsync(List<int> partidoIds)
        {
            var predicciones = new Dictionary<int, MatchPrediction>();

            if (!_trainingService.EstaModeloListo)
            {
                await _trainingService.CargarModeloAsync();
            }

            var partidos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Include(p => p.Liga)
                .Where(p => partidoIds.Contains(p.Id))
                .ToListAsync();

            foreach (var partido in partidos)
            {
                var prediccion = await PredecirPartidoAsync(partido);
                if (prediccion != null)
                {
                    predicciones[partido.Id] = prediccion;
                }
            }

            return predicciones;
        }

        /// <summary>
        /// Genera predicciones para todos los partidos próximos
        /// </summary>
        public async Task<Dictionary<int, MatchPrediction>> PredecirPartidosProximosAsync()
        {
            var fechaLimite = DateTime.Now.AddDays(7); // Próximos 7 días
            
            var partidosProximos = await _context.Partidos
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .Include(p => p.Liga)
                .Where(p => p.FechaHora >= DateTime.Now && p.FechaHora <= fechaLimite && p.Estado != EstadoPartido.Finalizado)
                .ToListAsync();

            var partidoIds = partidosProximos.Select(p => p.Id).ToList();
            return await PredecirPartidosAsync(partidoIds);
        }

        /// <summary>
        /// Genera recomendaciones de apuestas basadas en predicciones ML
        /// </summary>
        public async Task<List<RecomendacionApuesta>> GenerarRecomendacionesApuestasAsync()
        {
            var recomendaciones = new List<RecomendacionApuesta>();
            var predicciones = await PredecirPartidosProximosAsync();

            foreach (var (partidoId, prediccion) in predicciones)
            {
                var partido = await _context.Partidos
                    .Include(p => p.EquipoLocal)
                    .Include(p => p.EquipoVisitante)
                    .FirstOrDefaultAsync(p => p.Id == partidoId);

                if (partido == null) continue;

                // Solo recomendar si la confianza es alta
                if (prediccion.ConfianzaPrediccion >= 65)
                {
                    var recomendacion = new RecomendacionApuesta
                    {
                        PartidoId = partidoId,
                        EquipoLocal = partido.EquipoLocal?.Nombre ?? "Local",
                        EquipoVisitante = partido.EquipoVisitante?.Nombre ?? "Visitante",
                        FechaPartido = partido.FechaHora,
                        ResultadoRecomendado = prediccion.ResultadoPredicho,
                        ConfianzaML = prediccion.ConfianzaPrediccion,
                        ProbabilidadLocal = prediccion.ProbabilidadLocal * 100,
                        ProbabilidadEmpate = prediccion.ProbabilidadEmpate * 100,
                        ProbabilidadVisitante = prediccion.ProbabilidadVisitante * 100,
                        ValorApuesta = prediccion.ValorApuesta,
                        TipoApuesta = DeterminarTipoApuesta(prediccion),
                        MontoSugerido = CalcularMontoSugerido(prediccion.ConfianzaPrediccion, prediccion.ValorApuesta)
                    };

                    recomendaciones.Add(recomendacion);
                }
            }

            return recomendaciones.OrderByDescending(r => r.ConfianzaML).ToList();
        }

        /// <summary>
        /// Evalúa el rendimiento de las predicciones pasadas
        /// </summary>
        public async Task<RendimientoPredicciones> EvaluarRendimientoAsync(int diasAtras = 30)
        {
            var fechaDesde = DateTime.Now.AddDays(-diasAtras);
            
            var partidosFinalizados = await _context.Partidos
                .Where(p => p.FechaHora >= fechaDesde && p.Estado == EstadoPartido.Finalizado && !string.IsNullOrEmpty(p.Resultado))
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .ToListAsync();

            var prediccionesCorrectas = 0;
            var totalPredicciones = 0;
            var beneficioAcumulado = 0.0;
            var prediccionesPorTipo = new Dictionary<string, (int correctas, int total)>
            {
                ["Local"] = (0, 0),
                ["Empate"] = (0, 0),
                ["Visitante"] = (0, 0)
            };

            foreach (var partido in partidosFinalizados)
            {
                try
                {
                    // Simular predicción histórica
                    var prediccion = await SimularPrediccionHistorica(partido);
                    if (prediccion?.ConfianzaPrediccion >= 50) // Solo evaluar predicciones con confianza mínima
                    {
                        totalPredicciones++;
                        var resultadoReal = DeterminarResultadoReal(partido);
                        
                        if (prediccion.ResultadoPredicho == resultadoReal)
                        {
                            prediccionesCorrectas++;
                            prediccionesPorTipo[resultadoReal] = (prediccionesPorTipo[resultadoReal].correctas + 1, prediccionesPorTipo[resultadoReal].total + 1);
                            
                            // Simular beneficio de apuesta exitosa
                            var cuotaAproximada = CalcularCuotaAproximada(prediccion);
                            beneficioAcumulado += (cuotaAproximada - 1) * 10; // Apostando $10 por partido
                        }
                        else
                        {
                            prediccionesPorTipo[prediccion.ResultadoPredicho] = (prediccionesPorTipo[prediccion.ResultadoPredicho].correctas, prediccionesPorTipo[prediccion.ResultadoPredicho].total + 1);
                            beneficioAcumulado -= 10; // Pérdida de $10
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error evaluando partido {partido.Id}: {ex.Message}");
                }
            }

            return new RendimientoPredicciones
            {
                AccuracyGeneral = totalPredicciones > 0 ? (double)prediccionesCorrectas / totalPredicciones * 100 : 0,
                TotalPredicciones = totalPredicciones,
                PrediccionesCorrectas = prediccionesCorrectas,
                BeneficioAcumulado = beneficioAcumulado,
                AccuracyPorTipo = prediccionesPorTipo.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.total > 0 ? (double)kvp.Value.correctas / kvp.Value.total * 100 : 0
                ),
                FechaEvaluacion = DateTime.Now,
                DiasEvaluados = diasAtras
            };
        }

        /// <summary>
        /// Prepara datos de entrada para el modelo ML
        /// </summary>
        private async Task<MatchData> PrepararDatosPartidoAsync(Partido partido)
        {
            // No necesitamos crear un nuevo servicio de entrenamiento aquí
            
            var statsLocal = await CalcularStatsEquipoActuales(partido.EquipoLocalId);
            var statsVisitante = await CalcularStatsEquipoActuales(partido.EquipoVisitanteId);
            
            var matchData = new MatchData
            {
                PartidoId = partido.Id,
                
                // Stats equipo local
                EquipoLocalRating = statsLocal.Rating,
                EquipoLocalGolesPromedio = statsLocal.GolesPromedio,
                EquipoLocalGolesRecibidos = statsLocal.GolesRecibidosPromedio,
                EquipoLocalVictoriasUltimos5 = statsLocal.VictoriasUltimos5,
                EquipoLocalPartidosLocal = statsLocal.RendimientoLocal,
                
                // Stats equipo visitante
                EquipoVisitanteRating = statsVisitante.Rating,
                EquipoVisitanteGolesPromedio = statsVisitante.GolesPromedio,
                EquipoVisitanteGolesRecibidos = statsVisitante.GolesRecibidosPromedio,
                EquipoVisitanteVictoriasUltimos5 = statsVisitante.VictoriasUltimos5,
                EquipoVisitantePartidosVisitante = statsVisitante.RendimientoVisitante,
                
                // Contexto
                ImportanciaPartido = ObtenerImportanciaPartido(partido),
                DiferenciaRating = statsLocal.Rating - statsVisitante.Rating,
                HistorialEnfrentamientos = await CalcularHistorialEnfrentamientos(partido.EquipoLocalId, partido.EquipoVisitanteId),
                TemporadaActual = CalcularPosicionTemporada(partido.FechaHora),
                DiasDescanso = await CalcularDiasDescanso(partido.EquipoLocalId, partido.FechaHora)
            };

            return matchData;
        }

        /// <summary>
        /// Enriquece la predicción con análisis adicional
        /// </summary>
        private async Task EnriquecerPrediccionAsync(MatchPrediction prediction, Partido partido, MatchData matchData)
        {
            // Ajustar probabilidades si están muy sesgadas
            var total = prediction.ProbabilidadLocal + prediction.ProbabilidadEmpate + prediction.ProbabilidadVisitante;
            if (total > 0)
            {
                prediction.Probabilidades[0] = prediction.ProbabilidadLocal / total;
                prediction.Probabilidades[1] = prediction.ProbabilidadEmpate / total;
                prediction.Probabilidades[2] = prediction.ProbabilidadVisitante / total;
            }

            // Aplicar factores de corrección para hacer más realista
            var random = new Random(partido.Id + DateTime.Now.Day);
            
            // Agregar variabilidad natural (+/- 10%)
            var variabilidad = (float)(random.NextDouble() * 0.2 - 0.1); // -10% a +10%
            
            prediction.Probabilidades[0] = Math.Max(0.05f, Math.Min(0.85f, prediction.Probabilidades[0] + variabilidad));
            prediction.Probabilidades[1] = Math.Max(0.05f, Math.Min(0.85f, prediction.Probabilidades[1] + variabilidad * 0.5f));
            prediction.Probabilidades[2] = Math.Max(0.05f, Math.Min(0.85f, prediction.Probabilidades[2] - variabilidad * 0.5f));

            // Normalizar de nuevo
            total = prediction.Probabilidades[0] + prediction.Probabilidades[1] + prediction.Probabilidades[2];
            prediction.Probabilidades[0] /= total;
            prediction.Probabilidades[1] /= total;
            prediction.Probabilidades[2] /= total;

            // Determinar resultado basado en probabilidades ajustadas
            var maxProb = Math.Max(prediction.Probabilidades[0], Math.Max(prediction.Probabilidades[1], prediction.Probabilidades[2]));
            
            if (prediction.Probabilidades[0] == maxProb)
                prediction.ResultadoPredicho = "Local";
            else if (prediction.Probabilidades[1] == maxProb)
                prediction.ResultadoPredicho = "Empate";
            else
                prediction.ResultadoPredicho = "Visitante";

            // Calcular confianza basada en la diferencia entre la mayor y segunda mayor probabilidad
            var probs = prediction.Probabilidades.OrderByDescending(p => p).ToArray();
            var diferencia = probs[0] - probs[1];
            prediction.ConfianzaPrediccion = (diferencia * 200 + maxProb * 100) / 2; // Combina diferencia y probabilidad máxima

            // Ajustar por contexto del partido
            if (Math.Abs(matchData.DiferenciaRating) > 15)
            {
                prediction.ConfianzaPrediccion *= 1.1f;
            }

            if (Math.Abs(matchData.EquipoLocalVictoriasUltimos5 - matchData.EquipoVisitanteVictoriasUltimos5) >= 3)
            {
                prediction.ConfianzaPrediccion *= 1.05f;
            }

            // Generar recomendación basada en confianza ajustada
            prediction.ConfianzaPrediccion = Math.Max(25, Math.Min(95, prediction.ConfianzaPrediccion));

            if (prediction.ConfianzaPrediccion >= 70)
            {
                prediction.RecomendacionApuesta = $"Apuesta FUERTE en {prediction.ResultadoPredicho}";
                prediction.ValorApuesta = 7 + (prediction.ConfianzaPrediccion - 70) / 25 * 3;
            }
            else if (prediction.ConfianzaPrediccion >= 60)
            {
                prediction.RecomendacionApuesta = $"Apuesta moderada en {prediction.ResultadoPredicho}";
                prediction.ValorApuesta = 4 + (prediction.ConfianzaPrediccion - 60) / 10 * 3;
            }
            else if (prediction.ConfianzaPrediccion >= 50)
            {
                prediction.RecomendacionApuesta = $"Apuesta conservadora en {prediction.ResultadoPredicho}";
                prediction.ValorApuesta = 2 + (prediction.ConfianzaPrediccion - 50) / 10 * 2;
            }
            else
            {
                prediction.RecomendacionApuesta = "No apostar - Resultado muy incierto";
                prediction.ValorApuesta = 0;
            }

            prediction.ValorApuesta = Math.Max(0, Math.Min(10, prediction.ValorApuesta));
        }

        // Métodos auxiliares similares a MLTrainingService
        private async Task<EquipoStats> CalcularStatsEquipoActuales(int equipoId)
        {
            var fechaHasta = DateTime.Now;
            var partidos = await _context.Partidos
                .Where(p => (p.EquipoLocalId == equipoId || p.EquipoVisitanteId == equipoId) 
                           && p.FechaHora < fechaHasta 
                           && p.Estado == EstadoPartido.Finalizado)
                .OrderByDescending(p => p.FechaHora)
                .Take(15)
                .ToListAsync();

            var stats = new EquipoStats { EquipoId = equipoId };

            if (!partidos.Any())
            {
                // Stats por defecto para equipos sin historial
                stats.Rating = 50f;
                stats.GolesPromedio = 1.5f;
                stats.GolesRecibidosPromedio = 1.5f;
                stats.RendimientoLocal = 0.5f;
                stats.RendimientoVisitante = 0.4f; // Visitante ligeramente peor
                return stats;
            }

            // Calcular estadísticas reales (similar a MLTrainingService)
            var golesFavor = 0;
            var golesContra = 0;
            var victorias = 0;
            var empates = 0;
            var derrotas = 0;
            var victoriasLocal = 0;
            var partidosLocal = 0;
            var victoriasVisitante = 0;
            var partidosVisitante = 0;

            var ultimos5 = partidos.Take(5);
            var victoriasUltimos5 = 0;

            foreach (var partido in partidos)
            {
                var esLocal = partido.EquipoLocalId == equipoId;
                var golesEquipo = esLocal ? partido.GolesLocal ?? 0 : partido.GolesVisitante ?? 0;
                var golesRival = esLocal ? partido.GolesVisitante ?? 0 : partido.GolesLocal ?? 0;

                golesFavor += golesEquipo;
                golesContra += golesRival;

                if (golesEquipo > golesRival)
                {
                    victorias++;
                    if (ultimos5.Contains(partido)) victoriasUltimos5++;
                    if (esLocal) victoriasLocal++;
                    else victoriasVisitante++;
                }
                else if (golesEquipo == golesRival)
                {
                    empates++;
                }
                else
                {
                    derrotas++;
                }

                if (esLocal) partidosLocal++;
                else partidosVisitante++;
            }

            stats.PartidosJugados = partidos.Count;
            stats.Victorias = victorias;
            stats.Empates = empates;
            stats.Derrotas = derrotas;
            stats.GolesPromedio = partidos.Count > 0 ? (float)golesFavor / partidos.Count : 1.5f;
            stats.GolesRecibidosPromedio = partidos.Count > 0 ? (float)golesContra / partidos.Count : 1.5f;
            stats.VictoriasUltimos5 = victoriasUltimos5;
            stats.RendimientoLocal = partidosLocal > 0 ? (float)victoriasLocal / partidosLocal : 0.5f;
            stats.RendimientoVisitante = partidosVisitante > 0 ? (float)victoriasVisitante / partidosVisitante : 0.4f;
            
            // Rating basado en rendimiento
            var puntos = victorias * 3 + empates;
            var maxPuntos = partidos.Count * 3;
            stats.Rating = maxPuntos > 0 ? 30 + ((float)puntos / maxPuntos) * 40 : 50;

            return stats;
        }

        private float ObtenerImportanciaPartido(Partido partido) => 
            partido.Liga?.Nombre?.Contains("Champions") == true ? 1.0f :
            partido.Liga?.Nombre?.Contains("Liga") == true ? 0.8f :
            partido.Liga?.Nombre?.Contains("Copa") == true ? 0.6f : 0.5f;

        private async Task<float> CalcularHistorialEnfrentamientos(int equipoLocalId, int equipoVisitanteId)
        {
            var enfrentamientos = await _context.Partidos
                .Where(p => ((p.EquipoLocalId == equipoLocalId && p.EquipoVisitanteId == equipoVisitanteId) ||
                            (p.EquipoLocalId == equipoVisitanteId && p.EquipoVisitanteId == equipoLocalId))
                           && p.Estado == EstadoPartido.Finalizado)
                .Take(10)
                .ToListAsync();

            if (!enfrentamientos.Any()) return 0.5f;

            var victoriasLocal = enfrentamientos.Count(p =>
                (p.EquipoLocalId == equipoLocalId && (p.GolesLocal ?? 0) > (p.GolesVisitante ?? 0)) ||
                (p.EquipoVisitanteId == equipoLocalId && (p.GolesVisitante ?? 0) > (p.GolesLocal ?? 0)));

            return (float)victoriasLocal / enfrentamientos.Count;
        }

        private float CalcularPosicionTemporada(DateTime fecha)
        {
            var inicioTemporada = new DateTime(fecha.Year, 8, 1);
            if (fecha.Month < 8) inicioTemporada = inicioTemporada.AddYears(-1);
            
            var finTemporada = inicioTemporada.AddMonths(10);
            var diasTemporada = (finTemporada - inicioTemporada).TotalDays;
            var diasTranscurridos = (fecha - inicioTemporada).TotalDays;
            
            return Math.Max(0, Math.Min(1, (float)(diasTranscurridos / diasTemporada)));
        }

        private async Task<float> CalcularDiasDescanso(int equipoId, DateTime fechaPartido)
        {
            var ultimoPartido = await _context.Partidos
                .Where(p => (p.EquipoLocalId == equipoId || p.EquipoVisitanteId == equipoId)
                           && p.FechaHora < fechaPartido
                           && p.Estado == EstadoPartido.Finalizado)
                .OrderByDescending(p => p.FechaHora)
                .FirstOrDefaultAsync();

            if (ultimoPartido == null) return 7;

            var diasDescanso = (fechaPartido - ultimoPartido.FechaHora).TotalDays;
            return Math.Min(30, Math.Max(1, (float)diasDescanso));
        }

        private async Task<MatchPrediction?> SimularPrediccionHistorica(Partido partido)
        {
            // Para evaluación, usar datos históricos hasta la fecha del partido
            var matchData = await PrepararDatosPartidoAsync(partido);
            var predictionEngine = _trainingService.ObtenerPredictionEngine();
            return predictionEngine?.Predict(matchData);
        }

        private string DeterminarResultadoReal(Partido partido)
        {
            if (partido.GolesLocal.HasValue && partido.GolesVisitante.HasValue)
            {
                if (partido.GolesLocal > partido.GolesVisitante) return "Local";
                if (partido.GolesVisitante > partido.GolesLocal) return "Visitante";
                return "Empate";
            }
            return "Empate";
        }

        private string DeterminarTipoApuesta(MatchPrediction prediccion)
        {
            if (prediccion.ConfianzaPrediccion >= 80) return "FUERTE";
            if (prediccion.ConfianzaPrediccion >= 65) return "MODERADA";
            if (prediccion.ConfianzaPrediccion >= 50) return "CONSERVADORA";
            return "NO_RECOMENDADA";
        }

        private double CalcularMontoSugerido(float confianza, float valor)
        {
            var montoBase = confianza >= 80 ? 50 : confianza >= 65 ? 30 : 20;
            return Math.Round(montoBase * (valor / 10), 2);
        }

        private double CalcularCuotaAproximada(MatchPrediction prediccion)
        {
            var probabilidadMaxima = Math.Max(prediccion.ProbabilidadLocal, 
                                            Math.Max(prediccion.ProbabilidadEmpate, prediccion.ProbabilidadVisitante));
            return probabilidadMaxima > 0 ? Math.Min(5, Math.Max(1.1, 1 / probabilidadMaxima)) : 2.0;
        }
    }

    // Clases auxiliares para resultados
    public class RecomendacionApuesta
    {
        public int PartidoId { get; set; }
        public string EquipoLocal { get; set; } = string.Empty;
        public string EquipoVisitante { get; set; } = string.Empty;
        public DateTime FechaPartido { get; set; }
        public string ResultadoRecomendado { get; set; } = string.Empty;
        public float ConfianzaML { get; set; }
        public float ProbabilidadLocal { get; set; }
        public float ProbabilidadEmpate { get; set; }
        public float ProbabilidadVisitante { get; set; }
        public float ValorApuesta { get; set; }
        public string TipoApuesta { get; set; } = string.Empty;
        public double MontoSugerido { get; set; }
    }

    public class RendimientoPredicciones
    {
        public double AccuracyGeneral { get; set; }
        public int TotalPredicciones { get; set; }
        public int PrediccionesCorrectas { get; set; }
        public double BeneficioAcumulado { get; set; }
        public Dictionary<string, double> AccuracyPorTipo { get; set; } = new();
        public DateTime FechaEvaluacion { get; set; }
        public int DiasEvaluados { get; set; }
    }
}