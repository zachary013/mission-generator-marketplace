using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartMarketplace.Configuration;
using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiConfig _config;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, IOptions<AIConfig> aiConfig, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _config = aiConfig.Value.Gemini;
        _logger = logger;
        
        // Don't set BaseAddress, we'll use full URLs
    }

    public async Task<Mission?> GenerateMissionAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"Tu es un expert en création de missions freelance. Réponds uniquement en JSON valide, sans commentaires ni texte supplémentaire.\n\n{prompt}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 2000,
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_config.BaseUrl}/models/{_config.Model}:generateContent?key={_config.ApiKey}", content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Content}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Gemini API response: {Response}", responseContent);

            using var document = JsonDocument.Parse(responseContent);
            
            if (document.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content_prop) &&
                    content_prop.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        // Clean the JSON response
                        text = text.Trim().Replace("```json", "").Replace("```", "").Trim();
                        
                        var mission = JsonSerializer.Deserialize<Mission>(text, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (mission != null)
                        {
                            mission.Id = Guid.NewGuid().ToString();
                            mission.CreatedAt = DateTime.UtcNow;
                            return mission;
                        }
                    }
                }
            }

            _logger.LogWarning("Failed to parse Gemini response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return null;
        }
    }
}
