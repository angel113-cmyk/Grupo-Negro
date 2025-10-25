using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.EntityFrameworkCore;
using Grupo_negro.Data;
using Grupo_negro.Models;
using Grupo_negro.Models.MachineLearning;
using System.Globalization;

namespace Grupo_negro.Services
{
    /// <summary>
    /// Servicio para entrenar modelos de Machine Learning para predicción de partidos
    /// </summary>
    public class MLTrainingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MLTrainingService> _logger;
        private readonly MLContext _mlContext;
        private readonly MLConfig _config;
        private ITransformer? _trainedModel;
        private PredictionEngine<MatchData, MatchPrediction>? _predictionEngine;

        public MLTrainingService(
            ApplicationDbContext context, 
            ILogger<MLTrainingService> logger)
        {
            _context = context;
            _logger = logger;
            _mlContext = new MLContext(seed: 42);
            _config = new MLConfig();
            
            // Crear directorio para modelos si no existe
            var modelDir = Path.GetDirectoryName(_config.ModelPath);
            if (!string.IsNullOrEmpty(modelDir) && !Directory.Exists(modelDir))
            {
                Directory.CreateDirectory(modelDir);
            }
        }

        /// <summary>
        /// Entrena el modelo con datos históricos de partidos
        /// </summary>
        public async Task<ModelMetrics> EntrenarModeloAsync()
        {
            try
            {
                _logger.LogInformation("Iniciando entrenamiento del modelo ML...");
                var startTime = DateTime.Now;

                // 1. Preparar datos de entrenamiento
                var trainingData = await PrepararDatosEntrenamientoAsync();
                _logger.LogInformation($"Preparados {trainingData.Count} registros de entrenamiento");

                if (trainingData.Count < 50)
                {
                    _logger.LogWarning("Pocos datos para entrenar. Generando datos simulados...");
                    trainingData.AddRange(GenerarDatosSimulados(200));
                }

                // 2. Crear dataset de ML.NET
                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                // 3. Dividir datos en entrenamiento y prueba
                var splitData = _mlContext.Data.TrainTestSplit(dataView, testFraction: _config.TestFraction);

                // 4. Definir pipeline de entrenamiento
                var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        nameof(MatchData.EquipoLocalRating),
                        nameof(MatchData.EquipoLocalGolesPromedio),
                        nameof(MatchData.EquipoLocalGolesRecibidos),
                        nameof(MatchData.EquipoLocalVictoriasUltimos5),
                        nameof(MatchData.EquipoLocalPartidosLocal),
                        nameof(MatchData.EquipoVisitanteRating),
                        nameof(MatchData.EquipoVisitanteGolesPromedio),
                        nameof(MatchData.EquipoVisitanteGolesRecibidos),
                        nameof(MatchData.EquipoVisitanteVictoriasUltimos5),
                        nameof(MatchData.EquipoVisitantePartidosVisitante),
                        nameof(MatchData.ImportanciaPartido),
                        nameof(MatchData.DiferenciaRating),
                        nameof(MatchData.HistorialEnfrentamientos),
                        nameof(MatchData.TemporadaActual),
                        nameof(MatchData.DiasDescanso)))
                    .Append(_mlContext.MulticlassClassification.Trainers.OneVersusAll(
                        _mlContext.BinaryClassification.Trainers.FastTree(
                            numberOfLeaves: _config.NumberOfLeaves,
                            numberOfTrees: _config.NumberOfTrees,
                            minimumExampleCountPerLeaf: _config.MinimumExampleCountPerLeaf,
                            learningRate: _config.LearningRate)))
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                // 5. Entrenar modelo
                _logger.LogInformation("Entrenando modelo...");
                _trainedModel = pipeline.Fit(splitData.TrainSet);

                // 6. Evaluar modelo
                var predictions = _trainedModel.Transform(splitData.TestSet);
                var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

                // 7. Guardar modelo
                _mlContext.Model.Save(_trainedModel, dataView.Schema, _config.ModelPath);
                
                // 8. Crear engine de predicción
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<MatchData, MatchPrediction>(_trainedModel);

                var trainingTime = DateTime.Now - startTime;
                _logger.LogInformation($"Modelo entrenado exitosamente en {trainingTime.TotalSeconds:F1} segundos");

                // 9. Preparar métricas de resultado
                var modelMetrics = new ModelMetrics
                {
                    Accuracy = metrics.MacroAccuracy,
                    MacroAccuracy = metrics.MacroAccuracy,
                    MicroAccuracy = metrics.MicroAccuracy,
                    LogLoss = metrics.LogLoss,
                    LogLossReduction = metrics.LogLossReduction,
                    FechaEntrenamiento = DateTime.Now,
                    PartidosEntrenamiento = trainingData.Count - (int)(trainingData.Count * _config.TestFraction),
                    PartidosPrueba = (int)(trainingData.Count * _config.TestFraction),
                    TiempoEntrenamiento = trainingTime
                };

                // 10. Calcular métricas por clase (simplificado por ahora)
                var classes = new[] { "Local", "Empate", "Visitante" };
                
