using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Services.Proctoring
{
    public static class ProctoringAzureMediaLister
    {
        public static async Task<IReadOnlyList<ProctoringCapturedBlobViewModel>> ListForInterviewAsync(
            IAzureStorageHelper azureStorageHelper,
            IConfiguration configuration,
            int interviewId,
            string containerSettingKey,
            string kind)
        {
            var list = new List<ProctoringCapturedBlobViewModel>();
            if (interviewId <= 0 || azureStorageHelper == null || configuration == null)
                return list;

            var containerName = configuration[$"ProctoringSettings:{containerSettingKey}"];
            if (string.IsNullOrWhiteSpace(containerName))
                return list;

            try
            {
                var container = azureStorageHelper.GetCloudBlobContainer(containerName);
                var prefix = $"{interviewId}/";
                BlobContinuationToken continuation = null;
                do
                {
                    var segment = await container.ListBlobsSegmentedAsync(
                        prefix,
                        true,
                        BlobListingDetails.None,
                        null,
                        continuation,
                        null,
                        null).ConfigureAwait(false);
                    continuation = segment?.ContinuationToken;
                    if (segment?.Results == null)
                        continue;
                    foreach (var item in segment.Results)
                    {
                        var blob = item as CloudBlockBlob;
                        if (blob == null)
                            continue;
                        // Length is often unset when listing with BlobListingDetails.None; only skip known-empty blobs.
                        if (blob.Properties?.Length != null && blob.Properties.Length == 0)
                            continue;
                        list.Add(new ProctoringCapturedBlobViewModel
                        {
                            BlobUrl = blob.Uri?.ToString() ?? string.Empty,
                            CapturedAtUtc = blob.Properties.LastModified?.UtcDateTime,
                            Kind = kind,
                        });
                    }
                } while (continuation != null);
            }
            catch
            {
                return Array.Empty<ProctoringCapturedBlobViewModel>();
            }

            list.Sort((a, b) =>
            {
                var ta = a.CapturedAtUtc ?? DateTime.MinValue;
                var tb = b.CapturedAtUtc ?? DateTime.MinValue;
                return ta.CompareTo(tb);
            });
            return list;
        }
    }
}
