using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartMarketplace.Configuration;
using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public class LlamaService : ILlamaService
{
    private readonly HttpClient _httpClient;
    private readonly LlamaConfig _config;
    private readonly ILogger<LlamaService> _logger;

    public LlamaService(HttpClient httpClient, IOptions<AIConfig> aiConfig, ILogger<LlamaService> logger)
    {
        _httpClient = httpClient;
        _config = aiConfig.Value.Llama;
        _logger = logger;
        
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
    }

    public async Task<Mission?> GenerateMissionAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                model = _config.Model,
                messages = new[]
                {
                    new { role = "system", content = "Tu es un expert en création de missions freelance. Réponds uniquement en JSON valide, sans commentaires ni texte supplémentaire." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Llama API error: {StatusCode} - {Content}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Llama API response: {Response}", responseContent);

            using var document = JsonDocument.Parse(responseContent);
            
            if (document.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content_prop))
                {
                    var text = content_prop.GetString();
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

            _logger.LogWarning("Failed to parse Llama response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Llama API");
            return null;
        }
    }
}
