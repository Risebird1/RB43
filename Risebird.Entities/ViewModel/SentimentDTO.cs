using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class SentimentDTO
    {
        public int imid { get; set; }

        [Display(Name = "Video File")]

        public IFormFile TruncatedVideoFile { get; set; }
        public string VideoFileName { get; set; }
    }
}
