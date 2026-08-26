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
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    public class AWSRekognitionService : IAWSRekognitionService
    {
        private const int StreamCopyBufferSize = 81920;
        private static readonly TimeSpan DefaultVideoDownloadTimeout = TimeSpan.FromMinutes(15);

        // Shared so repeated analyses don't churn sockets. The per-download budget is
        // enforced with a CancellationToken instead of HttpClient.Timeout, which would
        // otherwise cap every download at the same fixed ceiling.
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly string topicArn;
        private readonly string roleArn;
        private readonly string bucketName;
        private readonly TimeSpan videoDownloadTimeout;
        //private readonly IAmazonS3 _s3Client;
        //private readonly IAmazonRekognition _rekognition;

        public AWSRekognitionService(IApplicationErrorLogger applicationErrorLogger, IConfiguration configuration)
        {
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
            topicArn = _configuration["AWSRekognition:TopicArn"];
            roleArn = _configuration["AWSRekognition:RoleArn"];
            bucketName = _configuration["AWSRekognition:BucketName"];
            videoDownloadTimeout =
                int.TryParse(_configuration["AWSRekognition:VideoDownloadTimeoutMinutes"], out var timeoutMinutes) && timeoutMinutes > 0
                    ? TimeSpan.FromMinutes(timeoutMinutes)
                    : DefaultVideoDownloadTimeout;
            //_s3Client = s3Client;
            //_rekognition = rekognition;
        }

        public async Task<string> UploadVideoAsync(string videoUrl)
        {
            // Spool to disk rather than uploading straight from the response stream:
            // TransferUtility needs a seekable stream to size the upload, and buffering a
            // full interview video in memory is what pushed the old download past its timeout.
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"rb_sentiment_{Guid.NewGuid():N}.tmp");

            try
            {
                using var downloadCts = new CancellationTokenSource(videoDownloadTimeout);
                string contentType;

                using (var response = await httpClient.GetAsync(videoUrl, HttpCompletionOption.ResponseHeadersRead, downloadCts.Token))
                {
                    response.EnsureSuccessStatusCode();
                    contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                    await using var downloadStream = await response.Content.ReadAsStreamAsync(downloadCts.Token);
                    await using var tempFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write,
                        FileShare.None, StreamCopyBufferSize, useAsync: true);
                    await downloadStream.CopyToAsync(tempFileStream, StreamCopyBufferSize, downloadCts.Token);
                }

                // Generate a file name from the URL or a GUID
                string fileName = Path.GetFileName(new Uri(videoUrl).LocalPath);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"file_{Guid.NewGuid()}";
                }

                await using var uploadStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, StreamCopyBufferSize, useAsync: true);

                if (uploadStream.Length == 0)
                {
                    throw new InvalidOperationException($"Downloaded video from '{videoUrl}' was empty; nothing to analyse.");
                }

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = uploadStream,
                    Key = $"uploads/{Guid.NewGuid()}_{fileName}",
                    BucketName = bucketName,
                    ContentType = contentType
                };

                using var _s3Client = new AmazonS3Client("AKIARVQVPFDBKUR6OYIU", "ybr8/aERuORrZ0Y5p/NuGoL+8DFUZNz0ZRc8GEWh", RegionEndpoint.USEast2);

                using var transfer = new TransferUtility(_s3Client);
                await transfer.UploadAsync(uploadRequest);

                return uploadRequest.Key;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch (IOException)
                {
                    // A leftover temp file is not worth failing the analysis over.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
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
            using var _s3Client = new AmazonS3Client("AKIARVQVPFDBKUR6OYIU", "ybr8/aERuORrZ0Y5p/NuGoL+8DFUZNz0ZRc8GEWh", RegionEndpoint.USEast2);

            using var transfer = new TransferUtility(_s3Client);
            await transfer.UploadAsync(uploadRequest);

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
