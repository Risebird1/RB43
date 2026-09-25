using Microsoft.Extensions.Options;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    /// <summary>
    /// Interviewer service agreement, e-signed through Zoho Sign. State is kept on
    /// InterviewerProfile (Agreement* columns); Zoho Sign is always the source of truth for status.
    /// </summary>
    public class InterviewerAgreementService : IInterviewerAgreementService
    {
        private readonly IInterviewerAgreementRepository _agreementRepository;
        private readonly IZohoSignHelper _zohoSignHelper;
        private readonly IInterviewerAgreementDocumentProvider _documentProvider;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly ZohoSignOptions _options;

        public InterviewerAgreementService(IInterviewerAgreementRepository agreementRepository, IZohoSignHelper zohoSignHelper,
            IInterviewerAgreementDocumentProvider documentProvider, IAzureStorageHelper azureStorageHelper,
            IApplicationErrorLogger applicationErrorLogger, IOptions<ZohoSignOptions> options)
        {
            _agreementRepository = agreementRepository;
            _zohoSignHelper = zohoSignHelper;
            _documentProvider = documentProvider;
            _azureStorageHelper = azureStorageHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _options = options.Value;
        }

        public async Task<byte[]> GetAgreementDocumentForReview()
        {
            try
            {
                return (await _documentProvider.GetDocumentAsync()).Pdf;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public Task<InterviewerAgreementVM> GetInterviewerAgreement(int interviewerProfileId) =>
            _agreementRepository.GetInterviewerAgreement(interviewerProfileId);

        public Task<InterviewerAgreementVM> GetInterviewerAgreementByZohoRequestId(string zohoRequestId) =>
            _agreementRepository.GetInterviewerAgreementByZohoRequestId(zohoRequestId);

        public async Task<(InterviewerAgreementVM Agreement, string Message)> InitiateAgreement(int interviewerProfileId, string returnUrl)
        {
            try
            {
                var agreement = await _agreementRepository.GetInterviewerAgreement(interviewerProfileId);
                if (agreement == null)
                    return (null, "Could not load your agreement details. Please try again later.");
                if (agreement.IsAgreementSigned)
                    return (agreement, "The agreement is already signed.");
                if (agreement.AgreementStatus == InterviewerAgreementStatus.InterviewerSigned)
                    return (agreement, "You have signed the agreement. It is awaiting Risebird's countersignature.");
                if (agreement.HasOpenRequest)
                    return (agreement, null);   // continue signing the request already sent

                if (!_zohoSignHelper.IsConfigured)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        "Interviewer agreement: Zoho Sign is not configured (ZohoSign:ClientId / ClientSecret / RefreshToken are empty).");
                    return (null, "E-signing is not set up yet. Please contact Risebird support.");
                }

                // Name, father's name and address are filled in by the interviewer inside Zoho Sign
                // while reading the agreement; they are read back once the interviewer has signed.
                var document = await _documentProvider.GetDocumentAsync();
                var zohoRequest = await _zohoSignHelper.SendInterviewerAgreementAsync(new ZohoSignAgreementRequest
                {
                    RecipientName = agreement.FullName,
                    RecipientEmail = agreement.EmailId,
                    ReturnUrl = returnUrl
                }, document);

                var saved = await _agreementRepository.SaveInterviewerAgreementRequest(interviewerProfileId, zohoRequest,
                    _options.InterviewerAgreementVersion);
                if (!saved)
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        $"Zoho Sign agreement request {zohoRequest.RequestId} created but not saved for InterviewerProfileId {interviewerProfileId}.");
                    return (null, "The agreement could not be saved. Please try again.");
                }
                return (await _agreementRepository.GetInterviewerAgreement(interviewerProfileId), null);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return (null, "The agreement could not be generated right now. Please try again later.");
            }
        }

        public async Task<bool> ResendAgreementEmail(int interviewerProfileId)
        {
            try
            {
                var agreement = await _agreementRepository.GetInterviewerAgreement(interviewerProfileId);
                if (agreement == null || !agreement.HasOpenRequest)
                    return false;
                await _zohoSignHelper.SendReminderAsync(agreement.AgreementZohoRequestId);
                return true;
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<string> GetEmbeddedSignUrl(int interviewerProfileId, string host)
        {
            try
            {
                var agreement = await _agreementRepository.GetInterviewerAgreement(interviewerProfileId);
                if (agreement == null || !agreement.HasOpenRequest || string.IsNullOrEmpty(agreement.AgreementZohoActionId))
                    return null;
                return await _zohoSignHelper.GetEmbeddedSignUrlAsync(agreement.AgreementZohoRequestId, agreement.AgreementZohoActionId, host);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<InterviewerAgreementVM> SyncAgreementStatus(string zohoRequestId)
        {
            try
            {
                var agreement = await _agreementRepository.GetInterviewerAgreementByZohoRequestId(zohoRequestId);
                if (agreement == null)
                    return null;   // not ours, or superseded by a newer request
                if (agreement.IsAgreementSigned && !string.IsNullOrEmpty(agreement.AgreementDocumentUrl) && !string.IsNullOrEmpty(agreement.AgreementAddress))
                    return agreement;

                var details = await _zohoSignHelper.GetRequestDetailsAsync(zohoRequestId, agreement.AgreementZohoActionId);
                var status = MapStatus(details, agreement.AgreementStatus);
                var isSigned = status == InterviewerAgreementStatus.Completed;
                var interviewerHasSigned = isSigned || status == InterviewerAgreementStatus.InterviewerSigned;
                var now = DateTime.UtcNow.AddMinutes(330);

                string documentUrl = null;
                if (isSigned && string.IsNullOrEmpty(agreement.AgreementDocumentUrl))
                    documentUrl = await StoreSignedDocument(agreement);

                string signerName = null, fatherName = null, address = null;
                if (interviewerHasSigned && string.IsNullOrEmpty(agreement.AgreementAddress))
                    (signerName, fatherName, address) = await ReadFilledDetails(zohoRequestId);

                await _agreementRepository.UpdateInterviewerAgreementStatus(zohoRequestId, status, isSigned,
                    signedOn: interviewerHasSigned && agreement.AgreementSignedOn == null ? details.SignedOn ?? now : null,
                    completedOn: isSigned && agreement.AgreementCompletedOn == null ? now : null,
                    documentUrl: documentUrl,
                    declineReason: status == InterviewerAgreementStatus.Declined ? Truncate(details.DeclineReason, 500) : null,
                    signerName: signerName,
                    fatherName: fatherName,
                    address: address);

                return await _agreementRepository.GetInterviewerAgreementByZohoRequestId(zohoRequestId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<byte[]> GetSignedDocument(InterviewerAgreementVM agreement)
        {
            try
            {
                if (agreement == null || !agreement.IsAgreementSigned)
                    return null;

                if (!string.IsNullOrEmpty(agreement.AgreementDocumentUrl))
                {
                    var blob = _azureStorageHelper.GetBlobDataAsBlob(_options.SignedDocumentContainer, agreement.AgreementDocumentUrl.Split('/').Last());
                    if (blob != null)
                    {
                        using var ms = new MemoryStream();
                        await blob.DownloadToStreamAsync(ms);
                        return ms.ToArray();
                    }
                }
                // Portal-signed agreements only exist in blob storage.
                if (IsPortalReference(agreement.AgreementZohoRequestId))
                    return null;
                // Not stored yet (e.g. the blob upload failed during sync): serve it from Zoho Sign.
                return await _zohoSignHelper.DownloadSignedDocumentAsync(agreement.AgreementZohoRequestId);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<(InterviewerAgreementVM Agreement, string Message)> SignInPortal(int interviewerProfileId, PortalSignatureStamp stamp)
        {
            try
            {
                var agreement = await _agreementRepository.GetInterviewerAgreement(interviewerProfileId);
                if (agreement == null)
                    return (null, "Could not load your agreement details. Please try again later.");
                if (agreement.IsAgreementSigned)
                    return (agreement, "The agreement is already signed.");

                // The reference is stored in the request id column; "PORTAL-" marks portal signing.
                stamp.InterviewerProfileId = interviewerProfileId;
                stamp.SignerEmail = agreement.EmailId;
                stamp.Reference = $"{PortalReferencePrefix}{Guid.NewGuid():N}".ToUpperInvariant();
                stamp.SignedOn = DateTime.UtcNow.AddMinutes(330);

                var document = await _documentProvider.GetDocumentAsync();
                var signedPdf = _documentProvider.StampPortalSignature(document, stamp);

                // The signed PDF is the record of the signature: only mark it signed once it is stored.
                var documentUrl = await UploadSignedPdf(signedPdf, interviewerProfileId, stamp.Reference);

                var reference = new ZohoSignRequestResult { RequestId = stamp.Reference };
                if (!await _agreementRepository.SaveInterviewerAgreementRequest(interviewerProfileId, reference, _options.InterviewerAgreementVersion)
                    || !await _agreementRepository.UpdateInterviewerAgreementStatus(stamp.Reference, InterviewerAgreementStatus.Completed, true,
                        stamp.SignedOn, stamp.SignedOn, documentUrl, null, stamp.FullName, stamp.FatherName, stamp.Address))
                {
                    await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception,
                        $"Portal agreement {stamp.Reference} stored at {documentUrl} but not saved for InterviewerProfileId {interviewerProfileId}.");
                    return (null, "Your signature could not be saved. Please try again.");
                }
                return (await _agreementRepository.GetInterviewerAgreement(interviewerProfileId), null);
            }
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return (null, "The agreement could not be signed right now. Please try again later.");
            }
        }

        private const string PortalReferencePrefix = "PORTAL-";

        private static bool IsPortalReference(string requestId) =>
            requestId != null && requestId.StartsWith(PortalReferencePrefix, StringComparison.OrdinalIgnoreCase);

        private async Task<string> UploadSignedPdf(byte[] pdf, int interviewerProfileId, string reference)
        {
            var fileName = $"InterviewerAgreement_{interviewerProfileId}_{reference}.pdf";
            var blob = _azureStorageHelper.GetCloudBlockBlob(_options.SignedDocumentContainer, fileName);
            blob.Properties.ContentType = "application/pdf";
            await blob.UploadFromByteArrayAsync(pdf, 0, pdf.Length);
            return blob.Uri.ToString();
        }

        private async Task<string> StoreSignedDocument(InterviewerAgreementVM agreement)
        {
            try
            {
                var pdf = await _zohoSignHelper.DownloadSignedDocumentAsync(agreement.AgreementZohoRequestId);
                return await UploadSignedPdf(pdf, agreement.InterviewerProfileId, agreement.AgreementZohoRequestId);
            }
            catch (Exception e)
            {
                // Status is still saved; the document is picked up again on the next sync / download.
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        // Values the interviewer typed into the agreement in Zoho Sign.
        private async Task<(string SignerName, string FatherName, string Address)> ReadFilledDetails(string zohoRequestId)
        {
            try
            {
                var fields = await _zohoSignHelper.GetFieldValuesAsync(zohoRequestId);
                string Value(string label) => fields.TryGetValue(label ?? string.Empty, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
                return (Truncate(Value(_options.NameFieldLabel), 150),
                        Truncate(Value(_options.FatherNameFieldLabel), 150),
                        Truncate(Value(_options.AddressFieldLabel), 500));
            }
            catch (Exception e)
            {
                // Status is still saved; the details are read again on the next sync.
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return (null, null, null);
            }
        }

        private static string MapStatus(ZohoSignRequestDetails details, string currentStatus)
        {
            switch ((details.RequestStatus ?? string.Empty).ToLowerInvariant())
            {
                case "completed":
                    return InterviewerAgreementStatus.Completed;
                case "declined":
                    return InterviewerAgreementStatus.Declined;
                case "expired":
                    return InterviewerAgreementStatus.Expired;
                case "recalled":
                    return InterviewerAgreementStatus.Recalled;
                case "inprogress":
                    var actionStatus = (details.ActionStatus ?? string.Empty).ToUpperInvariant();
                    if (actionStatus == "SIGNED")
                        return InterviewerAgreementStatus.InterviewerSigned;
                    if (actionStatus == "VIEWED")
                        return InterviewerAgreementStatus.Viewed;
                    return currentStatus ?? InterviewerAgreementStatus.Sent;
                default:
                    return currentStatus ?? InterviewerAgreementStatus.Sent;
            }
        }

        private static string Truncate(string value, int maxLength) =>
            value != null && value.Length > maxLength ? value.Substring(0, maxLength) : value;
    }
}
