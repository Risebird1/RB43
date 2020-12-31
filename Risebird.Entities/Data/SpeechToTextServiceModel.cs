using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.Data
{
    public class SpeechToTextServiceModel:TableEntity
    {
        public int InterviewId { get; set; }


        public string AudioFileUrl { get; set; }


        public string KeyWords { get; set; }
        public string ConvertedText { get; set; }
    }
}
