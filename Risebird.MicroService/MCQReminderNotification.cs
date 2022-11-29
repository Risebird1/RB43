using System;
using Dapper;
using System.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using System.Linq;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;

namespace Risebird.MicroService
{
    public class MCQReminderNotification
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private readonly IWhatsAppHelper _whatsAppHelper;
        private readonly string connectionString;

        public MCQReminderNotification(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, INotificationHelper notificationHelper, IWhatsAppHelper whatsAppHelper)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _whatsAppHelper = whatsAppHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];
        }
        [FunctionName("MCQReminderNotification")]
        public async Task Run([TimerTrigger("0 0 10 * * *")] TimerInfo myTimer, ILogger log)//Daily 10 AM
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var _mcqlist = await FetchApp_MCQReminderDetails();
            foreach (var item in _mcqlist)
            {
                string currenturl = "https://portal.risebird.io/MCQ/SkillTest?Id=" + item.MCQId;
                if (item.botactive)
                {
                    currenturl = "https://bot.risebird.io/api/v1/bot/invite/" + item.roomId;
                }
                else
                {
                    currenturl = currenturl + "&uniqueid=448";
                }
                try
                {
                    var result = string.Empty;
                    Mail _mail = new Mail
                    {
                        TO = item.EmailAddress,
                        Subject = "Reminder: Invitation for " + item.Title + " : " + item.ApplyingCompany
                    };
                    result = await _notificationHelper.SharedReminderTestLink(_mail, item.Title, item.Timer.ToString(), currenturl, item.ApplyingCompany, item.ShareDate.AddHours(item.Expiretime));
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
            var _mcqporject = await FetchProject_MCQReminderDetails();
            foreach (var item in _mcqporject)
            {
                var currenturl = "https://bot.risebird.io/api/v1/bot/invite/" + item.roomId;
                try
                {
                    var result = string.Empty;
                    Mail _mail = new Mail
                    {
                        TO = item.EmailAddress,
                        Subject = "Reminder: Invitation for " + item.Title + " : " + item.ApplyingCompany
                    };
                    result = await _notificationHelper.SharedReminderTestLink(_mail, item.Title, item.Timer.ToString(), currenturl, item.ApplyingCompany, item.ShareDate.AddHours(item.Expiretime));
                }
                catch (Exception ex)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                }
            }
        }
        public async Task<IEnumerable<MCQVMShare>> FetchApp_MCQReminderDetails()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MCQVMShare>(con, "usp_FetchMCQReminderDetails", null,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
        public async Task<IEnumerable<MCQVMShare>> FetchProject_MCQReminderDetails()
        {
            try
            {
                var _connectionString = "Server=tcp:risebirdmcqdev.database.windows.net,1433;Initial Catalog=risebirdmcqdev;Persist Security Info=False;User ID=risebirdmcqadmin;Password=rise_123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Max Pool Size=1000;";
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    var result = await SqlMapper.QueryAsync<MCQVMShare>(con, "usp_FetchMCQReminderDetails", null,
                        commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (SqlException e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
            }
            return null;
        }
    }
}
