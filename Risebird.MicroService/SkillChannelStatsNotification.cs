using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class SkillChannelStatsNotification
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IUserService _userService;
        private readonly IIRFService _irfService;

        public SkillChannelStatsNotification(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IUserService userService, IIRFService irfService)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _userService = userService;
            _irfService = irfService;
        }
        [FunctionName("SkillChannelStatsNotification")]
        public async Task Run([TimerTrigger("0 0 0 * * 6")] TimerInfo myTimer, ILogger log)//Every Saturday
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                var clientusers = await _userService.GetClientUsers("talent@risebird.io");
                foreach (var item in clientusers)
                {
                    try
                    {
                        var irfList = (await _irfService.GetAllIRFDetailsIRF(DateTime.Now.AddDays(-30), DateTime.Now.AddDays(7), "Client", item.EmailId))?.ToList();
                        if (irfList != null || irfList.Count() > 0)
                        {
                            DataTable dt = new DataTable();
                            dt.Columns.AddRange(new DataColumn[5] { new DataColumn("Skill Channel", typeof(string)),
                            new DataColumn("Candidates", typeof(int)),
                            new DataColumn("Completed",typeof(int)),
                            new DataColumn("Selected",typeof(int)),
                            new DataColumn("Rejected",typeof(int))});
                            foreach (var _irfitem in irfList)
                            {
                                dt.Rows.Add(_irfitem.IRFId, _irfitem.CandidateCount, _irfitem.CompletedInterviews, _irfitem.SelectedMeetings, _irfitem.RejectedMeetings);
                            }
                            Mail _mail1 = new Mail
                            {
                                TO = item.EmailId,
                                CC = "talent@risebird.io",
                                Subject = "Skill Channel Stats Details :" + DateTime.Now.AddDays(-30).ToShortDateString() + " to " + DateTime.Now.AddDays(7).ToShortDateString()
                            };
                            var emailsend = await _notificationHelper.SendSkillChannelStatsDetails(_mail1, item.FirstName, dt, item.logolink);
                        }
                    }
                    catch (Exception e)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                    }
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
