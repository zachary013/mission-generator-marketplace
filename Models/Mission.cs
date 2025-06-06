using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartMarketplace.Models
{
    public class Mission
    {
        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Le titre est obligatoire")]
        public string Title { get; set; }
        
        [Required(ErrorMessage = "La description est obligatoire")]
        public string Description { get; set; }
        
        [Required(ErrorMessage = "Le pays est obligatoire")]
        public string Country { get; set; }
        
        [Required(ErrorMessage = "La ville est obligatoire")]
        public string City { get; set; }
        
        [Required(ErrorMessage = "Le mode de travail est obligatoire")]
        [RegularExpression("REMOTE|ONSITE|HYBRID", ErrorMessage = "Le mode de travail doit être 'REMOTE', 'ONSITE' ou 'HYBRID'")]
        public string WorkMode { get; set; }
        
        [Required(ErrorMessage = "La durée est obligatoire")]
        [Range(1, int.MaxValue, ErrorMessage = "La durée doit être un nombre positif")]
        public int Duration { get; set; }
        
        [Required(ErrorMessage = "Le type de durée est obligatoire")]
        [RegularExpression("MONTH|YEAR", ErrorMessage = "Le type de durée doit être 'MONTH' ou 'YEAR'")]
        public string DurationType { get; set; }
        
        public bool StartImmediately { get; set; }
        
        public DateTime? StartDate { get; set; }
        
        [Required(ErrorMessage = "L'expérience requise est obligatoire")]
        [RegularExpression("0-3|3-7|7-12|12\\+", ErrorMessage = "L'expérience doit être '0-3', '3-7', '7-12' ou '12+'")]
        public string ExperienceYear { get; set; }
        
        [Required(ErrorMessage = "Le type de contrat est obligatoire")]
        [RegularExpression("FORFAIT|REGIE", ErrorMessage = "Le type de contrat doit être 'FORFAIT' ou 'REGIE'")]
        public string ContractType { get; set; }
        
        [DataType(DataType.Currency)]
        public decimal EstimatedDailyRate { get; set; }
        
        public string Domain { get; set; }
        
        public string Position { get; set; }
        
        public List<string> RequiredExpertises { get; set; } = new List<string>();
    }
}
