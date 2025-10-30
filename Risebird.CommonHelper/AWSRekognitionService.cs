using Amazon;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.RekognitionModels;
using Risebird.Entities.Enum;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class AWSRekognitionService : IAWSRekognitionService
    {
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string topicArn;
        private readonly string roleArn;
        private readonly string bucketName;
        //private readonly IAmazonS3 _s3Client;
        //private readonly IAmazonRekognition _rekognition;

        public AWSRekognitionService(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            topicArn = _configuration["AWSRekognition:TopicArn"];
            roleArn = _configuration["AWSRekognition:RoleArn"];
            bucketName = _configuration["AWSRekognition:BucketName"];
            //_s3Client = s3Client;
            //_rekognition = rekognition;
        }

        public async Task<string> UploadVideoAsync(string videoUrl)
        {
            using var httpClient = new HttpClient();

            // Download the file as a stream
            using var response = await httpClient.GetAsync(videoUrl);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            // Generate a file name from the URL or a GUID
            string fileName = Path.GetFileName(new Uri(videoUrl).LocalPath);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"file_{Guid.NewGuid()}";
            }

            using var stream = await response.Content.ReadAsStreamAsync();

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = $"uploads/{Guid.NewGuid()}_{fileName}",
                BucketName = bucketName,
                ContentType = contentType
            };

            var _s3Client = new AmazonS3Client("AKIARVQVPFDBKUR6OYIU", "ybr8/aERuORrZ0Y5p/NuGoL+8DFUZNz0ZRc8GEWh", RegionEndpoint.USEast2);

            var transfer = new TransferUtility(_s3Client);
            await transfer.UploadAsync(uploadRequest);
            await stream.DisposeAsync();

            return uploadRequest.Key;
        }

        public async Task<string> UploadVideoAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = $"uploads/{Guid.NewGuid()}_{file.FileName}",
                BucketName = bucketName,
                ContentType = file.ContentType
            };
            var _s3Client = new AmazonS3Client("AKIARVQVPFDBKUR6OYIU", "ybr8/aERuORrZ0Y5p/NuGoL+8DFUZNz0ZRc8GEWh", RegionEndpoint.USEast2);

            var transfer = new TransferUtility(_s3Client);
            await transfer.UploadAsync(uploadRequest);
            await stream.DisposeAsync();

            return uploadRequest.Key;
        }

        public async Task<VideoAnalysisResponseDto> AnalyzeVideoAsync(string videoKey)
        {
            var startRequest = new StartFaceDetectionRequest
            {
                Video = new Video
                {
                    S3Object = new S3Object { Bucket = bucketName, Name = videoKey }
                },
                NotificationChannel = new NotificationChannel
                {
                    SNSTopicArn = topicArn,
                    RoleArn = roleArn
                },
                FaceAttributes = FaceAttributes.ALL
            };
            var _rekognition = new AmazonRekognitionClient("AKIARVQVPFDBKUR6OYIU", "ybr8/aERuORrZ0Y5p/NuGoL+8DFUZNz0ZRc8GEWh", RegionEndpoint.USEast2);

            var startResponse = await _rekognition.StartFaceDetectionAsync(startRequest);
            var jobId = startResponse.JobId;


            var video = new VideoMetadataEntity
            {
                S3Bucket = bucketName,
                S3Key = videoKey,
                RekognitionJobId = jobId,
                Status = "PROCESSING"
            };
            //_db.Videos.Add(video);
            //await _db.SaveChangesAsync();

            // Poll for job completion
            GetFaceDetectionResponse? response = null;
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                response = await _rekognition.GetFaceDetectionAsync(new GetFaceDetectionRequest
                {
                    JobId = jobId,
                    MaxResults = 1000
                });
            }
            while (response.JobStatus != VideoJobStatus.SUCCEEDED &&
                   response.JobStatus != VideoJobStatus.FAILED);

            video.Status = response.JobStatus.Value;
            //await _db.SaveChangesAsync();

            if (response.JobStatus == VideoJobStatus.FAILED)
                throw new Exception($"Rekognition job {jobId} failed.");

            // Extract results
            var result = new VideoAnalysisResponseDto
            {
                JobId = jobId,
                Status = response.JobStatus.Value
            };

            foreach (var fd in response.Faces)
            {
                var face = fd.Face;
                var dto = new FaceDetectionResultDto
                {
                    Timestamp = fd.Timestamp,
                    Confidence = face.Confidence,
                    BoundingBox = new BoundingBoxDto
                    {
                        Left = face.BoundingBox.Left,
                        Top = face.BoundingBox.Top,
                        Width = face.BoundingBox.Width,
                        Height = face.BoundingBox.Height
                    }
                };

                foreach (var emotion in face.Emotions)
                {
                    dto.Emotions.Add(new EmotionDto
                    {
                        Type = emotion.Type,
                        Confidence = emotion.Confidence
                    });
                }

                result.Faces.Add(dto);
            }
            //string jsonResult = JsonConvert.SerializeObject(result);
            return result;
        }

        
    }

}
