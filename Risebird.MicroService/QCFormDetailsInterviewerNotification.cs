using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class QCFormDetailsInterviewerNotification
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        private readonly INotificationHelper _notificationHelper;

        public QCFormDetailsInterviewerNotification(IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository, INotificationHelper notificationHelper)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            _notificationHelper = notificationHelper;
        }
        [FunctionName("QCFormDetailsInterviewerNotification")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
                DateTime _startdate = DateTime.Now.Date;
                DateTime _enddate = _startdate.AddDays(1);
                var _list = await _meetingRespository.QCReportDetailsByDate(_startdate, _enddate);
                if (_list == null)
                {
                    _list = new List<MeetingVM>();
                }
                var _listinterviewer = _list.Select(x => x.interviewer).Distinct().ToList();
                foreach (var item in _listinterviewer)
                {
                    try
                    {
                        var _redflagdetails = _list.Where(x => x.interviewer == item).ToList();
                        Mail _mail = new Mail
                        {
                            TO = item,
                            //CC = "ashutosh@risebird.io;vaibhaw_sachan@risebird.io;shahkar_alam@risebird.io",
                            Subject = "Risebird: QC Report Details"
                        };
                        DataTable dt = new DataTable();
                        dt.Columns.AddRange(new DataColumn[4] { new DataColumn("Interview Date", typeof(string)),
                            new DataColumn("Candidate Name", typeof(string)),
                            new DataColumn("Client", typeof(string)),
                            new DataColumn("Red Flags", typeof(string)) });
                        foreach (var _redflag in _redflagdetails)
                        {
                            dt.Rows.Add(_redflag.interviewDate, _redflag.candidateName, _redflag.ClientName, _redflag.Redflags);
                        }
                        var emailresult = _notificationHelper.SendQCFormDetails(_mail, dt);
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
