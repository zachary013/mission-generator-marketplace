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

namespace SmartMarketplace.Pages
{
    public class CreateMissionModel : PageModel
    {
        private readonly IGrokService _grokService;

        public CreateMissionModel(IGrokService grokService)
        {
            _grokService = grokService;
        }

        [BindProperty]
        public Mission Mission { get; set; }

        public SelectList WorkModeOptions { get; set; }
        public SelectList DurationTypeOptions { get; set; }
        public SelectList ExperienceYearOptions { get; set; }
        public SelectList ContractTypeOptions { get; set; }
        
        public string MissionJson { get; set; }
        public bool SubmissionSuccessful { get; set; }

        public void OnGet()
        {
            // Initialize SelectLists for dropdown menus
            InitializeSelectLists();
            
            // Initialize a new Mission object
            Mission = new Mission
            {
                StartImmediately = true // Default value
            };
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
            
            // Check if description is empty or too short
            if (string.IsNullOrWhiteSpace(Mission.Description) || Mission.Description.Length < 20)
            {
                try
                {
                    // Build the prompt for Grok API
                    string expertises = Mission.RequiredExpertises != null && Mission.RequiredExpertises.Any() 
                        ? string.Join(", ", Mission.RequiredExpertises) 
                        : "various technical skills";
                    
                    string domain = !string.IsNullOrWhiteSpace(Mission.Domain) 
                        ? Mission.Domain 
                        : "technology";
                    
                    string experience = !string.IsNullOrWhiteSpace(Mission.ExperienceYear) 
                        ? Mission.ExperienceYear 
                        : "3-7";
                    
                    string prompt = $"Generate a detailed mission description for a freelance project requiring skills in {expertises} for the {domain} domain, targeting professionals with {experience} years of experience.";
                    
                    // Call Grok API
                    string grokResponse = await _grokService.CallGrokApiAsync(prompt);
                    
                    // Check if response is valid
                    if (!string.IsNullOrWhiteSpace(grokResponse) && !grokResponse.StartsWith("Error"))
                    {
                        Mission.Description = grokResponse;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue with user's description
                    Console.WriteLine($"Error calling Grok API: {ex.Message}");
                }
            }

            if (!ModelState.IsValid)
            {
                // Re-initialize SelectLists if validation fails
                InitializeSelectLists();
                return Page();
            }

            // Serialize the Mission object to JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            MissionJson = JsonSerializer.Serialize(Mission, options);
            SubmissionSuccessful = true;

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
