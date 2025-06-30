using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAIInterviewerRepository
    {
        Task CreateSession(InterviewSession session);
        Task<string> GetThreadId(Guid sessionId);
        Task LogTranscript(Guid sessionId, string role, string message);
    }
}
