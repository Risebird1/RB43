using Microsoft.Extensions.Configuration;
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
    public class CandidateService : ICandidateService
    {
        private readonly ICandidateRepository _candidateRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;
        private readonly IIRFService _irfService;
        private readonly IIRFRepository _irfRepository;
        private readonly ITwilioHelper _twilioHelper;


        private readonly string twilioAuthToken;
        private readonly string twilioAccountSID;
        private readonly string twilioVoiceCallNumber;
        public CandidateService(ICandidateRepository candidateRepository, IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration, IIRFService irfService, IIRFRepository irfRepository, ITwilioHelper twilioHelper)
        {
            _candidateRepository = candidateRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
            _irfService = irfService;
            twilioAuthToken = _configuration["TwilioSettings:AUTH_TOKEN"];
            twilioAccountSID = _configuration["TwilioSettings:ACCOUNT_SID"];
            twilioVoiceCallNumber = _configuration["TwilioSettings:CallerNumber"];
            _irfRepository = irfRepository;
            _twilioHelper = twilioHelper;
        }

        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimeline(string emailId = null)
        {
            try
            {
                return await _candidateRepository.GetCandidateTimeline(emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IEnumerable<CandidateTimelineData>> GetCandidateTimelinebyDate(DateTime startDate, DateTime endDate, string emailId = null)
        {
            try
            {
                return await _candidateRepository.GetCandidateTimelinebyDate(startDate, endDate, emailId);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        //public async Task<bool> AutoCallCandidate(string phoneNumber, int irfId, int candidateId, string loggedInEmail)
        //{
        //    try
        //    {
        //        var link = string.Format("https://portal.risebird.io/TwilioVoice/Index?irfId={0}", irfId.ToString());
        //        var url = new Uri(link);
        //        TwilioClient.Init(twilioAccountSID, twilioAuthToken);
        //        var to = new PhoneNumber(phoneNumber);
        //        var from = new PhoneNumber(twilioVoiceCallNumber);

        //        var call = await CallResource.CreateAsync(
        //            to: to,
        //            from: from,
        //            url: url);

        //        if (call.Status != null)
        //        {
        //            var result = call.Status.ToString();
        //            if (result != CallResource.StatusEnum.Failed.ToString())
        //            {
        //                await _irfService.AddCandidateStatus(loggedInEmail, candidateId, irfId, "Placed a reminder call", (int)CandidateStatusOptions.ReminderCall);
        //                return true;
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //        return false;

        //    }

        //    catch (Exception e)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
        //        return false;
        //    }
        //}
        public async Task<bool> AutoCallCandidate(string phoneNumber, int irfId, int candidateId, string loggedInEmail)
        {
            try
            {
                return await _twilioHelper.AutoCallCandidate(phoneNumber, irfId, candidateId, loggedInEmail);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<CandidateDetailsForWhatsApp> FetchInterviewDetails(int interviewid)
        {
            try
            {
                return await _candidateRepository.FetchInterviewDetails(interviewid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<string> CancelInterviewDetails(int interviewid, string CandidateEmail)
        {
            try
            {
                return await _candidateRepository.CancelInterviewDetails(interviewid, CandidateEmail);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<InterviewUsersDetails> FetchChannelOwnerandInterviewer(int interviewid)
        {
            try
            {
                return await _candidateRepository.FetchChannelOwnerandInterviewer(interviewid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<InterviewUsersDetails> FetchInterviewerDetails(int interviewid)
        {
            try
            {
                return await _candidateRepository.FetchInterviewerDetails(interviewid);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<Candidate> GetCandidatesDetails(int irfId, int candidateId)
        {
            try
            {
                return await _candidateRepository.GetCandidatesDetails(irfId, candidateId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<bool> AddResume(List<CandidateResume> resumeList)
        {
            try
            {
                return await _candidateRepository.AddResume(resumeList);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IEnumerable<CandidateResume>> GetCandidatesResumes(int irfId)
        {
            try
            {
                return await _candidateRepository.GetCandidatesResumes(irfId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<bool> DeleteCandidateResume(int id)
        {
            try
            {
                return await _candidateRepository.DeleteCandidateResume(id);
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
