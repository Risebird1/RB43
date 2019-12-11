using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Risebird.Business
{
    public class EvaluatorService : IEvaluatorService
    {
        private IEvaluatorRepository _evaluatorRespository;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;

        public EvaluatorService(IEvaluatorRepository evaluatorRespository, INotificationHelper notificationHelper, IApplicationErrorLogger applicationErrorLogger)
        {
            _evaluatorRespository = evaluatorRespository;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
        }

        public List<CandidateResumeEvaluation> GetOpenEvaluations(string emailId)
        {
            try
            {
                return _evaluatorRespository.GetOpenEvaluations(emailId).Result.ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public string AssignSelectabilityYourself(int id, string mailId)
        {
            try
            {
                return _evaluatorRespository.AssignYourselfSelectability(id, mailId);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }

        }

        public List<CandidateResumeEvaluation> GetAssignedSelectabilityEvaluations(string emailID)
        {
            try
            {
                var assignedEvaluations =_evaluatorRespository.GetAssignedSelectabilityEvaluations(emailID).Result.ToList();                
                return assignedEvaluations;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public bool UnAssignSelectability(int id)
        {
            try
            {
                return _evaluatorRespository.UnAssignSelectability(id);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }




    }
}
