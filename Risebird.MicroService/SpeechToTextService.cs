using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Risebird.Entities.Enum;
using MicrosoftSpeechSDKSamples;

namespace Risebird.MicroService
{
    public class SpeechToTextService
    {

        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string connectionString;

        public SpeechToTextService(IAzureHelper azureHelper, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }
        
        [FunctionName("SpeechToTextService")]
        public async Task<IActionResult> Run(
           [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "SpeechToTextService")] HttpRequest req,
           ILogger log)
        {
            string path = string.Empty;
            try
            {
                log.LogInformation($"SpeechToTextService HTTP trigger function executed at: {DateTime.Now}");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                SpeechToTextServiceModel data = JsonConvert.DeserializeObject<SpeechToTextServiceModel>(requestBody);
                var urlArray = data.AudioFileUrl.Split("/");
                var urlLength = urlArray.Length;
                var fileName = urlArray[urlLength - 1];
                var audioBlob = _azureStorageHelper.GetBlobDataAsBlob("audios", fileName);

                var speechConfig = SpeechConfig.FromSubscription(_configuration["SpeechCreds:subscriptionKey"], _configuration["SpeechCreds:region"]);


                using (Stream audioStream = audioBlob.OpenReadAsync().Result)
                {

                    using (var reader = new BinaryReader(audioStream))
                    {
                        using (var audioInputStream = AudioInputStream.CreatePushStream(Helper.readWaveHeader(reader)))
                        {
                            byte[] readBytes;
                            do
                            {
                                readBytes = reader.ReadBytes(Convert.ToInt32(audioStream.Length));
                                audioInputStream.Write(readBytes, readBytes.Length);
                            } while (readBytes.Length > 0);
                            using (var audioConfig = AudioConfig.FromStreamInput(audioInputStream))
                            {
                                using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
                                {

                                    var result = await recognizer.RecognizeOnceAsync();
                                    Console.WriteLine($"RECOGNIZED: Text={result.Text}");
                                }
                            }
                        }
                    }
                    
                }

                
                log.LogInformation($"SpeechToTextService HTTP trigger function completed at: {DateTime.Now}");

                return new OkObjectResult("Successful");

            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }


        }

    }
}




//var audioConfig = AudioConfig.FromWavFileInput(@"c:\Users\prashantvm\Desktop\Recording\One.wav");
//var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

//var result = await recognizer.RecognizeOnceAsync();





