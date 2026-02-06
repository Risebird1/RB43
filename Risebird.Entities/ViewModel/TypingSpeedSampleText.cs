using System;

namespace Risebird.Entities.ViewModel
{
    public class TypingSpeedSampleText
    {
        public int id { get; set; }
        public string SampleText { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
