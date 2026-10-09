using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business
{
    /// <inheritdoc cref="IAIScreeningBlobPurgeService"/>
    public class AIScreeningBlobPurgeService : IAIScreeningBlobPurgeService
    {
        private const string JdContainerName = "resumes";
        private const string AudioContainerName = "audios";

        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IAIScreeningRepository _repository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;

        public AIScreeningBlobPurgeService(
            IAzureStorageHelper azureStorageHelper,
            IAIScreeningRepository repository,
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration)
        {
            _azureStorageHelper = azureStorageHelper;
            _repository = repository;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
        }

        public async Task<AIScreeningBlobPurgeResult> PurgeAsync()
        {
            var result = new AIScreeningBlobPurgeResult();

            if (!_configuration.GetValue("AIScreening:BlobPurge:Enabled", false))
            {
                result.Skipped = "AIScreening:BlobPurge:Enabled is false.";
                return result;
            }

            // Default true, so enabling the job reports before it ever deletes. Someone has to look
            // at a real list from a real environment and then turn the dry run off deliberately.
            result.DryRun = _configuration.GetValue("AIScreening:BlobPurge:DryRun", true);
            var retentionDays = _configuration.GetValue(
                "AIScreening:BlobPurge:RetentionDays", AIScreeningBlobRetention.DefaultRetentionDays);

            // Read what is in use FIRST. A null here means the read failed, and a purge that cannot
            // see what is referenced must do nothing at all rather than assume nothing is.
            var referencedUrls = await _repository.GetReferencedJdLinksAsync();
            if (referencedUrls == null)
            {
                result.Skipped = "The list of referenced job descriptions could not be read; nothing was deleted.";
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Error,
                    "AI screening blob purge aborted: IRF.JDLink could not be read, so no blob could be " +
                    "proven unreferenced. Nothing was deleted.");
                return result;
            }

            var referenced = AIScreeningBlobRetention.ToReferencedBlobNames(referencedUrls);
            result.ReferencedCount = referenced.Count;

            var nowUtc = DateTimeOffset.UtcNow;

            await PurgeContainerAsync(JdContainerName, nowUtc, retentionDays, referenced, result);
            await PurgeContainerAsync(AudioContainerName, nowUtc, retentionDays, referenced, result);

            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Info,
                $"AI screening blob purge{(result.DryRun ? " (dry run)" : "")}: examined {result.Examined}, " +
                $"{(result.DryRun ? "would delete" : "deleted")} {result.Deleted}, failed {result.Failed}, " +
                $"kept {result.ReferencedCount} referenced job descriptions clear.");

            return result;
        }

        private async Task PurgeContainerAsync(
            string containerName,
            DateTimeOffset nowUtc,
            int retentionDays,
            HashSet<string> referenced,
            AIScreeningBlobPurgeResult result)
        {
            try
            {
                var container = _azureStorageHelper.GetCloudBlobContainer(containerName);
                if (container == null) return;

                // Enumerated rather than listed by prefix: the two blob families have different
                // name shapes and the filter that matters is IsPurgeable, not the listing.
                await foreach (var item in container.Client.GetBlobsAsync())
                {
                    result.Examined++;

                    if (!AIScreeningBlobRetention.IsPurgeable(
                            item.Name, item.Properties?.LastModified, nowUtc, retentionDays, referenced))
                    {
                        continue;
                    }

                    result.Candidates.Add($"{containerName}/{item.Name}");

                    if (result.DryRun)
                    {
                        result.Deleted++;
                        continue;
                    }

                    try
                    {
                        await container.GetBlockBlobReference(item.Name).DeleteAsync();
                        result.Deleted++;
                    }
                    catch (Exception deleteEx)
                    {
                        result.Failed++;
                        await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, deleteEx);
                    }
                }
            }
            catch (Exception ex)
            {
                // One container failing must not stop the other, and must not look like success.
                result.Failed++;
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            }
        }
    }
}
