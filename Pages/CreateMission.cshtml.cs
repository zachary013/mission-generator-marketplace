using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SmartMarketplace.Models;
using SmartMarketplace.Services;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;

namespace SmartMarketplace.Pages
{
    public class CreateMissionModel : PageModel
    {
        private readonly IGrokService _grokService;
        private readonly IInputAnalysisService _inputAnalysisService;

        public CreateMissionModel(IGrokService grokService, IInputAnalysisService inputAnalysisService)
        {
            _grokService = grokService;
            _inputAnalysisService = inputAnalysisService;
        }

        [BindProperty]
        public Mission Mission { get; set; }

        public SelectList WorkModeOptions { get; set; }
        public SelectList DurationTypeOptions { get; set; }
        public SelectList ExperienceYearOptions { get; set; }
        public SelectList ContractTypeOptions { get; set; }
        
        public string MissionJson { get; set; }
        public bool SubmissionSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsGenerationMode { get; set; }
        public string ExpertisesInputValue { get; set; }
        public string SimpleInput { get; set; }
        public bool ShowGeneratedMission { get; set; }

        public void OnGet()
        {
            // Check if there's an error being handled
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature != null)
            {
                ErrorMessage = "Une erreur s'est produite. Veuillez réessayer plus tard.";
            }
            
            // Initialize SelectLists for dropdown menus
            InitializeSelectLists();
            
            // Initialize a new Mission object
            Mission = new Mission
            {
                StartImmediately = true, // Default value
                Country = "Maroc" // Default country
            };
            
            // Default mode is simple input
            IsGenerationMode = false;
            ShowGeneratedMission = false;
        }

        public async Task<IActionResult> OnPostGenerateAsync(string simpleInput)
        {
            if (string.IsNullOrWhiteSpace(simpleInput))
            {
                ErrorMessage = "Veuillez saisir une description de votre besoin.";
                return Page();
            }

            try
            {
                // Store the original input
                SimpleInput = simpleInput;
                
                // Initialize SelectLists for dropdown menus
                InitializeSelectLists();
                
                // Set generation mode
                IsGenerationMode = true;
                ShowGeneratedMission = true;
                
                // Extract information from the simple input using the new analysis service
                var extractedInfo = _inputAnalysisService.AnalyzeInput(simpleInput);
                
                // Generate mission using the improved GrokService
                Mission = await _grokService.GenerateFullMissionAsync(simpleInput, extractedInfo);
                
                // Set the expertises input value for the form
                ExpertisesInputValue = string.Join(", ", Mission.RequiredExpertises ?? new List<string>());
                
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Une erreur s'est produite lors de la génération de la mission: {ex.Message}";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Process RequiredExpertises from comma-separated string
            string expertisesInput = Request.Form["ExpertisesInput"];
            if (!string.IsNullOrWhiteSpace(expertisesInput))
            {
                Mission.RequiredExpertises = expertisesInput
                    .Split(',')
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
            }

            if (!ModelState.IsValid)
            {
                // Re-initialize SelectLists if validation fails
                InitializeSelectLists();
                IsGenerationMode = true;
                ShowGeneratedMission = true;
                ExpertisesInputValue = expertisesInput;
                return Page();
            }

            // Serialize the Mission object to JSON (for debugging purposes)
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            MissionJson = JsonSerializer.Serialize(Mission, options);
            SubmissionSuccessful = true;
            ShowGeneratedMission = true;

            // TODO: Save the Mission to your database
            // For example: await _context.Missions.AddAsync(Mission);
            // await _context.SaveChangesAsync();

            return Page();
        }
        
        private void InitializeSelectLists()
        {
            WorkModeOptions = new SelectList(new[] { "REMOTE", "ONSITE", "HYBRID" });
            DurationTypeOptions = new SelectList(new[] { "MONTH", "YEAR" });
            ExperienceYearOptions = new SelectList(new[] { "0-3", "3-7", "7-12", "12+" });
            ContractTypeOptions = new SelectList(new[] { "FORFAIT", "REGIE" });
        }
    }
}
