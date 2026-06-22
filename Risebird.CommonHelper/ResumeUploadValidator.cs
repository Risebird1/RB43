using Microsoft.AspNetCore.Http;
using Risebird.CommonHelper.Interface;
using System;
using System.Collections.Generic;
using System.IO;

namespace Risebird.CommonHelper
{
    /// <summary>Validates interviewer/candidate resume uploads (.pdf, .doc, .docx).</summary>
    public static class ResumeUploadValidator
    {
        public const string InvalidFileMessage = "Please select valid file for import(.pdf, .doc, .docx)!";

        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx"
        };

        private static readonly HashSet<string> AllowedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword",
            "application/vnd.ms-word",
            "application/x-msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-office",
            "application/zip",
            "application/x-zip-compressed",
            "application/x-zip",
            "application/octet-stream"
        };

        private static readonly HashSet<string> BlockedMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/x-msdownload",
            "application/x-msdos-program",
            "application/x-executable",
            "application/x-dosexec",
            "application/java-archive",
            "application/x-sh",
            "text/html"
        };

        public static bool IsAllowedResumeUpload(IFormFile file, IFileMimeTypeHelper mimeHelper, out string errorMessage)
        {
            errorMessage = null;
            if (file == null || file.Length <= 0)
            {
                errorMessage = InvalidFileMessage;
                return false;
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                errorMessage = InvalidFileMessage;
                return false;
            }

            string mimeType = null;
            try
            {
                mimeType = mimeHelper?.GetFileMimeType(file);
            }
            catch
            {
                // Sniffing failed — extension already validated.
                return true;
            }

            if (string.IsNullOrWhiteSpace(mimeType))
                return true;

            mimeType = mimeType.Trim().ToLowerInvariant();
            if (AllowedMimeTypes.Contains(mimeType))
                return true;

            if (IsResumeMimeCompatibleWithExtension(mimeType, extension))
                return true;

            if (BlockedMimeTypes.Contains(mimeType))
            {
                errorMessage = InvalidFileMessage;
                return false;
            }

            // Office/PDF sniffers are inconsistent; trust the validated extension.
            return true;
        }

        private static bool IsResumeMimeCompatibleWithExtension(string mimeType, string extension)
        {
            if (extension == ".pdf")
                return mimeType.Contains("pdf", StringComparison.Ordinal);

            if (extension == ".doc")
            {
                return mimeType.Contains("word", StringComparison.Ordinal)
                    || mimeType.Contains("msword", StringComparison.Ordinal)
                    || mimeType.Contains("office", StringComparison.Ordinal);
            }

            if (extension == ".docx")
            {
                return mimeType.Contains("word", StringComparison.Ordinal)
                    || mimeType.Contains("office", StringComparison.Ordinal)
                    || mimeType.Contains("zip", StringComparison.Ordinal)
                    || mimeType.Contains("openxml", StringComparison.Ordinal);
            }

            return false;
        }
    }
}
