using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface ITaskRepository
    {
        Task<SkillChannelCollectiveDetailsVM> SkillChannelCollectiveDetails(int irfId, string type);
        Task<IEnumerable<PQIDetails>> GetSkillChannelAccuracyDetails(int irfId, string type);
        Task<IEnumerable<SkillChannelTaskSpeedDetailsVM>> SkillChannelSpeedDetails(int irfId, string type);
        Task<bool> AddTaskStatus(SkillChannelTasks model);
        Task<IEnumerable<SkillChannelTaskCompletionDetailsVM>> SkillChannelCompletionDetails(int irfId, string type);
        Task<IEnumerable<SkillChannelTasks>> GetTaskStatusList(int irfId, string tab, string taskValue);
        Task<IEnumerable<SkillChannelTaskCompletionDetailsVM>> GetSkillChannelAccuracyRedoDetails(int irfId, string type);
    }
}
