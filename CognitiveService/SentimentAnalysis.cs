using GleamTech.VideoUltimate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class SentimentAnalysis
    {
         [FunctionName("SentimentAnalysis")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "SentimentAnalysis")] HttpRequest req,
            ILogger log)
        {
            var accountEmail = req.Headers["accountEmail"].ToString();
            if (!string.IsNullOrEmpty(accountEmail))
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation(requestBody);
                SentimentDTO data = JsonConvert.DeserializeObject<SentimentDTO>(requestBody);
                log.LogInformation("C# HTTP trigger function processed a request.");
                //var str = ConfigurationManager //.ConnectionStrings["sqldb_connection"].ConnectionString;


                int frameCount = 1;
                var sentimentData = new List<VideoSentiment>();
                FaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials("95e31b589d194f98884d7b0e268a3300"))
                {
                    Endpoint = "https://sentimentapiresource.cognitiveservices.azure.com/"
                };

                using (var videoFrameReader = new VideoFrameReader(@"E:\EmotionAnalysisFiles\WIN_20200819_14_24_35_Pro.mp4"))
                {
                    foreach (var frame in videoFrameReader)
                    {
                        using (frame)
                        {
                            //byte[] data = new byte[0];
                            using (MemoryStream ms = new MemoryStream())
                            {
                                Image.GetThumbnailImageAbort myCallback =
                                            new Image.GetThumbnailImageAbort(ThumbnailCallback);
                                var img = frame.GetThumbnailImage(1280, 720, myCallback, IntPtr.Zero);
                                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Seek(0, 0);

                                var faces = (await faceClient.Face.DetectWithStreamAsync(ms,
                                            returnFaceId: false,
                                            returnFaceLandmarks: false,
                                            returnFaceAttributes: new FaceAttributeType[1] { FaceAttributeType.Emotion })).ToArray();

                                if (faces != null)
                                {
                                    var array = new double[8];
                                    array[0] = faces.FirstOrDefault().FaceAttributes.Emotion.Anger;
                                    array[1] = faces.FirstOrDefault().FaceAttributes.Emotion.Contempt;
                                    array[2] = faces.FirstOrDefault().FaceAttributes.Emotion.Disgust;
                                    array[3] = faces.FirstOrDefault().FaceAttributes.Emotion.Fear;
                                    array[4] = faces.FirstOrDefault().FaceAttributes.Emotion.Happiness;
                                    array[5] = faces.FirstOrDefault().FaceAttributes.Emotion.Neutral;
                                    array[6] = faces.FirstOrDefault().FaceAttributes.Emotion.Sadness;
                                    array[7] = faces.FirstOrDefault().FaceAttributes.Emotion.Surprise;

                                    var largest = array[0];
                                    var index = 0;
                                    for (int i = 1; i < 8; i++)
                                    {
                                        if (array[i] > largest)
                                        {
                                            largest = array[i];
                                            index = i;
                                        }
                                    }

                                    var sentimentType = index + 1;
                                    var sentimentValue = largest;
                                    var fileName = data.imid + "_Frame_" + frameCount;
                                    var imageUrl = await _azureStorageHelper.InsertBlobCloudCompare(fileName, "frameimages", ms);
                                    frameCount = frameCount + 1;
                                    sentimentData.Add(new VideoSentiment { InterviewId = data.imid, ImageUrl = imageUrl, EmotionType = sentimentType, Value = sentimentValue });

                                }
                                //var result = faceClient.Face.DetectWithStreamAsync(ms).Result;

                                //data = ms.ToArray();
                            }
                            //return data;


                        }
                    }
                }

                var result = await _meetingService.InsertVideoSentiment(sentimentData);
                
                return new OkObjectResult(result);
            }
            return new BadRequestResult();
        }


        public bool ThumbnailCallback()
        {
            return false;
        }
    }
}
