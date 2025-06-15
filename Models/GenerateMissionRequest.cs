using System.ComponentModel.DataAnnotations;

namespace SmartMarketplace.Models;

public class GenerateMissionRequest
{
    [Required(ErrorMessage = "SimpleInput is required")]
    [MinLength(10, ErrorMessage = "SimpleInput must be at least 10 characters")]
    public string SimpleInput { get; set; } = string.Empty;
    
    public string? PreferredProvider { get; set; } // "Gemini", "Llama", "Mistral"
}
