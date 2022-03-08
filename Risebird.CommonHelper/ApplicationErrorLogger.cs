using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class ApplicationErrorLogger : IApplicationErrorLogger
    {
        private readonly IConfiguration _configuration;
        public ApplicationErrorLogger(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Log exception in table storage 'onesharelogs'
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <param name="projectId"></param>
        /// <param name="callingFilePath"></param>
        /// <param name="callingMethod"></param>
        /// <param name="callingFileLineNumber"></param>
        public async Task LogMessage(LogLevelInfo logLevelInfo, Exception ex, string userEmailid = "")
        {
            try
            {
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);

                var st = new StackTrace(ex, true); // create the stack trace
                var query = st.GetFrames()         // get the frames
                              .Select(frame => new
                              {                   // get the info
                                  FileName = frame.GetFileName(),
                                  LineNumber = frame.GetFileLineNumber(),
                                  ColumnNumber = frame.GetFileColumnNumber(),
                                  Method = frame.GetMethod(),
                                  Class = frame.GetMethod().DeclaringType
                              });
                if (query != null)
                {
                    var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    var exception = new LogData(date, Guid.NewGuid().ToString())
                    {
                        LogeLevel = (int)logLevelInfo,
                        UserEmailId = userEmailid,
                        LogMessage = ex.Message,
                        InnerException = ex.InnerException != null ? ex.InnerException.StackTrace : "NA",
                        FileName = query.FirstOrDefault()?.FileName.ToString(),
                        MethodName = query.FirstOrDefault()?.Method.ToString(),
                        ClassName = query.FirstOrDefault()?.Class.ToString(),
                        LineNumber = query.FirstOrDefault().LineNumber,
                        Status = (int)LogStatus.Unresolved
                    };
                    await _azureTableStorageService.InsertEntity("Risebirdlogs", exception);
                }

            }
            catch (Exception ex1)
            {
                Console.WriteLine(ex1.InnerException);
            }
        }
        public async Task LogMessage(LogLevelInfo logLevelInfo, string message, string userEmailid = "")
        {
            try
            {
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);

              
                    var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    var exception = new LogData(date, Guid.NewGuid().ToString())
                    {
                        LogeLevel = (int)logLevelInfo,
                        UserEmailId = userEmailid,
                        LogMessage = message                     
                    };
                    await _azureTableStorageService.InsertEntity("Risebirdlogs", exception);
               

            }
            catch (Exception ex1)
            {
                Console.WriteLine(ex1.InnerException);
            }
        }

        public async Task LogUpdateIRFMessage(LogLevelInfo logLevelInfo, Exception ex, string userEmailid = "")
        {
            try
            {
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);

                var st = new StackTrace(ex, true); // create the stack trace
                var query = st.GetFrames()         // get the frames
                              .Select(frame => new
                              {                   // get the info
                                  FileName = frame.GetFileName(),
                                  LineNumber = frame.GetFileLineNumber(),
                                  ColumnNumber = frame.GetFileColumnNumber(),
                                  Method = frame.GetMethod(),
                                  Class = frame.GetMethod().DeclaringType
                              });
                if (query != null)
                {
                    var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    var exception = new LogData(date, Guid.NewGuid().ToString())
                    {
                        LogeLevel = (int)logLevelInfo,
                        UserEmailId = userEmailid,
                        LogMessage = ex.Message,
                        InnerException = ex.InnerException != null ? ex.InnerException.StackTrace : "NA",
                        FileName = query.FirstOrDefault()?.FileName.ToString(),
                        MethodName = query.FirstOrDefault()?.Method.ToString(),
                        ClassName = query.FirstOrDefault()?.Class.ToString(),
                        LineNumber = query.FirstOrDefault().LineNumber,
                        Status = (int)LogStatus.Unresolved
                    };
                    await _azureTableStorageService.InsertEntity("UpdateIRFLogs", exception);
                }

            }
            catch (Exception ex1)
            {
                Console.WriteLine(ex1.InnerException);
            }
        }

    }
}
