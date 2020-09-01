using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using GleamTech.VideoUltimate;
using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace CognitiveService
{
   
    public static class SentimentAnalysis
    {
        [FunctionName("SentimentAnalysis")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            string requestBody = await req.Content.ReadAsStringAsync();
            SentimentDTO data = JsonConvert.DeserializeObject<SentimentDTO>(requestBody);

            var videoBlob = GetBlobData("sentimentvideos", data.VideoFileName);
            var videoStream = videoBlob.OpenReadAsync().Result;
            videoStream.Seek(0, 0);
            try
            {
                int frameCount = 1;
                var sentimentData = new List<VideoSentimentService>();
                FaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials("95e31b589d194f98884d7b0e268a3300"))
                {
                    Endpoint = "https://sentimentapiresource.cognitiveservices.azure.com/"
                };


                using (var videoFrameReader = new VideoFrameReader(videoStream))
                {
                    foreach (var frame in videoFrameReader)
                    {
                        using (frame)
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                Image.GetThumbnailImageAbort myCallback =
                                            new Image.GetThumbnailImageAbort(ThumbnailCallback);
                                var img = frame.GetThumbnailImage(1280, 720, myCallback, IntPtr.Zero);
                                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                ms.Seek(0, 0);
                                MemoryStream dest = new MemoryStream();
                                ms.CopyTo(dest);
                                dest.Seek(0, 0);
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

                                    var fileName = data.imid + "_Frame_" + frameCount + ".png";
                                    var imageUrl = await InsertBlobCloudCompare(fileName, "frameimages", dest);
                                    sentimentData.Add(new VideoSentimentService { InterviewId = data.imid, EmotionType = sentimentType, Value = sentimentValue, ImageUrl = imageUrl });
                                    frameCount = frameCount + 1;
                                }

                            }



                        }
                    }
                    
                }

                var resultObj = JsonConvert.SerializeObject(sentimentData);
                return req.CreateResponse(HttpStatusCode.OK, resultObj);
            }
            catch (Exception ex)
            {

                return req.CreateResponse(HttpStatusCode.BadRequest, ex.Message); ;
            }

        
        }

        public static bool ThumbnailCallback()
        {
            return false;
        }

        public static CloudBlockBlob GetCloudBlockBlobClient(string containerName, string filename)
        {

            StorageCredentials creds = new StorageCredentials("risebirdportal", "2AKmR4SJlNvtr0BY5h7gVJANY7vBcCqph7KWywH9cqLcel/cxT0fKLXyGUVrSj3Xj89HUf2Z0QuWmQ8Zd5SqeQ==");
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);

            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            if (!blobContainer.ExistsAsync().Result)
            {
                blobContainer.CreateIfNotExistsAsync();
                var permission = new BlobContainerPermissions();
                permission.PublicAccess = BlobContainerPublicAccessType.Container;
                blobContainer.SetPermissionsAsync(permission);
            }
            CloudBlockBlob blob = blobClient.GetContainerReference(containerName).GetBlockBlobReference(filename);

            return blob;
        }


        public static async Task<string> InsertBlobCloudCompare(string filename, string containerName, MemoryStream blobFile)
        {
            CloudBlockBlob blob;

            blob = GetCloudBlockBlobClient(containerName, filename);
            if (blobFile != null)
            {
                await blob.UploadFromStreamAsync(blobFile);
            }
            else
            {
                return null;
            }

            return blob.Uri.ToString();
        }
        
        public static CloudBlob GetBlobData(string containerName, string fileName)
        {
            try
            {
                CloudBlobContainer blobContainer = GetCloudBlobContainer(containerName);
                CloudBlob blob = blobContainer.GetBlobReference(fileName);
                
                return blob;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static CloudBlobContainer GetCloudBlobContainer(string containerName)
        {
            CloudStorageAccount account = GetStorageAccount();
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            if (!blobContainer.ExistsAsync().Result)
            {
                blobContainer.CreateIfNotExistsAsync();
                var permission = new BlobContainerPermissions();
                permission.PublicAccess = BlobContainerPublicAccessType.Container;
                blobContainer.SetPermissionsAsync(permission);
            }
            return blobContainer;
        }

        public static CloudStorageAccount GetStorageAccount()
        {
            StorageCredentials creds = new StorageCredentials("risebirdportal", "2AKmR4SJlNvtr0BY5h7gVJANY7vBcCqph7KWywH9cqLcel/cxT0fKLXyGUVrSj3Xj89HUf2Z0QuWmQ8Zd5SqeQ==");
            CloudStorageAccount account = new CloudStorageAccount(creds, useHttps: true);
            return account;
        }
    }


    public class SentimentDTO
    {
        public int imid { get; set; }
        public string VideoFileName { get; set; }
    }

    [Serializable]
    public class VideoSentimentService
    {
        public int InterviewId { get; set; }
        public string ImageUrl { get; set; }
        public int EmotionType { get; set; }
        public double Value { get; set; }
    }
}
