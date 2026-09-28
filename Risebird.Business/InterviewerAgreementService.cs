using Microsoft.Extensions.Options;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Threading.Tasks;

namespace Risebird.Business
{
    /// <summary>
    /// Interviewer service agreement: the interviewer reads the agreement, enters their details and signs;
    /// the signature image and a screenshot of the signing page (proof) go to blob storage and the details are
    /// saved on InterviewerProfile.
    /// </summary>
    public class InterviewerAgreementService : IInterviewerAgreementService
    {
        private readonly IInterviewerAgreementRepository _agreementRepository;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ServiceAgreementOptions _options;

        public InterviewerAgreementService(IInterviewerAgreementRepository agreementRepository, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IOptions<ServiceAgreementOptions> options)
        {
            _agreementRepository = agreementRepository;
            _azureStorageHelper = azureStorageHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _options = options.Value;
        }

        public Task<InterviewerAgreementVM> GetInterviewerAgreement(int interviewerProfileId) =>
            _agreementRepository.GetInterviewerAgreement(interviewerProfileId);

        public async Task<string> SignAgreement(int interviewerProfileId, InterviewerAgreementSignRequest request)
        {
            try
            {
                var agreement = await _agreementRepository.GetInterviewerAgreement(interviewerProfileId);
                if (agreement == null)
                    return "Could not load your profile. Please try again later.";
                if (agreement.IsAgreementSigned)
                    return "The agreement is already signed.";

                var id = Guid.NewGuid().ToString("N");
                var signatureUrl = await UploadImage($"InterviewerAgreementSignature_{interviewerProfileId}_{id}.png", "image/png", request.SignatureImage);
                var proofUrl = await UploadProof($"InterviewerAgreementProof_{interviewerProfileId}_{id}.jpg", request.ProofImage);

                var saved = await _agreementRepository.SaveInterviewerAgreement(interviewerProfileId, _options.Version,
                    request.SignerName, request.FatherName, request.Address, signatureUrl, proofUrl);
                return saved ? null : "Your signature could not be saved. Please try again.";
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "The agreement could not be signed right now. Please try again later.";
            }
        }

        // The proof screenshot is best effort: without it (or if its upload fails) the agreement is still saved.
        private async Task<string> UploadProof(string fileName, byte[] content)
        {
            if (content == null || content.Length == 0)
                return null;
            try
            {
                return await UploadImage(fileName, "image/jpeg", content);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        private async Task<string> UploadImage(string fileName, string contentType, byte[] content)
        {
            var blob = _azureStorageHelper.GetCloudBlockBlob(_options.SignatureContainer, fileName);
            blob.Properties.ContentType = contentType;
            await blob.UploadFromByteArrayAsync(content, 0, content.Length);
            return blob.Uri.ToString();
        }
    }
}
