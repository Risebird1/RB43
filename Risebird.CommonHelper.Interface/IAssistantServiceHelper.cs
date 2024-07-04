using Microsoft.AspNetCore.Rewrite;
using Risebird.Entities.Data;
using Risebird.Entities.Data.OpenAIModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IAssistantServiceHelper
    {
        //Task<CandidateScreeningResponse> ScreenCandidateAsync(ScreeningRequest request);
        Task<RunResult> ScreenAsync(ScreeningRequest request);
    }
}
