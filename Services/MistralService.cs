using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartMarketplace.Configuration;
using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public class MistralService : IMistralService
{
    private readonly HttpClient _httpClient;
    private readonly MistralConfig _config;
    private readonly ILogger<MistralService> _logger;

    public MistralService(HttpClient httpClient, IOptions<AIConfig> aiConfig, ILogger<MistralService> logger)
    {
        _httpClient = httpClient;
        _config = aiConfig.Value.Mistral;
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
                _logger.LogError("Mistral API error: {StatusCode} - {Content}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var mistralResponse = JsonSerializer.Deserialize<MistralApiResponse>(responseContent);
            
            if (mistralResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
            {
                _logger.LogError("Invalid Mistral response structure");
                return null;
            }

            var missionJson = mistralResponse.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
            
            if (string.IsNullOrEmpty(missionJson))
            {
                _logger.LogWarning("Empty response from Mistral API");
                return null;
            }
            
            // Clean JSON if it contains markdown formatting
            if (missionJson.StartsWith("```json"))
            {
                missionJson = missionJson.Replace("```json", "").Replace("```", "").Trim();
            }

            var mission = JsonSerializer.Deserialize<Mission>(missionJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return mission;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mission with Mistral");
            return null;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_config.ApiKey))
                return false;

            var response = await _httpClient.GetAsync("/models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private class MistralApiResponse
    {
        public Choice[]? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }
}
