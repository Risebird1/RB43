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


    }
}
