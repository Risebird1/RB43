using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class CuratedTalentSectionsForSkillChannel
    {
        public int Id { get; set; }
        public int IRFId { get; set; }
        public bool CommunicationSkills { get; set; }
        public bool VerbalAptitude { get; set; }
        public bool QuantitativeAptitude { get; set; }
        public bool PsychometricTest { get; set; }
        public bool DataInterpretation { get; set; }
        public bool DataSufficiency { get; set; }
        public bool CriticalReasoning { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string ModifiedBy { get; set; } // Note: In the table, ModifiedBy is datetime, not string
        public DateTime? ModifiedAt { get; set; }
    }
}
