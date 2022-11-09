using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class CandidateStatusNotifier
    {
        private readonly IConfiguration _configuration;
        private readonly ICandidateRepository _candidateRepository;
        private readonly IIRFRepository _irfRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;

        private readonly string connectionString;

        public CandidateStatusNotifier(IConfiguration configuration, ICandidateRepository candidateRepository, IApplicationErrorLogger applicationErrorLogger, IIRFRepository irfRepository, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _candidateRepository = candidateRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfRepository = irfRepository;
            _notificationHelper = notificationHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }

        [FunctionName("CandidateStatusNotifier")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

                var data = await _candidateRepository.GetCandidateLastStatus();
                if (data != null)
                {
                    var irfList = new List<int>();
                    irfList = data.Select(x => x.IRFintId).Distinct().ToList();

                    if (irfList != null)
                    {
                        foreach (var irf in irfList)
                        {
                            var candidates = new List<string>();

                            var mailList = await _irfRepository.GetIRFClientsEmail(irf);
                            if (mailList == null || mailList.Count() == 0)
                            {
                                continue;
                            }
                            foreach (var item in data.Where(x => x.IRFintId == irf && !string.IsNullOrEmpty(x.StatusValue)))
                            {
                                var status = string.Empty;


                                if (item.StatusValue == "Interview Status")
                                {
                                    if ((item.Comments == InterviewStatus.Selected.GetDescription() || item.Comments == InterviewStatus.BorderLine.GetDescription()) && item.QCStatus == null)
                                    {
                                        status = MeetingStatus.Completed.GetDescription();
                                    }
                                    else
                                    {
                                        status = item.Comments;
                                    }
                                }

                                else if (item.StatusValue == "Meeting Status")
                                {
                                    status = item.Comments;
                                }

                                else
                                {
                                    status = item.StatusValue;
                                }

                                candidates.Add(item.candidateName + "|" + item.candidateEmail + "|" + status);
                            }
                            if (candidates == null || candidates.Count() == 0)
                            {
                                continue;
                            }
                            var details = data.Where(x => x.IRFintId == irf).FirstOrDefault();
                            var primarySkill = details.JDTitle.Split('|')[0];
                           await _notificationHelper.SendCandidateLastStatusMail(mailList.ToList(), candidates, details.IRFId, primarySkill);
                        }
                    }


                }

                log.LogInformation($"C# Timer trigger function completed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
