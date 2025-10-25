using Grupo_negro.Models;
using Grupo_negro.Models.MachineLearning;
using Grupo_negro.Services;

namespace Grupo_negro.ViewModels
{
    /// <summary>
    /// ViewModel para el dashboard principal de Machine Learning
    /// </summary>
    public class MLDashboardViewModel
    {
        public bool EstaModeloEntrenado { get; set; }
        public int TotalPartidosHistoricos { get; set; }
        public int PartidosProximos { get; set; }
        public double AccuracyModelo { get; set; }
        public List<RecomendacionApuesta> RecomendacionesRecientes { get; set; } = new();
        public RendimientoPredicciones? RendimientoActual { get; set; }
    }

    /// <summary>
    /// ViewModel para la vista de predicciones
    /// </summary>
    public class PrediccionesViewModel
    {
        public List<Partido> PartidosProximos { get; set; } = new();
        public Dictionary<int, MatchPrediction> Predicciones { get; set; } = new();
        public bool ModeloDisponible { get; set; }
    }

    /// <summary>
    /// ViewModel para recomendaciones de apuestas
    /// </summary>
    public class RecomendacionesViewModel
    {
        public List<RecomendacionApuesta> Recomendaciones { get; set; } = new();
        public int TotalRecomendaciones { get; set; }
        public int RecomendacionesFuertes { get; set; }
        public int RecomendacionesModeradas { get; set; }
        public DateTime FechaGeneracion { get; set; }
    }

    /// <summary>
    /// ViewModel para análisis de rendimiento
    /// </summary>
    public class RendimientoViewModel
    {
        public RendimientoPredicciones Rendimiento { get; set; } = new();
        public int DiasAnalisis { get; set; }
    }
}