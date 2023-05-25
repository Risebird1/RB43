using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data.AffIndaModels
{
    public class PhoneNumber
    {
        public string rawText { get; set; }
        public string formattedNumber { get; set; }
        public string countryCode { get; set; }
        public string internationalCountryCode { get; set; }
        public string nationalNumber { get; set; }
    }
}
