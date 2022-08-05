using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class ResumeUploadCount
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly INotificationHelper _notificationHelper;

        public ResumeUploadCount(IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository, INotificationHelper notificationHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
        }
        [FunctionName("ResumeUploadCount")]
        public async Task Run([TimerTrigger("0 0 */8 * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                DateTime _enddate = DateTime.Now;
                DateTime _startdate = DateTime.Now.AddHours(-8);
                var _list = await _meetingRespository.NotifiyUploadResumeDetails(_startdate, _enddate);
                if (_list == null)
                {
                    _list = new List<ResumeDetailsVM>();
                }
                var _listChannelowner = _list.Select(x => x.EmailId).Distinct().ToList();
                foreach (var item in _listChannelowner)
                {
                    try
                    {
                        var _countdetails = _list.Where(x => x.EmailId == item).ToList();
                        Mail _mail = new Mail
                        {
                            TO = item,
                            CC = "ashutosh@risebird.io;vaibhaw_sachan@risebird.io;shahkar_alam@risebird.io",
                            Subject = "Risebird: New Uploaded Resumes"
                        };
                        DataTable dt = new DataTable();
                        dt.Columns.AddRange(new DataColumn[2] { new DataColumn("Skill Channel", typeof(string)), new DataColumn("Uploaded Resumes", typeof(string)) });
                        foreach (var _irf in _countdetails)
                        {
                            dt.Rows.Add(_irf.IRFId, _irf.TotalResumes);
                        }
                        var emailresult = _notificationHelper.SendResumeUploadCounts(_mail, dt, _countdetails.FirstOrDefault().FirstName);
                    }
                    catch (Exception ex)
                    {
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
