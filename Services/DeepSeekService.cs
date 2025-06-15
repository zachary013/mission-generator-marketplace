using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SmartMarketplace.Configuration;
using SmartMarketplace.Models;

namespace SmartMarketplace.Services;

public class DeepSeekService : IDeepSeekService
{
    private readonly HttpClient _httpClient;
    private readonly AIConfig _config;
    private readonly ILogger<DeepSeekService> _logger;

    public DeepSeekService(HttpClient httpClient, IOptions<AIConfig> aiConfig, ILogger<DeepSeekService> logger)
    {
        _httpClient = httpClient;
        _config = aiConfig.Value;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.DeepSeek.ApiKey}");
        _httpClient.BaseAddress = new Uri(_config.DeepSeek.BaseUrl);
    }

    public async Task<Mission?> GenerateMissionAsync(string prompt)
    {
        try
        {
            var requestBody = new
            {
                inputs = prompt,
                parameters = new
                {
                    temperature = 0.7,
                    max_new_tokens = 2000,
                    return_full_text = false
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/{_config.DeepSeek.Model}", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("DeepSeek API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("DeepSeek API response: {Response}", responseContent);

            var mission = JsonSerializer.Deserialize<Mission>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (mission != null)
            {
                mission.Id = Guid.NewGuid().ToString();
                mission.CreatedAt = DateTime.UtcNow;
                return mission;
            }

            _logger.LogWarning("Failed to parse DeepSeek response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DeepSeek API: {Message}", ex.Message);
            return null;
        }
    }
}
