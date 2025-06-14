using System.ComponentModel.DataAnnotations;

namespace SmartMarketplace.Models;

public class Mission
{
    public string? Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public string Country { get; set; } = string.Empty;
    
    [Required]
    public string City { get; set; } = string.Empty;
    
    public string WorkMode { get; set; } = "REMOTE"; // ONSITE, REMOTE, HYBRID
    
    public int Duration { get; set; }
    
    public string DurationType { get; set; } = "MONTH"; // MONTH, WEEK, DAY, YEAR
    
    public bool StartImmediately { get; set; } = true;
    
    public string? StartDate { get; set; } // yyyy-MM-dd format
    
    public string ExperienceYear { get; set; } = string.Empty; // "0-3", "3-7", "7-12", "12+"
    
    public string ContractType { get; set; } = "FORFAIT"; // REGIE, FORFAIT, CDI, CDD
    
    public decimal EstimatedDailyRate { get; set; }
    
    public string Domain { get; set; } = string.Empty;
    
    public string Position { get; set; } = string.Empty;
    
    public List<string> RequiredExpertises { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
