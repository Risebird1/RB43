using System;
using System.Collections.Generic;
using System.Text;
using Risebird.Entities.ViewModel;

namespace Risebird.Business.Interface
{
    public interface IInterviewerSkillsService
    {
        IEnumerable<InterviewerSkillsModel> GetAllInterviewerSkills(string emailId, string profileName);

    }
}
