using Grupo_negro.Data;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Grupo_negro.Services;

public class SemanticKernelService
{
    private readonly ILogger<SemanticKernelService> _logger;
    private readonly string _endpoint;
    private readonly string _modelName;
    private readonly HttpClient _httpClient;
    private readonly string _documentsPath;
    private List<DocumentChunk> _documentChunks;

    public SemanticKernelService(IConfiguration configuration, ILogger<SemanticKernelService> logger)
    {
        _logger = logger;
        _endpoint = configuration["SemanticKernel:Endpoint"] ?? "http://localhost:11434";
        _modelName = configuration["SemanticKernel:ModelName"] ?? "mistral:latest";
        _documentsPath = Path.Combine(Directory.GetCurrentDirectory(), "Documents");
        _documentChunks = new List<DocumentChunk>();

        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
        _httpClient.BaseAddress = new Uri(_endpoint);

        // Cargar documentos automáticamente al iniciar
        _ = InitializeDocumentsAsync();

        _logger.LogInformation("SemanticKernelService inicializado - Endpoint: {Endpoint}, Modelo: {ModelName}", _endpoint, _modelName);
    }

    private async Task InitializeDocumentsAsync()
    {
        try
        {
            _logger.LogInformation("Inicializando documentos desde: {Path}", _documentsPath);
            
            // Crear directorio si no existe
            if (!Directory.Exists(_documentsPath))
            {
                Directory.CreateDirectory(_documentsPath);
                _logger.LogInformation("Directorio Documents creado");
            }

            // Cargar todos los documentos disponibles
            await LoadAllDocumentsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inicializando documentos");
        }
    }

    public async Task LoadAllDocumentsAsync()
    {
        try
        {
            _documentChunks.Clear();

            // Cargar PDF de apuestas responsables
            var pdfPath = Path.Combine(_documentsPath, "guia_apuestas_responsables.pdf");
            if (File.Exists(pdfPath))
            {
                await TrainWithDocumentAsync("guia_apuestas_responsables.pdf");
            }
            else
            {
                _logger.LogWarning("PDF no encontrado: {PdfPath}", pdfPath);
            }

            // Cargar términos y condiciones
            var terminosPath = Path.Combine(_documentsPath, "terminos_condiciones.txt");
            if (File.Exists(terminosPath))
            {
                await TrainWithDocumentAsync("terminos_condiciones.txt");
            }

            // Cargar promociones
            var promocionesPath = Path.Combine(_documentsPath, "promociones_actuales.txt");
            if (File.Exists(promocionesPath))
            {
                await TrainWithDocumentAsync("promociones_actuales.txt");
            }

            _logger.LogInformation("Carga completa de documentos finalizada. Total chunks: {Count}", _documentChunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en LoadAllDocumentsAsync");
        }
    }

    public async Task TrainWithDocumentAsync(string documentName)
    {
        var documentPath = Path.Combine(_documentsPath, documentName);
        if (!File.Exists(documentPath))
        {
            _logger.LogWarning("Documento no encontrado: {Document}", documentName);
            return;
        }
        
        try
        {
            string content;
            if (documentName.EndsWith(".pdf"))
            {
                content = ExtractTextFromPdf(documentPath);
                _logger.LogInformation("PDF procesado: {Document}, caracteres: {Length}", documentName, content.Length);
            }
            else if (documentName.EndsWith(".txt"))
            {
                content = await File.ReadAllTextAsync(documentPath);
                _logger.LogInformation("TXT procesado: {Document}, caracteres: {Length}", documentName, content.Length);
            }
            else
            {
                _logger.LogWarning("Formato no soportado: {Document}", documentName);
                return;
            }
            
            var newChunks = SplitIntoChunks(content, 400);
            _documentChunks.AddRange(newChunks);
            
            _logger.LogInformation("Documento {Document} procesado: {Chunks} chunks agregados", 
                documentName, newChunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando documento: {Document}", documentName);
        }
    }

    // Método RAG mejorado
    public async Task<string> GetChatResponseWithRagAsync(string userMessage)
    {
        _logger.LogInformation("Enviando mensaje RAG a Ollama: {Message}", userMessage);
        
        try
        {
            // 1. Buscar chunks relevantes
            var relevantChunks = FindRelevantChunks(userMessage, _documentChunks, topK: 4);
            var context = string.Join("\n\n", relevantChunks);

            // 2. Crear prompt mejorado y específico
            var prompt = CreateEnhancedPrompt(userMessage, context);
            
            _logger.LogInformation("Contexto RAG: {Chunks} chunks, {Length} caracteres", 
                relevantChunks.Count, context.Length);
            
            // 3. Enviar a Ollama
            var requestBody = new
            {
                model = _modelName,
                stream = false,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };
            
            var json = JsonSerializer.Serialize(requestBody);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/chat", requestContent);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Respuesta RAG recibida de Ollama exitosamente");
                
                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? "Lo siento, no pude generar una respuesta.";
                }
            }
            else
            {
                _logger.LogError("Error en respuesta RAG de Ollama: {StatusCode}", response.StatusCode);
                return "Error temporal del servicio. Por favor, intenta nuevamente.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetChatResponseWithRagAsync");
            return "Ocurrió un error procesando tu solicitud. Por favor, contacta con soporte.";
        }
        
