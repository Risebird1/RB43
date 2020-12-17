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

namespace Risebird.MicroService
{
    public class SpeechToTextService
    {

        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string connectionString;

        public SpeechToTextService(IAzureHelper azureHelper, IConfiguration configuration, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _azureHelper = azureHelper;
            _applicationErrorLogger = applicationErrorLogger;
            connectionString = _configuration["ConnectionString:DBConnectionString"];

        }
        [FunctionName("SpeechToTextService")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "SpeechToTextService")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"SpeechToTextService HTTP trigger function executed at: {DateTime.Now}");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            SpeechToTextDTO data = JsonConvert.DeserializeObject<SpeechToTextDTO>(requestBody);

            var speechConfig = SpeechConfig.FromSubscription(_configuration["SpeechCreds:subscriptionKey"], _configuration["SpeechCreds:region"]);


            var reader = new BinaryReader(data.AudioFile.OpenReadStream());
            var audioInputStream = AudioInputStream.CreatePushStream();
            var audioConfig = AudioConfig.FromStreamInput(audioInputStream);
            var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            byte[] readBytes;
            do
            {
                readBytes = reader.ReadBytes(1024);
                audioInputStream.Write(readBytes, readBytes.Length);
            } while (readBytes.Length > 0);

            var result = await recognizer.RecognizeOnceAsync();
            Console.WriteLine($"RECOGNIZED: Text={result.Text}");


            log.LogInformation($"SpeechToTextService HTTP trigger function completed at: {DateTime.Now}");

            return new OkObjectResult("");
        }

        
    }
}
