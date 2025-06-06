using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartMarketplace.Services
{
    public class GrokService : IGrokService
    {
        private readonly HttpClient _httpClient;
        private const string ApiUrl = "https://api.x.ai/v1/chat/completions";
        private const string ApiKey = "xai-y7lIUR23aSbFKStNYfGqdnW56WwfQTEDxghnEIPpXfvNE8qnWaIs8hbAemsNKGCeYTLON1vOnUOyxboZ";

        public GrokService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> CallGrokApiAsync(string prompt)
        {
            try
            {
                // Create request payload
                var requestData = new
                {
                    model = "grok-3",
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 500
                };

                // Serialize the request data to JSON
                var jsonContent = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the request to the API
                var response = await _httpClient.PostAsync(ApiUrl, content);

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read and parse the response
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(jsonResponse);
                    
                    // Extract the response text from choices[0].message.content
                    var root = document.RootElement;
                    if (root.TryGetProperty("choices", out var choices) && 
                        choices.GetArrayLength() > 0 &&
                        choices[0].TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var messageContent))
                    {
                        return messageContent.GetString();
                    }
                    
                    return "No valid response content found";
                }
                else
                {
                    // Handle unsuccessful response
                    return $"Error calling Grok API: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return $"Error calling Grok API: {ex.Message}";
            }
        }
    }
}
