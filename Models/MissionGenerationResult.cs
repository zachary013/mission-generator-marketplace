namespace SmartMarketplace.Models;

public class MissionGenerationResult
{
    public Mission? Mission { get; set; }
    public string Provider { get; set; } = string.Empty;
    public bool IsSuccess => Mission != null;
}
