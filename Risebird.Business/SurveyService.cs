using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class SurveyService : ISurveyService
    {
        private readonly ISurveyRepository _surveyRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public SurveyService(ISurveyRepository surveyRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _surveyRepository = surveyRepository;
            _applicationErrorLogger = applicationErrorLogger;

        }
        public async Task<List<ClientAudioFeedbackDetails>> FetchClientAudioFeedbackDetails()
        {
            try
            {
                var result = await _surveyRepository.FetchClientAudioFeedbackDetails();
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<List<ClientAudioFeedbackDetails>> FetchBroadcastMessages()
        {
            try
            {
                var result = await _surveyRepository.FetchBroadcastMessages();
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<SurveyCandidateInterviewer> AddOrEditCandidateRating(SurveyVM model)
        {
            try
            {
                var result = await _surveyRepository.AddOrEditCandidateRating(model);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<SurveyVM>> GetCandidateRating(string emailid,string profileName)
        {
            try
            {
                var result = await _surveyRepository.GetCandidateRating(emailid, profileName);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<SurveyVM>> GetCandidateRatingByIRFId(string IRFId)
        {
            try
            {
                var result = await _surveyRepository.GetCandidateRatingByIRFId(IRFId);
                return result;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> CandidateFeedbackFormDetailsService(PaymentSLAVM model)
        {
            try
            {
                return await _surveyRepository.CandidateFedbackForm(model);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
