using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.MicroService
{
    public class FetchTwilioCallRecordingData
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        
        private readonly ITwilioHelper _twilioHelper;
        private readonly IUserRepository _userRepository;



        public FetchTwilioCallRecordingData(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, ITwilioHelper twilioHelper, IUserRepository userRepository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _twilioHelper = twilioHelper;
            _userRepository = userRepository;


        }
        [FunctionName("FetchTwilioCallRecordingData")]
        public async Task Run([TimerTrigger("0 */20 * * * *")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                log.LogInformation($"FetchTwilioCallRecordingData: C# Timer trigger function started executing at: {DateTime.Now}");

                var recordingDataToBeUpdated = await _userRepository.GetRecordingDataToUpdate();

                //foreach (var recording in recordingDataToBeUpdated)
                //{
                //    var recordingData = await _twilioHelper.FetchCallRecordingDetails(recording.RecordingSid);
                //    //update the data recieved to the database
                //    CallRecordingDataVM obj = new CallRecordingDataVM();
                //    obj.id = recording.id;
                //    obj.RecordingSid = recording.RecordingSid;
                //    obj.StartTime = recordingData.StartTime;
                //    obj.Price = recordingData.Price;
                //    obj.Duration = recordingData.Duration;
                //    obj.PriceUnit = recordingData.PriceUnit;
                //    obj.RecordingUri = recordingData.Uri;
                //    obj.Status = recordingData.Status.ToString();

                //    await _userRepository.UpdateRecordingData(obj);
                //}
               
                Parallel.ForEach(recordingDataToBeUpdated, recording =>
                {
                    try
                    {
                        var recordingData = _twilioHelper.FetchCallRecordingDetails(recording.RecordingSid).Result;
                        //update the data recieved to the database
                        CallRecordingDataVM obj = new CallRecordingDataVM();
                        obj.id = recording.id;
                        obj.RecordingSid = recording.RecordingSid;
                        obj.StartTime = recordingData.StartTime;
                        obj.Price = recordingData.Price;
                        obj.Duration = recordingData.Duration;
                        obj.PriceUnit = recordingData.PriceUnit;
                        obj.RecordingUri = recordingData.Uri;
                        obj.RecordingUri = "https://api.twilio.com/2010-04-01/Accounts/ACcad9f6682d4a76df9af7cde78dd0867e/Recordings/" + recording.RecordingSid;

                        obj.Status = recordingData.Status.ToString();

                        _ = _userRepository.UpdateRecordingData(obj).Result;

                    }
                    catch (Exception ex)
                    {
                        _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                        log.LogError($"FetchTwilioCallRecordingData : Error fetching/updating recording data for Recording with id: {recording.RecordingSid}  at: {DateTime.Now}, Message : {ex.Message}");
                        return;
                    }
                });

                log.LogInformation($"FetchTwilioCallRecordingData : C# completed execution at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
