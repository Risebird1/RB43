using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IInterviewerAvailabilityRepository
    {
        Task<bool> Add(List<InterviewerAvailabilityVM> interviewerAvailabilities);
    }
}
