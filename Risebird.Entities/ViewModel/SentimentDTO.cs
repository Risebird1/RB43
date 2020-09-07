using System.ComponentModel.DataAnnotations;

namespace Risebird.Entities.ViewModel
{
    public class SentimentDTO
    {
        public int imid { get; set; }

        [Display(Name = "Video File")]
        public string VideoFileName { get; set; }
    }
}
