using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class AlterMessage
    {
        public int ID { get; set; }
        [Required(ErrorMessage = "Announcement should not be blank")]
        public string Announcement { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime StartTime { get; set; }
        public string CalendlyLink { get; set; }
        public bool IsActive { get; set; }
    }
}
