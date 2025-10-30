using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.Data.RekognitionModels
{
    public class Sub
    {

    }
    public class FaceDetectionResultDto
    {
        public long? Timestamp { get; set; }
        public float? Confidence { get; set; }
        public BoundingBoxDto BoundingBox { get; set; } = new();
        public List<EmotionDto> Emotions { get; set; } = new();
    }

    public class BoundingBoxDto
    {
        public float? Left { get; set; }
        public float? Top { get; set; }
        public float? Width { get; set; }
        public float? Height { get; set; }
    }

    public class EmotionDto
    {
        public string Type { get; set; } = string.Empty;
        public float? Confidence { get; set; }
    }

    public class VideoAnalysisResponseDto
    {
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
        public List<FaceDetectionResultDto> Faces { get; set; } = new();
    }

    public class AnalyzeVideoRequest
    {
        public string BucketName { get; set; } = string.Empty;
        public string VideoKey { get; set; } = string.Empty;
        public string SnsTopicArn { get; set; } = string.Empty;
        public string RoleArn { get; set; } = string.Empty;
    }

    public class VideoMetadataEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string S3Bucket { get; set; } = string.Empty;
        public string S3Key { get; set; } = string.Empty;
        public string RekognitionJobId { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "UPLOADED";
    }
}
