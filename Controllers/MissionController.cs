using Microsoft.AspNetCore.Mvc;
using SmartMarketplace.Models;
using SmartMarketplace.Services;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace SmartMarketplace.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MissionController : ControllerBase
{
    private readonly IAIService _aiService;
    private readonly ILogger<MissionController> _logger;
    private static readonly List<Mission> _missions = new(); // In-memory storage for demo

    public MissionController(IAIService aiService, ILogger<MissionController> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Génère une mission freelance à partir d'une description simple
    /// </summary>
    /// <param name="request">Requête contenant la description simple</param>
    /// <returns>Mission générée par l'IA</returns>
    [HttpPost("generate")]
    public async Task<ActionResult<ApiResponse<Mission>>> GenerateMission([FromBody] GenerateMissionRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<Mission>.ErrorResult($"Validation errors: {string.Join(", ", errors)}"));
            }

            _logger.LogInformation("Generating mission from input: {Input}", request.SimpleInput);

            var result = await _aiService.GenerateMissionAsync(request.SimpleInput, request.PreferredProvider);

            if (result.Mission == null)
            {
                _logger.LogWarning("Failed to generate mission for input: {Input}", request.SimpleInput);
                return StatusCode(500, ApiResponse<Mission>.ErrorResult("Impossible de générer la mission. Veuillez réessayer."));
            }

            _logger.LogInformation("Successfully generated mission: {Title} with provider: {Provider}", result.Mission.Title, result.Provider);

            return Ok(ApiResponse<Mission>.SuccessResult(result.Mission, result.Provider));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating mission for input: {Input}", request.SimpleInput);
            return StatusCode(500, ApiResponse<Mission>.ErrorResult("Une erreur interne s'est produite."));
        }
    }

    /// <summary>
    /// Sauvegarde une mission
    /// </summary>
    /// <param name="mission">Mission à sauvegarder</param>
    /// <returns>Mission sauvegardée</returns>
    [HttpPost("save")]
    public ActionResult<ApiResponse<Mission>> SaveMission([FromBody] Mission mission)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(ApiResponse<Mission>.ErrorResult($"Validation errors: {string.Join(", ", errors)}"));
            }

            // Generate ID if not provided
            if (string.IsNullOrEmpty(mission.Id))
            {
                mission.Id = Guid.NewGuid().ToString();
            }

            mission.CreatedAt = DateTime.UtcNow;

            // Remove existing mission with same ID
            _missions.RemoveAll(m => m.Id == mission.Id);
            
            // Add new mission
            _missions.Add(mission);

            _logger.LogInformation("Mission saved successfully: {Id} - {Title}", mission.Id, mission.Title);

            return Ok(ApiResponse<Mission>.SuccessResult(mission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving mission: {Title}", mission.Title);
            return StatusCode(500, ApiResponse<Mission>.ErrorResult("Erreur lors de la sauvegarde de la mission."));
        }
    }

    /// <summary>
    /// Récupère toutes les missions sauvegardées
    /// </summary>
    /// <returns>Liste des missions</returns>
    [HttpGet]
    public ActionResult<ApiResponse<List<Mission>>> GetAllMissions()
    {
        try
        {
            var missions = _missions.OrderByDescending(m => m.CreatedAt).ToList();
            return Ok(ApiResponse<List<Mission>>.SuccessResult(missions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving missions");
            return StatusCode(500, ApiResponse<List<Mission>>.ErrorResult("Erreur lors de la récupération des missions."));
        }
    }

    /// <summary>
    /// Récupère une mission par son ID
    /// </summary>
    /// <param name="id">ID de la mission</param>
    /// <returns>Mission trouvée</returns>
    [HttpGet("{id}")]
    public ActionResult<ApiResponse<Mission>> GetMissionById(string id)
    {
        try
        {
            var mission = _missions.FirstOrDefault(m => m.Id == id);
            
            if (mission == null)
            {
                return NotFound(ApiResponse<Mission>.ErrorResult("Mission non trouvée."));
            }

            return Ok(ApiResponse<Mission>.SuccessResult(mission));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mission by ID: {Id}", id);
            return StatusCode(500, ApiResponse<Mission>.ErrorResult("Erreur lors de la récupération de la mission."));
        }
    }

    /// <summary>
    /// Supprime une mission
    /// </summary>
    /// <param name="id">ID de la mission à supprimer</param>
    /// <returns>Résultat de la suppression</returns>
    [HttpDelete("{id}")]
    public ActionResult<ApiResponse<bool>> DeleteMission(string id)
    {
        try
        {
            var removed = _missions.RemoveAll(m => m.Id == id) > 0;
            
            if (!removed)
            {
                return NotFound(ApiResponse<bool>.ErrorResult("Mission non trouvée."));
            }

            _logger.LogInformation("Mission deleted successfully: {Id}", id);
            return Ok(ApiResponse<bool>.SuccessResult(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting mission: {Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResult("Erreur lors de la suppression de la mission."));
        }
    }

    /// <summary>
    /// Vérifie le statut des services IA
    /// </summary>
    /// <returns>Statut des services</returns>
    [HttpGet("ai-status")]
    public async Task<ActionResult<ApiResponse<Dictionary<string, bool>>>> GetAIStatus()
    {
        try
        {
            var providers = _aiService.GetAvailableProviders();
            var status = new Dictionary<string, bool>();

            foreach (var provider in providers)
            {
                status[provider] = await _aiService.IsProviderAvailableAsync(provider);
            }

            return Ok(ApiResponse<Dictionary<string, bool>>.SuccessResult(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking AI service status");
            return StatusCode(500, ApiResponse<Dictionary<string, bool>>.ErrorResult("Erreur lors de la vérification du statut des services IA."));
        }
    }
}
