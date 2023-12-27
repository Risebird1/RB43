using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class ZoomQualityOfServiceEventCapture
    {
        private readonly IConfiguration _configuration;

        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;
        public ZoomQualityOfServiceEventCapture(IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;

        }

        [FunctionName("ZoomQualityOfServiceEventCapture")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "ZoomQualityOfServiceEventCapture")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"ZoomQualityOfServiceEventCapture : C# HTTP trigger function invoked at {DateTime.Now}");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

                //log.LogInformation($"ZoomQualityOfServiceEventCapture : Event data recieved with request body :  {requestBody}");

                string eventType = string.Empty;
                
                dynamic stuff = Newtonsoft.Json.JsonConvert.DeserializeObject(requestBody);


                string plainToken = string.Empty;
                var children = stuff.Children().GetEnumerator();



                while (children.MoveNext())
                {

                    var mainDetails = children.Current;

                    if (((JProperty)mainDetails).Name == "event")
                    {

                        eventType = ((JProperty)mainDetails).Value.ToString().Split('.').Last().ToUpper();
                    }

                    if (((JProperty)mainDetails).Name == "payload")
                    {
                        foreach (var dataItem in mainDetails.Children())
                        {

                            var internalNodes = dataItem.Children().GetEnumerator();
                            while (internalNodes.MoveNext())
                            {
                                var mainFamilies1 = internalNodes.Current;
                                if (((Newtonsoft.Json.Linq.JProperty)((JToken)mainFamilies1)).Name == "plainToken")
                                {
                                    plainToken = ((Newtonsoft.Json.Linq.JProperty)((JToken)mainFamilies1)).Value.ToString();
                                }
                                
                            }
                        }
                    }
                }

                if (eventType == "URL_VALIDATION")
                {

                    var validationkey = _configuration["ZoomAppDetails:apivalidationKey"].ToString();
                    var enc = Encoding.Default;
                    HMACSHA256 hmac = new HMACSHA256(enc.GetBytes(validationkey));
                    hmac.Initialize();
                    byte[] buffer = enc.GetBytes(plainToken);
                    var hashForValidate = BitConverter.ToString(hmac.ComputeHash(buffer)).Replace("-", "").ToLower();
                    ZoomParticipentResponse _zoom = new ZoomParticipentResponse();
                    _zoom.plainToken = plainToken;
                    _zoom.encryptedToken = hashForValidate;
                    return new OkObjectResult(Newtonsoft.Json.JsonConvert.SerializeObject(_zoom));
                }


                var data = JsonConvert.DeserializeObject<ZoomQualityOfServiceData>(requestBody);

                log.LogInformation($"ZoomQualityOfServiceEventCapture : Event data recieved with request body :  {requestBody}");


                if (data != null)
                {
                    if(data.payload.@object.participant.qos.Where(x => x.type == "audio_input") != null)
                    {
                        log.LogInformation($"ZoomQualityOfServiceEventCapture : qos type audio_input found");

                        foreach (var item in data.payload.@object.participant.qos.Where(x => x.type == "audio_input"))
                        {
                            // Extracting integer using regular expression
                            Match match = Regex.Match(item.details.max_jitter, @"\d+");
                            if (match.Success)
                            {
                                int jitterValue = int.Parse(match.Value);
                                log.LogInformation($"ZoomQualityOfServiceEventCapture : jitter Value : {jitterValue}");

                                if (jitterValue > 40)
                                {
                                    log.LogInformation($"ZoomQualityOfServiceEventCapture : jitter Value : {jitterValue}");

                                    var qosDataObj = new PoorQOSZoomData 
                                    { 
                                        ZoomMeetingId = data.payload.@object.uuid, 
                                        participantEmail= data.payload.@object.participant.email,
                                        qosType = "audio_input",
                                        max_jitter = item.details.max_jitter,
                                        min_jitter = item.details.min_jitter,
                                        avg_jitter = item.details.avg_jitter,
                                        avg_loss = item.details.avg_loss,
                                        max_loss = item.details.max_loss,

                                        //quality = item.details.quality,
                                        quality_score = item.details.quality_score,
                                        max_latency = item.details.max_jitter,
                                    };

                                    //store in db
                                    log.LogInformation($"ZoomQualityOfServiceEventCapture : qosDataObj : {qosDataObj}");

                                    await _meetingRespository.SavePoorQOSDetails(qosDataObj);

                                }
                            }
                            

                        }
                    }
                    //check for audio_input event type, then check whether jitter is less than 40.
                    //If greater than 40, then save in a table.
                    //Parallel.ForEach(dataList, async data =>
                    //{
                    //    try
                    //    {
                    //        if (data != null && !string.IsNullOrEmpty(data.sg_message_id) && data.sg_message_id.Contains("."))
                    //        {
                    //            data.Split_Message_Id = data.sg_message_id.Split(".")[0];
                    //        }
                    //        _ = await _sendGridRepository.SaveSendGridEventDetails(data);
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                    //        log.LogError($"ZoomQualityOfServiceEventCapture : Error at: {DateTime.Now}, Message : {ex.Message}");
                    //        return;
                    //    }
                    //});
                }

                return new OkObjectResult(true);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
