using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class FLNConversionVM
    {
        public string EmailId { get; set; }
        public string ProfileName { get; set; }
        public int TotalProfilesAdded { get; set; }
        public int TotalAcknowledged { get; set; }
        public int PaymentApplicable { get; set; }
    }
}
