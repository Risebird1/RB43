using System;

namespace Risebird.Entities.ViewModel
{
    public class AIInterviewExitModalViewModel
    {
        public string Title { get; set; } = "Interview Session Complete";
        public string Message { get; set; }
        public string IconClass { get; set; } = "fa-check-circle";
        public string IconColor { get; set; } = "#28a745";
        public string ButtonText { get; set; } = "Close";
        public string ButtonClass { get; set; } = "btn-success";
        public bool IsSuccess { get; set; } = true;
        public string CandidateName { get; set; }
        public string SessionId { get; set; }
        public DateTime ExitTime { get; set; } = DateTime.Now;
        
        // Add missing properties
        public DateTime? InterviewDate { get; set; }
     
        
        // Add this new property
        public bool IsFromIndex { get; set; } = false;
    }
} 