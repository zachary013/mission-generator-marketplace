using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using SmartMarketplace.Models;

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
                    max_tokens = 1000
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
        
        public async Task<Mission> GenerateFullMissionAsync(string simpleInput, ExtractedInformation extractedInfo)
        {
            try
            {
                // Build a detailed prompt that respects the extracted values
                string expertisesJson = JsonSerializer.Serialize(extractedInfo.Expertises);
                string titlePart = string.IsNullOrEmpty(extractedInfo.Title) ? "" : $"Title={extractedInfo.Title}, ";
                string positionPart = string.IsNullOrEmpty(extractedInfo.Position) ? "" : $"Position={extractedInfo.Position}, ";
                string domainPart = string.IsNullOrEmpty(extractedInfo.Domain) ? "" : $"Domain={extractedInfo.Domain}, ";
                
                string prompt = $@"
                Generate a detailed freelance mission based on these extracted details: 
                {titlePart}Country={extractedInfo.Country}, City={extractedInfo.City}, 
                WorkMode={extractedInfo.WorkMode}, Duration={extractedInfo.Duration} {extractedInfo.DurationType}, 
                EstimatedDailyRate={extractedInfo.Salary} {extractedInfo.Currency}, 
                {positionPart}{domainPart}ExperienceYear={extractedInfo.Experience}, 
                ContractType={extractedInfo.ContractType}, RequiredExpertises={expertisesJson}.
                
                Original input: '{simpleInput}'
                
                IMPORTANT: You MUST respect ALL the extracted values exactly as provided. Do not change or override:
                - The city name (e.g., if 'Rabat' is specified, don't change it to 'Casablanca')
                - The salary amount (e.g., if '7000' is specified, use exactly that value)
                - The work mode (REMOTE, ONSITE, HYBRID)
                - The duration and duration type
                - Any other explicitly provided values
                
                For missing fields, generate reasonable values based on the role and context:
                - Create a professional title if not provided
                - Generate a detailed technical description including:
                  * Project context
                  * Developer responsibilities
                  * Expected deliverables
                  * Technical stack details
                - Add relevant required expertise if not enough are provided
                
                Return ONLY a valid JSON object with the following structure:
                {{
                  ""title"": ""Professional title"",
                  ""description"": ""Detailed description"",
                  ""country"": ""{extractedInfo.Country}"",
                  ""city"": ""{extractedInfo.City}"",
                  ""workMode"": ""{extractedInfo.WorkMode}"",
                  ""duration"": {extractedInfo.Duration},
                  ""durationType"": ""{extractedInfo.DurationType}"",
                  ""startImmediately"": true,
                  ""experienceYear"": ""{extractedInfo.Experience}"",
                  ""contractType"": ""{extractedInfo.ContractType}"",
                  ""estimatedDailyRate"": {extractedInfo.Salary},
                  ""domain"": ""Domain field"",
                  ""position"": ""Position title"",
                  ""requiredExpertises"": [""expertise1"", ""expertise2"", ...]
                }}";

                // Call the API
                string grokResponse = await CallGrokApiAsync(prompt);
                
                // Check if response is valid
                if (!string.IsNullOrWhiteSpace(grokResponse) && !grokResponse.StartsWith("Error"))
                {
                    try
                    {
                        // Extract JSON from response (in case there's additional text)
                        string jsonContent = ExtractJsonFromResponse(grokResponse);
                        
                        // Deserialize the JSON response
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var generatedMission = JsonSerializer.Deserialize<Mission>(jsonContent, options);
                        
                        // Ensure the extracted values are preserved
                        if (generatedMission != null)
                        {
                            // Override with extracted values to ensure they're preserved
                            generatedMission.Country = extractedInfo.Country;
                            generatedMission.City = extractedInfo.City;
                            generatedMission.WorkMode = extractedInfo.WorkMode;
                            generatedMission.Duration = extractedInfo.Duration;
                            generatedMission.DurationType = extractedInfo.DurationType;
                            generatedMission.EstimatedDailyRate = extractedInfo.Salary;
                            generatedMission.ContractType = extractedInfo.ContractType;
                            generatedMission.ExperienceYear = extractedInfo.Experience;
                            
                            // Only override these if they were explicitly extracted
                            if (!string.IsNullOrEmpty(extractedInfo.Title))
                                generatedMission.Title = extractedInfo.Title;
                                
                            if (!string.IsNullOrEmpty(extractedInfo.Position))
                                generatedMission.Position = extractedInfo.Position;
                                
                            if (!string.IsNullOrEmpty(extractedInfo.Domain))
                                generatedMission.Domain = extractedInfo.Domain;
                                
                            // Ensure StartImmediately is set correctly
                            if (generatedMission.StartImmediately && generatedMission.StartDate.HasValue)
                            {
                                generatedMission.StartDate = null;
                            }
                            
                            return generatedMission;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON parsing error: {ex.Message}");
                    }
                }
                
                // Fallback to a basic mission if API call fails or returns invalid JSON
                return GenerateFallbackMission(simpleInput, extractedInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateFullMissionAsync: {ex.Message}");
                return GenerateFallbackMission(simpleInput, extractedInfo);
            }
        }
        
        private string ExtractJsonFromResponse(string response)
        {
            // Find the first '{' and the last '}'
            int start = response.IndexOf('{');
            int end = response.LastIndexOf('}');
            
            if (start >= 0 && end > start)
            {
                return response.Substring(start, end - start + 1);
            }
            
            return response; // Return the original if no JSON found
        }
        
        private Mission GenerateFallbackMission(string simpleInput, ExtractedInformation extractedInfo)
        {
            string techString = string.Join(", ", extractedInfo.Expertises);
            string title = !string.IsNullOrEmpty(extractedInfo.Position) 
                ? extractedInfo.Position 
                : $"Développement {techString}";
            
            if (title.Length > 80)
            {
                title = $"Développement {extractedInfo.Expertises.FirstOrDefault() ?? "web"}";
            }
            
            string description = $"Nous recherchons un développeur expérimenté en {techString} pour un projet de développement web. " +
                                $"Le candidat devra maîtriser les technologies mentionnées et être capable de travailler de manière autonome. " +
                                $"Cette mission se déroulera à {extractedInfo.City}, {extractedInfo.Country} en mode {extractedInfo.WorkMode.ToLower()} " +
                                $"pour une durée de {extractedInfo.Duration} {(extractedInfo.DurationType == "MONTH" ? "mois" : "ans")}.";
            
            return new Mission
            {
                Title = title,
                Description = description,
                Country = extractedInfo.Country,
                City = extractedInfo.City,
                WorkMode = extractedInfo.WorkMode,
                Duration = extractedInfo.Duration,
                DurationType = extractedInfo.DurationType,
                StartImmediately = true,
                ExperienceYear = extractedInfo.Experience,
                ContractType = extractedInfo.ContractType,
                EstimatedDailyRate = extractedInfo.Salary,
                Domain = extractedInfo.Domain,
                Position = extractedInfo.Position,
                RequiredExpertises = extractedInfo.Expertises.Count > 0 ? extractedInfo.Expertises : new List<string> { "Développement web" }
            };
        }
    }
}
