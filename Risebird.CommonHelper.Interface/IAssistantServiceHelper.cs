using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IAssistantServiceHelper
    {
        Task<CandidateScreeningResponse> ScreenCandidateAsync(CandidateScreeningRequest request);
    }
}
