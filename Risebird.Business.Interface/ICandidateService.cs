using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface ICandidateService
    {
        Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline();
    }
}