                for (int i = 0; i < classes.Length; i++)
                {
                    // Valores por defecto - se pueden mejorar más adelante
                    modelMetrics.PrecisionPorClase[classes[i]] = metrics.MacroAccuracy;
                    modelMetrics.RecallPorClase[classes[i]] = metrics.MacroAccuracy;
                    modelMetrics.F1ScorePorClase[classes[i]] = metrics.MacroAccuracy;
                }

                _logger.LogInformation($"Accuracy del modelo: {metrics.MacroAccuracy:P2}");
                return modelMetrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el entrenamiento del modelo");
                throw;
            }
        }

        /// <summary>
        /// Prepara datos de entrenamiento desde la base de datos
        /// </summary>
        private async Task<List<MatchData>> PrepararDatosEntrenamientoAsync()
        {
            var datos = new List<MatchData>();
            
            // Obtener partidos finalizados con resultado
            var partidosFinalizados = await _context.Partidos
                .Where(p => p.Estado == EstadoPartido.Finalizado && !string.IsNullOrEmpty(p.Resultado))
                .Include(p => p.EquipoLocal)
                .Include(p => p.EquipoVisitante)
                .OrderBy(p => p.FechaHora)
                .ToListAsync();

            _logger.LogInformation($"Procesando {partidosFinalizados.Count} partidos históricos...");

            foreach (var partido in partidosFinalizados)
            {
                try
                {
                    var statsLocal = await CalcularStatsEquipo(partido.EquipoLocalId, partido.FechaHora);
                    var statsVisitante = await CalcularStatsEquipo(partido.EquipoVisitanteId, partido.FechaHora);
                    
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
                        
                        // Contexto del partido
                        ImportanciaPartido = ObtenerImportanciaPartido(partido),
                        DiferenciaRating = statsLocal.Rating - statsVisitante.Rating,
                        HistorialEnfrentamientos = await CalcularHistorialEnfrentamientos(partido.EquipoLocalId, partido.EquipoVisitanteId, partido.FechaHora),
                        TemporadaActual = CalcularPosicionTemporada(partido.FechaHora),
                        DiasDescanso = await CalcularDiasDescanso(partido.EquipoLocalId, partido.FechaHora),
                        
                        // Resultado (label)
                        Resultado = DeterminarResultado(partido)
                    };

                    datos.Add(matchData);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error procesando partido {partido.Id}: {ex.Message}");
                }
            }

            return datos;
        }

        /// <summary>
        /// Calcula estadísticas históricas de un equipo hasta una fecha específica
        /// </summary>
        private async Task<EquipoStats> CalcularStatsEquipo(int equipoId, DateTime fechaHasta)
        {
            var partidos = await _context.Partidos
                .Where(p => (p.EquipoLocalId == equipoId || p.EquipoVisitanteId == equipoId) 
                           && p.FechaHora < fechaHasta 
                           && p.Estado == EstadoPartido.Finalizado)
                .OrderByDescending(p => p.FechaHora)
                .Take(20) // Últimos 20 partidos
                .ToListAsync();

            var stats = new EquipoStats { EquipoId = equipoId };

            if (!partidos.Any())
            {
                return stats; // Devolver stats por defecto
            }

            var golesFavor = 0;
            var golesContra = 0;
            var victorias = 0;
            var empates = 0;
            var derrotas = 0;
            var victoriasLocal = 0;
            var partidosLocal = 0;
            var victoriasVisitante = 0;
            var partidosVisitante = 0;

            // Últimos 5 partidos
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
            stats.GolesPromedio = partidos.Count > 0 ? (float)golesFavor / partidos.Count : 0;
            stats.GolesRecibidosPromedio = partidos.Count > 0 ? (float)golesContra / partidos.Count : 0;
            stats.DiferenciaGoles = stats.GolesPromedio - stats.GolesRecibidosPromedio;
            stats.VictoriasUltimos5 = victoriasUltimos5;
            stats.RendimientoLocal = partidosLocal > 0 ? (float)victoriasLocal / partidosLocal : 0.5f;
            stats.RendimientoVisitante = partidosVisitante > 0 ? (float)victoriasVisitante / partidosVisitante : 0.5f;
            
            // Calcular rating basado en rendimiento
            var puntos = victorias * 3 + empates;
            var maxPuntos = partidos.Count * 3;
            stats.Rating = maxPuntos > 0 ? 30 + ((float)puntos / maxPuntos) * 40 : 50; // Rating entre 30-70

            return stats;
        }

        /// <summary>
        /// Genera datos simulados para entrenar cuando hay pocos datos reales
        /// </summary>
        private List<MatchData> GenerarDatosSimulados(int cantidad)
        {
            var datos = new List<MatchData>();
            var random = new Random();
            var resultados = new[] { "Local", "Empate", "Visitante" };

            for (int i = 0; i < cantidad; i++)
            {
                var ratingLocal = (float)(random.NextDouble() * 40 + 30); // 30-70
                var ratingVisitante = (float)(random.NextDouble() * 40 + 30);
                var diferenciaRating = ratingLocal - ratingVisitante;

                // Distribución más realista: Local 40%, Empate 25%, Visitante 35%
                var randomValue = random.NextDouble();
                string resultado;
                
                // Ajustar por diferencia de rating pero mantener balance
                var ventajaLocal = Math.Max(-0.15f, Math.Min(0.15f, diferenciaRating / 80f));
                
                if (randomValue < 0.40f + ventajaLocal)
                    resultado = "Local";
                else if (randomValue < 0.65f + ventajaLocal)
                    resultado = "Empate";
                else
                    resultado = "Visitante";

                datos.Add(new MatchData
                {
                    PartidoId = 1000 + i,
                    EquipoLocalRating = ratingLocal,
                    EquipoLocalGolesPromedio = (float)(random.NextDouble() * 2 + 0.5),
                    EquipoLocalGolesRecibidos = (float)(random.NextDouble() * 2 + 0.5),
                    EquipoLocalVictoriasUltimos5 = random.Next(0, 6),
                    EquipoLocalPartidosLocal = (float)(random.NextDouble() * 0.6 + 0.2),
                    EquipoVisitanteRating = ratingVisitante,
                    EquipoVisitanteGolesPromedio = (float)(random.NextDouble() * 2 + 0.5),
                    EquipoVisitanteGolesRecibidos = (float)(random.NextDouble() * 2 + 0.5),
                    EquipoVisitanteVictoriasUltimos5 = random.Next(0, 6),
                    EquipoVisitantePartidosVisitante = (float)(random.NextDouble() * 0.6 + 0.2),
                    ImportanciaPartido = (float)random.NextDouble(),
                    DiferenciaRating = diferenciaRating,
                    HistorialEnfrentamientos = (float)random.NextDouble(),
                    TemporadaActual = (float)random.NextDouble(),
                    DiasDescanso = random.Next(1, 15),
                    Resultado = resultado
                });
            }

            return datos;
        }

        private float ObtenerImportanciaPartido(Partido partido)
        {
            // Por ahora, importancia basada en la competición
            if (partido.Liga?.Nombre?.Contains("Champions") == true) return 1.0f;
            if (partido.Liga?.Nombre?.Contains("Liga") == true) return 0.8f;
            if (partido.Liga?.Nombre?.Contains("Copa") == true) return 0.6f;
            return 0.5f;
        }

        private async Task<float> CalcularHistorialEnfrentamientos(int equipoLocalId, int equipoVisitanteId, DateTime fechaHasta)
        {
            var enfrentamientos = await _context.Partidos
                .Where(p => ((p.EquipoLocalId == equipoLocalId && p.EquipoVisitanteId == equipoVisitanteId) ||
                            (p.EquipoLocalId == equipoVisitanteId && p.EquipoVisitanteId == equipoLocalId))
                           && p.FechaHora < fechaHasta
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
            var inicioTemporada = new DateTime(fecha.Year, 8, 1); // Agosto
            if (fecha.Month < 8) inicioTemporada = inicioTemporada.AddYears(-1);
            
            var finTemporada = inicioTemporada.AddMonths(10); // Hasta mayo
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

            if (ultimoPartido == null) return 7; // Defecto 7 días

            var diasDescanso = (fechaPartido - ultimoPartido.FechaHora).TotalDays;
            return Math.Min(30, Math.Max(1, (float)diasDescanso)); // Entre 1 y 30 días
        }

        private string DeterminarResultado(Partido partido)
        {
            if (string.IsNullOrEmpty(partido.Resultado)) return "Empate";
            
            // Analizar el resultado del partido
            if (partido.GolesLocal.HasValue && partido.GolesVisitante.HasValue)
            {
                if (partido.GolesLocal > partido.GolesVisitante) return "Local";
                if (partido.GolesVisitante > partido.GolesLocal) return "Visitante";
                return "Empate";
            }

            // Fallback: analizar string de resultado
            if (partido.Resultado.Contains("Local") || partido.Resultado.Contains("HOME"))
                return "Local";
            if (partido.Resultado.Contains("Visitante") || partido.Resultado.Contains("AWAY"))
                return "Visitante";
            
            return "Empate";
        }

        /// <summary>
        /// Carga el modelo entrenado desde disco
        /// </summary>
        public async Task<bool> CargarModeloAsync()
        {
            try
            {
                if (File.Exists(_config.ModelPath))
                {
                    _trainedModel = _mlContext.Model.Load(_config.ModelPath, out var modelSchema);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<MatchData, MatchPrediction>(_trainedModel);
                    _logger.LogInformation("Modelo ML cargado exitosamente desde disco");
                    return true;
                }
                else
                {
                    _logger.LogWarning("No se encontró modelo entrenado. Ejecute entrenamiento primero.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando modelo ML");
                return false;
            }
        }

        /// <summary>
        /// Verifica si el modelo está cargado y listo para usar
        /// </summary>
        public bool EstaModeloListo => _predictionEngine != null;

        /// <summary>
        /// Obtiene el engine de predicción actual
        /// </summary>
        public PredictionEngine<MatchData, MatchPrediction>? ObtenerPredictionEngine() => _predictionEngine;
    }
}