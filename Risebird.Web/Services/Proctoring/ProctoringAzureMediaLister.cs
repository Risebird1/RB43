using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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
                var container = azureStorageHelper.GetCloudBlobContainer(containerName).Client;
                var prefix = $"{interviewId}/";
                await foreach (var item in container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, default).ConfigureAwait(false))
                {
                    // Length is often unset when listing; only skip known-empty blobs.
                    if (item.Properties?.ContentLength != null && item.Properties.ContentLength == 0)
                        continue;
                    list.Add(new ProctoringCapturedBlobViewModel
                    {
                        BlobUrl = container.GetBlobClient(item.Name).Uri?.ToString() ?? string.Empty,
                        CapturedAtUtc = item.Properties.LastModified?.UtcDateTime,
                        Kind = kind,
                    });
                }
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
