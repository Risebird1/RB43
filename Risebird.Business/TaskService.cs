using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public TaskService(ITaskRepository taskRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _taskRepository = taskRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public async Task<SkillChannelCollectiveDetailsVM> SkillChannelCollectiveDetails(int irfId, string type)
        {
            try
            {
                return await _taskRepository.SkillChannelCollectiveDetails(irfId, type);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<PQIDetails>> GetSkillChannelAccuracyDetails(int irfId, string type)
        {
            try
            {
                return await _taskRepository.GetSkillChannelAccuracyDetails(irfId, type);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<SkillChannelTaskSpeedDetailsVM>> SkillChannelSpeedDetails(int irfId, string type)
        {
            try
            {
                return await _taskRepository.SkillChannelSpeedDetails(irfId, type);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<bool> AddTaskStatus(SkillChannelTasks model)
        {
            try
            {
                return await _taskRepository.AddTaskStatus(model);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<IEnumerable<SkillChannelTaskCompletionDetailsVM>> SkillChannelCompletionDetails(int irfId, string type)
        {
            try
            {
                return await _taskRepository.SkillChannelCompletionDetails(irfId, type);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<SkillChannelTasks>> GetTaskStatusList(int irfId, string tab, string taskValue)
        {
            try
            {
                return await _taskRepository.GetTaskStatusList(irfId, tab, taskValue);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<IEnumerable<SkillChannelTaskCompletionDetailsVM>> GetSkillChannelAccuracyRedoDetails(int irfId, string type)
        {
            try
            {
                return await _taskRepository.GetSkillChannelAccuracyRedoDetails(irfId, type);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
    }
}
