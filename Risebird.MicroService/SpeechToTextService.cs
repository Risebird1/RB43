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
using System.Linq;
using System.Text;

namespace Risebird.MicroService
{
    public class SpeechToTextService
    {

        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ISpeechToTextHelper _speechToTextHelper;
        private readonly string connectionString;
        private string result = string.Empty;
        public SpeechToTextService(IAzureHelper azureHelper, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISpeechToTextHelper speechToTextHelper)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _speechToTextHelper = speechToTextHelper;
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
                log.LogInformation($"SpeechToTextService Audio file recieved  and processing started at: {DateTime.Now}");
                RecognitionWithPushAudioStreamAsync(audioBlob.OpenReadAsync().Result).Wait();

                data.ConvertedText = result.ToString();
                log.LogInformation($"SpeechToTextService Audio file  processing completed at: {DateTime.Now}");
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
                    log.LogInformation($"SpeechToTextService Audio file text saved in azure table at: {DateTime.Now}");
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

        public async Task RecognitionWithPushAudioStreamAsync(Stream audioStream)
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription(_configuration["SpeechCreds:subscriptionKey"], _configuration["SpeechCreds:region"]); ;

            var stopRecognition = new TaskCompletionSource<int>();

            // Create a push stream
            using (var reader = new BinaryReader(audioStream))
            {
                using (var pushStream = AudioInputStream.CreatePushStream(_speechToTextHelper.readWaveHeader(reader)))
                {
                    using (var audioInput = AudioConfig.FromStreamInput(pushStream))
                    {
                        // Creates a speech recognizer using audio stream input.
                        using (var recognizer = new SpeechRecognizer(config, audioInput))
                        {
                            // Subscribes to events.
                            recognizer.Recognizing += (s, e) =>
                            {
                                //Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                            };

                            recognizer.Recognized += (s, e) =>
                            {
                                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                                {
                                    if(result==string.Empty)
                                    {
                                        result = e.Result.Text;
                                    }
                                    else
                                    {
                                        result = result + " " + e.Result.Text;

                                    }
                                    //Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                                }
                                else if (e.Result.Reason == ResultReason.NoMatch)
                                {
                                    //result=
                                    //Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                                }
                            };

                            recognizer.Canceled += (s, e) =>
                            {
                                //Console.WriteLine($"CANCELED: Reason={e.Reason}");

                                if (e.Reason == CancellationReason.Error)
                                {
                                    //Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                                    //Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                                    //Console.WriteLine($"CANCELED: Did you update the subscription info?");
                                }

                                stopRecognition.TrySetResult(0);
                            };

                            recognizer.SessionStarted += (s, e) =>
                            {
                                //Console.WriteLine("\nSession started event.");
                            };

                            recognizer.SessionStopped += (s, e) =>
                            {
                                //Console.WriteLine("\nSession stopped event.");
                                //Console.WriteLine("\nStop recognition.");
                                stopRecognition.TrySetResult(0);
                            };

                            // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                            // open and read the wave file and push the buffers into the recognizer
                            byte[] readBytes;
                            do
                            {
                                readBytes = reader.ReadBytes(1024);
                                pushStream.Write(readBytes, readBytes.Length);
                            } while (readBytes.Length > 0);

                            pushStream.Close();

                            // Waits for completion.
                            // Use Task.WaitAny to keep the task rooted.
                            Task.WaitAny(new[] { stopRecognition.Task });

                            // Stops recognition.
                            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                        }
                    }
                }

            }
        }

    }

}








