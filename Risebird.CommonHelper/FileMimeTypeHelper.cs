using FileTypeInterrogator;
using Microsoft.AspNetCore.Http;
using Risebird.CommonHelper.Interface;
using System;
using System.IO;

namespace Risebird.CommonHelper
{
    public class FileMimeTypeHelper : IFileMimeTypeHelper
    {

        public string GetFileMimeType(IFormFile file)
        {
            // You should have checked for null and file length before reaching here

            IFileTypeInterrogator interrogator = new FileTypeInterrogator.FileTypeInterrogator();

            byte[] fileBytes;
            using (var stream = new MemoryStream())
            {
                file.CopyTo(stream);
                fileBytes = stream.ToArray();
            }

            FileTypeInfo fileTypeInfo = interrogator.DetectType(fileBytes);
            return Mp4BrandMimeType(fileBytes) ?? fileTypeInfo?.MimeType?.ToLower() ?? string.Empty;
        }

        public string GetFileMimeTypeFromByteArray(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length == 0)
                throw new ArgumentException("File bytes are null or empty.", nameof(fileBytes));

            IFileTypeInterrogator interrogator = new FileTypeInterrogator.FileTypeInterrogator();
            FileTypeInfo fileTypeInfo = interrogator.DetectType(fileBytes);

            return Mp4BrandMimeType(fileBytes) ?? fileTypeInfo?.MimeType?.ToLower() ?? string.Empty;
        }

        /// <summary>
        /// MP4 brands FileTypeInterrogator misreads. It only knows isom/mp42, and reports the
        /// fragmented MP4 that Safari/iOS MediaRecorder writes (iso5, iso6, mp41, avc1 ...) as
        /// text/plain and M4V as video/x-flv - so every iPhone recording was rejected as an invalid
        /// video type ("Error saving video Response").
        /// </summary>
        private static readonly string[] Mp4VideoBrands =
        {
            "isom", "iso2", "iso3", "iso4", "iso5", "iso6", "iso7", "iso8", "iso9",
            "mp41", "mp42", "avc1", "dash", "cmfc", "cmf2", "M4V ", "msnv"
        };

        /// <summary>video/mp4 when the bytes are an ISO base media file with an MP4 video brand; otherwise null.</summary>
        public static string Mp4BrandMimeType(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length < 12) return null;
            if (fileBytes[4] != (byte)'f' || fileBytes[5] != (byte)'t' || fileBytes[6] != (byte)'y' || fileBytes[7] != (byte)'p')
                return null;

            var brand = System.Text.Encoding.ASCII.GetString(fileBytes, 8, 4);
            return Array.IndexOf(Mp4VideoBrands, brand) >= 0 ? "video/mp4" : null;
        }
    }
}
