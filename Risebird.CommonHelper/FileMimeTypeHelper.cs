using FileTypeInterrogator;
using Microsoft.AspNetCore.Http;
using Risebird.CommonHelper.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Risebird.CommonHelper
{
    public class FileMimeTypeHelper: IFileMimeTypeHelper
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
            return fileTypeInfo.MimeType.ToLower();
        }
    }
}