        return "No pude procesar tu pregunta en este momento.";
    }

    private string CreateEnhancedPrompt(string userMessage, string context)
    {
        return $"""
        Eres APUESTAKONG, el asistente oficial de la plataforma de apuestas deportivas. 
        Tu propósito es ayudar a usuarios con información sobre apuestas, promociones y juego responsable.

        INFORMACIÓN OFICIAL DE APUESTAKONG:
        {context}

        REGLAS ESTRICTAS DE RESPUESTA:
        1. IDENTIDAD: Siempre te presentas como APUESTAKONG
        2. TEMAS PERMITIDOS: 
           - Apuestas deportivas y cómo funcionan
           - Promociones y bonos activos
           - Términos y condiciones
           - Juego responsable y prevención de ludopatía
           - Información sobre deportes y mercados
        3. PROHIBICIONES ABSOLUTAS:
           - NUNCA des consejos específicos de apuestas
           - NUNCA predigas resultados de partidos
           - NUNCA sugieras que las apuestas son fuente de ingresos
           - NUNCA encourages aumentar presupuesto de apuestas
        4. FORMATO DE RESPUESTA:
           - Clara, directa y útil
           - Mención de límites y juego responsable cuando sea relevante
           - Información basada ÚNICAMENTE en la documentación proporcionada
           - Incluir números de contacto de ayuda si el usuario muestra señales de problema

        PREGUNTA DEL USUARIO: {userMessage}

        Por favor, responde de manera profesional y responsable:
        """;
    }

    // Métodos de utilidad para RAG
    private List<string> FindRelevantChunks(string query, List<DocumentChunk> chunks, int topK = 3)
    {
        if (!chunks.Any())
            return new List<string> { "Información no disponible en este momento." };

        var queryWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var scoredChunks = chunks.Select(chunk =>
        {
            var chunkText = chunk.Content.ToLower();
            var score = queryWords.Sum(word => chunkText.Contains(word) ? 2 : 0) +
                       queryWords.Sum(word => chunkText.Split(' ').Contains(word) ? 3 : 0);
            
            // Bonus por matches exactos en títulos
            if (chunkText.Contains("apuesta") && queryWords.Contains("apuesta")) score += 2;
            if (chunkText.Contains("bono") && queryWords.Contains("bono")) score += 2;
            if (chunkText.Contains("responsable") && queryWords.Contains("responsable")) score += 2;
            if (chunkText.Contains("depósito") && queryWords.Contains("depósito")) score += 1;
            
            return (chunk, score);
        })
        .Where(x => x.score > 0)
        .OrderByDescending(x => x.score)
        .Take(topK)
        .Select(x => x.chunk.Content)
        .ToList();
        
        return scoredChunks.Any() ? scoredChunks : chunks.Take(topK).Select(c => c.Content).ToList();
    }

    private List<DocumentChunk> SplitIntoChunks(string content, int chunkSize)
    {
        var chunks = new List<DocumentChunk>();
        if (string.IsNullOrEmpty(content))
            return chunks;

        var paragraphs = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();
        var chunkId = 1;

        foreach (var paragraph in paragraphs)
        {
            if (currentChunk.Length + paragraph.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(new DocumentChunk 
                { 
                    Id = chunkId++, 
                    Content = currentChunk.ToString().Trim() 
                });
                currentChunk.Clear();
            }
            currentChunk.AppendLine(paragraph);
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(new DocumentChunk 
            { 
                Id = chunkId, 
                Content = currentChunk.ToString().Trim() 
            });
        }

        return chunks;
    }

    private static string ExtractTextFromPdf(string pdfPath)
    {
        var text = new StringBuilder();

        try
        {
            using (var document = PdfDocument.Open(pdfPath))
            {
                foreach (var page in document.GetPages())
                {
                    var pageText = ContentOrderTextExtractor.GetText(page);
                    text.AppendLine(pageText);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error extrayendo texto del PDF: {ex.Message}", ex);
        }

        return text.ToString();
    }

    // Métodos originales mantenidos para compatibilidad
    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        _logger.LogInformation("Enviando mensaje básico a Ollama: {Message}", userMessage);
        try
        {
            var requestBody = new
            {
                model = _modelName,
                stream = false,
                messages = new[]
                {
                    new { role = "user", content = userMessage }
                }
            };
            
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/chat", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                
                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? "Sin respuesta";
                }
            }
            else
            {
                _logger.LogError("Error en respuesta de Ollama: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener respuesta básica de Ollama");
        }
        
        return "Error al comunicarse con el servicio";
    }

    public async Task<string> GetChatResponseWithHistoryAsync(string userMessage, IEnumerable<ChatMessage> messages)
    {
        _logger.LogInformation("Enviando mensaje con historial a Ollama: {Message}", userMessage);
        try
        {
            var messagesList = new List<object>();
            
            foreach (var message in messages)
            {
                var role = message.Sender == "User" ? "user" : "assistant";
                messagesList.Add(new { role = role, content = message.Message });
            }
            
            messagesList.Add(new { role = "user", content = userMessage });
            
            var requestBody = new
            {
                model = _modelName,
                stream = false,
                messages = messagesList.ToArray()
            };
            
            var json = JsonSerializer.Serialize(requestBody);
            var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/api/chat", requestContent);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                
                using var doc = JsonDocument.Parse(responseContent);
                if (doc.RootElement.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? "Sin respuesta";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener respuesta con historial de Ollama");
        }
        
        return "Error al comunicarse con el servicio";
    }

    // Método para testing del sistema RAG
    public async Task<string> TestRagSystemAsync()
    {
        var testQuestions = new[]
        {
            "¿Qué bonos de bienvenida tienen?",
            "¿Cómo puedo apostar responsablemente?",
            "¿Cuál es el depósito mínimo?",
            "¿Dónde busco ayuda si tengo problemas con el juego?",
            "¿Qué deportes están disponibles para apostar?"
        };
        
        var results = new StringBuilder();
        results.AppendLine("=== TEST DEL SISTEMA RAG ===");
        results.AppendLine($"Documentos cargados: {_documentChunks.Count} chunks");
        results.AppendLine();
        
        foreach (var question in testQuestions)
        {
            results.AppendLine($"P: {question}");
            var response = await GetChatResponseWithRagAsync(question);
            results.AppendLine($"R: {response}");
            results.AppendLine("---");
            await Task.Delay(500); // Pequeña pausa entre requests
        }
        
        return results.ToString();
    }

    public int GetLoadedChunksCount() => _documentChunks.Count;
    public List<string> GetLoadedDocumentNames() => _documentChunks.Select(c => c.Source).Distinct().ToList();
}

public class DocumentChunk
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}