using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface ICalendlyHelper
    {
        void DumpExistingIRFResourceURIs();
        Task UpdatePendingCalendlyResourceUriForIRF();
        Task<string> GenerateOneTimeSchedulingLink(int irfId);
    }
}
