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
using System.Linq;

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
                                    data.ConvertedText = result.Text;
                                }
                            }
                        }
                    }
                    
                }




                var tableEntries = _azureStorageHelper.GetAllEntities<SpeechToTextServiceModel>("SpeechToTextTbl");
                var requiredEntry = new SpeechToTextServiceModel();
                if (tableEntries != null)
                {
                    requiredEntry = tableEntries.Where(x => x.InterviewId == data.InterviewId).FirstOrDefault();

                }
                if (requiredEntry == null)
                {
                    //insert new entry in the azure table

                    var modelData = new SpeechToTextServiceModel();
                    modelData.PartitionKey = Guid.NewGuid().ToString();
                    modelData.RowKey = Guid.NewGuid().ToString();
                    modelData.AudioFileUrl = data.AudioFileUrl;
                    modelData.ConvertedText = data.ConvertedText;
                    modelData.InterviewId = data.InterviewId;
                    modelData.KeyWords = data.KeyWords;

                    await _azureStorageHelper.InsertEntity("SpeechToTextTbl", modelData);

                }
                else
                {
                    //edit the existing entry in the azure table
                    var updateData = new SpeechToTextServiceModel();
                    updateData.PartitionKey = requiredEntry.PartitionKey;
                    updateData.RowKey = requiredEntry.RowKey;
                    updateData.AudioFileUrl = data.AudioFileUrl;
                    updateData.ConvertedText = data.ConvertedText;
                    updateData.InterviewId = requiredEntry.InterviewId;
                    updateData.KeyWords = data.KeyWords;

                    await _azureStorageHelper.InsertEntity("SpeechToTextTbl", updateData);

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





