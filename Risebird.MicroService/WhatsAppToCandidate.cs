namespace Risebird.MicroService
{
    public class WhatsAppToCandidate
    {
        //private readonly IConfiguration _configuration;
        //private readonly IApplicationErrorLogger _applicationErrorLogger;
        //private readonly INotificationHelper _notificationHelper;
        //private readonly IDashboarRepository _dashboardRespository;
        //private readonly IWhatsAppHelper _whatsAppHelper;

        //private readonly string _connection;
        //public WhatsAppToCandidate(IConfiguration configuration, IDashboarRepository dashboardRespository, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper)
        //{
        //    _configuration = configuration;
        //    _applicationErrorLogger = applicationErrorLogger;
        //    _notificationHelper = notificationHelper;
        //    _dashboardRespository = dashboardRespository;
        //    _whatsAppHelper = whatsAppHelper;
        //    _connection = _configuration["ConnectionString:DBConnectionString"];
        //}
        //[FunctionName("WhatsAppToCandidate")]
        //public async void Run([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer, ILogger log)
        //{
        //    log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        //    try
        //    {
        //        var Emails = new List<CandidateDetailsForWhatsApp>();
        //        var str = _connection;
        //        var query = "select distinct im.id,candidateEmail,interviewDate,Irf.JDTiltle as JobTitle,cl.Name as ClientName from InterviewMeetings im left join IRF on im.IRFID=irf.irfid left join Client cl on cl.id=irf.ClientId where interviewDate<=@timeH and interviewDate>=@timeL and confirmationStatus=1";
        //        var dynamicParameters = new DynamicParameters();
        //        dynamicParameters.Add("@timeL", DateTime.Now.AddMinutes(115));
        //        dynamicParameters.Add("@timeH", DateTime.Now.AddMinutes(125));
        //        using (SqlConnection con = new SqlConnection(str))
        //        {
        //            Emails = (List<CandidateDetailsForWhatsApp>)await con.QueryAsync<CandidateDetailsForWhatsApp>(query, dynamicParameters);
        //        }

        //        if (Emails != null && Emails.Count() > 0)
        //        {
        //            try
        //            {
        //                foreach (var item in Emails)
        //                {
        //                    string urllink = "https://portal.risebird.io/Candidate/CancelInterviewProcess/" + item.id;
        //                    WhatsAppVM _whatsAppVM = new WhatsAppVM();
        //                    _whatsAppVM.Emailid = item.candidateEmail;
        //                    _whatsAppVM.InterviewDate = item.interviewDate.ToString();
        //                    _whatsAppVM.buttonlink = !string.IsNullOrEmpty(urllink) ? urllink.Replace("https://portal.risebird.io/", "") : "";
        //                    _whatsAppVM.Event = "Candidate Reminder";
        //                    _whatsAppVM.MessageValue = item.ClientName;
        //                    _whatsAppVM.Header = item.JobTitle;
        //                    _whatsAppHelper.WhatsAppSendCandidateReminder(_whatsAppVM);
        //                }

        //            }
        //            catch (Exception)
        //            {
        //                log.LogError($"Failed to send WhatsApp. Please check all filled and try again.");
        //            }
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
        //        log.LogError($"Failed to send WhatsApp. Please check all filled and try again.");
        //    }
        //}
    }
}
