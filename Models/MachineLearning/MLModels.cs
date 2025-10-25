using Microsoft.ML.Data;

namespace Grupo_negro.Models.MachineLearning
{
    /// <summary>
    /// Modelo de datos de entrada para entrenar el algoritmo de predicción
    /// Contiene las características (features) de un partido
    /// </summary>
    public class MatchData
    {
        // Identificadores
        public float PartidoId { get; set; }
        
        // Características del equipo local
        [LoadColumn(0)]
        public float EquipoLocalRating { get; set; } // Rating promedio del equipo (0-100)
        
        [LoadColumn(1)]
        public float EquipoLocalGolesPromedio { get; set; } // Goles promedio por partido
        
        [LoadColumn(2)]
        public float EquipoLocalGolesRecibidos { get; set; } // Goles recibidos promedio
        
        [LoadColumn(3)]
        public float EquipoLocalVictoriasUltimos5 { get; set; } // Victorias en últimos 5 partidos (0-5)
        
        [LoadColumn(4)]
        public float EquipoLocalPartidosLocal { get; set; } // Rendimiento jugando de local (0-1)
        
        // Características del equipo visitante
        [LoadColumn(5)]
        public float EquipoVisitanteRating { get; set; }
        
        [LoadColumn(6)]
        public float EquipoVisitanteGolesPromedio { get; set; }
        
        [LoadColumn(7)]
        public float EquipoVisitanteGolesRecibidos { get; set; }
        
        [LoadColumn(8)]
        public float EquipoVisitanteVictoriasUltimos5 { get; set; }
        
        [LoadColumn(9)]
        public float EquipoVisitantePartidosVisitante { get; set; } // Rendimiento jugando de visitante
        
        // Contexto del partido
        [LoadColumn(10)]
        public float ImportanciaPartido { get; set; } // 0-1 (Liga, Copa, etc.)
        
        [LoadColumn(11)]
        public float DiferenciaRating { get; set; } // Diferencia de rating entre equipos
        
        [LoadColumn(12)]
        public float HistorialEnfrentamientos { get; set; } // Resultado histórico entre equipos (0-1)
        
        // Resultado del partido (etiqueta para entrenar)
        [LoadColumn(13), ColumnName("Label")]
        public string Resultado { get; set; } = string.Empty; // "Local", "Empate", "Visitante"
        
        // Características adicionales para mejor predicción
        [LoadColumn(14)]
        public float TemporadaActual { get; set; } // Posición en la temporada (0-1)
        
        [LoadColumn(15)]
        public float DiasDescanso { get; set; } // Días desde último partido
    }

    /// <summary>
    /// Resultado de la predicción del modelo
    /// </summary>
    public class MatchPrediction
    {
        // Predicción principal
        [ColumnName("PredictedLabel")]
        public string ResultadoPredicho { get; set; } = string.Empty;
        
        // Probabilidades para cada resultado
        [ColumnName("Score")]
        public float[] Probabilidades { get; set; } = new float[3];
        
        // Propiedades calculadas para fácil acceso
        public float ProbabilidadLocal => Probabilidades?.Length > 0 ? Probabilidades[0] : 0f;
        public float ProbabilidadEmpate => Probabilidades?.Length > 1 ? Probabilidades[1] : 0f;
        public float ProbabilidadVisitante => Probabilidades?.Length > 2 ? Probabilidades[2] : 0f;
        
        // Confianza de la predicción (0-100)
        public float ConfianzaPrediccion { get; set; }
        
        // Recomendación de apuesta
        public string RecomendacionApuesta { get; set; } = string.Empty;
        public float ValorApuesta { get; set; } // Valor esperado de la apuesta (0-10)
    }

    /// <summary>
    /// Estadísticas de un equipo para usar en ML
    /// </summary>
    public class EquipoStats
    {
        public int EquipoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        
        // Estadísticas generales
        public float Rating { get; set; } = 50f; // Rating inicial
        public int PartidosJugados { get; set; }
        public int Victorias { get; set; }
        public int Empates { get; set; }
        public int Derrotas { get; set; }
        
        // Estadísticas de goles
        public float GolesPromedio { get; set; }
        public float GolesRecibidosPromedio { get; set; }
        public float DiferenciaGoles { get; set; }
        
        // Forma actual (últimos 5 partidos)
        public int VictoriasUltimos5 { get; set; }
        public int EmpatesUltimos5 { get; set; }
        public int DerrotasUltimos5 { get; set; }
        
        // Rendimiento casa/visitante
        public float RendimientoLocal { get; set; } // Porcentaje de puntos ganados de local
        public float RendimientoVisitante { get; set; } // Porcentaje de puntos ganados de visitante
        
        // Estadísticas avanzadas
        public DateTime UltimaActualizacion { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Configuración para el entrenamiento del modelo
    /// </summary>
    public class MLConfig
    {
        // Rutas de archivos
        public string ModelPath { get; set; } = "MLModels/football_predictor.zip";
        public string DataPath { get; set; } = "Data/training_data.csv";
        
        // Parámetros de entrenamiento
        public int NumberOfTrees { get; set; } = 100; // Para FastTree
        public int NumberOfLeaves { get; set; } = 20;
        public int MinimumExampleCountPerLeaf { get; set; } = 10;
        public double LearningRate { get; set; } = 0.2;
        
        // Validación
        public float TestFraction { get; set; } = 0.2f; // 20% para testing
        public int RandomSeed { get; set; } = 42;
        
        // Umbrales de confianza
        public float ConfianzaMinima { get; set; } = 0.6f; // Confianza mínima para recomendar apuesta
        public float ProbabilidadMinima { get; set; } = 0.4f; // Probabilidad mínima para considerar resultado
    }

    /// <summary>
    /// Métricas de evaluación del modelo
    /// </summary>
    public class ModelMetrics
    {
        public double Accuracy { get; set; }
        public double MacroAccuracy { get; set; }
        public double MicroAccuracy { get; set; }
        public double LogLoss { get; set; }
        public double LogLossReduction { get; set; }
        
        // Métricas por clase
        public Dictionary<string, double> PrecisionPorClase { get; set; } = new();
        public Dictionary<string, double> RecallPorClase { get; set; } = new();
        public Dictionary<string, double> F1ScorePorClase { get; set; } = new();
        
        // Información del entrenamiento
        public DateTime FechaEntrenamiento { get; set; } = DateTime.Now;
        public int PartidosEntrenamiento { get; set; }
        public int PartidosPrueba { get; set; }
        public TimeSpan TiempoEntrenamiento { get; set; }
        
        // Validación
        public bool EsModeloValido => Accuracy > 0.5 && LogLoss < 1.5;
    }
}