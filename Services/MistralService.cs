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
        // Don't set BaseAddress, we'll use full URLs
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

            var response = await _httpClient.PostAsync($"{_config.BaseUrl}/chat/completions", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Mistral API error: {StatusCode} - {Content}", 
                    response.StatusCode, errorContent);
                _logger.LogError("Request URL: {Url}", response.RequestMessage?.RequestUri);
                _logger.LogError("Request Model: {Model}", _config.Model);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Mistral API response: {Response}", responseContent);

            using var document = JsonDocument.Parse(responseContent);
            
            if (document.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentProp))
                {
                    var missionJson = contentProp.GetString()?.Trim();
                    
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

                    if (mission != null)
                    {
                        mission.Id = Guid.NewGuid().ToString();
                        mission.CreatedAt = DateTime.UtcNow;
                        return mission;
                    }
                }
            }

            _logger.LogWarning("Failed to parse Mistral response");
            return null;
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
