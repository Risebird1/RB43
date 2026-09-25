using Microsoft.Extensions.Options;
using Moq;
using Risebird.Business;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.InterviewerTests
{
    public class InterviewerAgreementServiceTests
    {
        private const string RequestId = "req-1";
        private const string ActionId = "act-1";

        private readonly Mock<IInterviewerAgreementRepository> _repository = new();
        private readonly Mock<IZohoSignHelper> _zohoSign = new();
        private readonly Mock<IAzureStorageHelper> _storage = new();
        private readonly Mock<IInterviewerAgreementDocumentProvider> _documents = new();
        private readonly Mock<IApplicationErrorLogger> _logger = new();

        public InterviewerAgreementServiceTests() => _zohoSign.Setup(z => z.IsConfigured).Returns(true);

        [Fact]
        public async Task Initiate_reports_missing_zoho_configuration_without_calling_zoho()
        {
            _zohoSign.Setup(z => z.IsConfigured).Returns(false);
            _repository.Setup(r => r.GetInterviewerAgreement(7)).ReturnsAsync(Agreement(null));

            var (agreement, message) = await CreateSut().InitiateAgreement(7, "https://x/return");

            Assert.Null(agreement);
            Assert.Contains("not set up", message);
            _zohoSign.Verify(z => z.SendInterviewerAgreementAsync(It.IsAny<ZohoSignAgreementRequest>(), It.IsAny<AgreementDocument>()), Times.Never);
        }

        private static PortalSignatureStamp Stamp() => new()
        {
            FullName = "Asha Rao",
            FatherName = "Gajendra",
            Address = "Abhuapura, Agra 283122",
            SignatureImage = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1 },
            IpAddress = "10.0.0.1"
        };

        [Fact]
        public async Task SignInPortal_does_nothing_when_already_signed()
        {
            _repository.Setup(r => r.GetInterviewerAgreement(7)).ReturnsAsync(Agreement(InterviewerAgreementStatus.Completed, isSigned: true));

            var (agreement, message) = await CreateSut().SignInPortal(7, Stamp());

            Assert.NotNull(agreement);
            Assert.Contains("already signed", message);
            _documents.Verify(d => d.StampPortalSignature(It.IsAny<AgreementDocument>(), It.IsAny<PortalSignatureStamp>()), Times.Never);
        }

        [Fact]
        public async Task SignInPortal_is_not_marked_signed_when_the_signed_pdf_cannot_be_stored()
        {
            _repository.Setup(r => r.GetInterviewerAgreement(7)).ReturnsAsync(Agreement(null));
            _documents.Setup(d => d.GetDocumentAsync()).ReturnsAsync(new AgreementDocument());
            _documents.Setup(d => d.StampPortalSignature(It.IsAny<AgreementDocument>(), It.IsAny<PortalSignatureStamp>())).Returns(new byte[] { 1 });
            // GetCloudBlockBlob is not set up, so storing the PDF fails.

            var stamp = Stamp();
            var (agreement, message) = await CreateSut().SignInPortal(7, stamp);

            Assert.Null(agreement);
            Assert.NotNull(message);
            Assert.StartsWith("PORTAL-", stamp.Reference);
            Assert.Equal("asha@test.com", stamp.SignerEmail);
            _repository.Verify(r => r.SaveInterviewerAgreementRequest(It.IsAny<int>(), It.IsAny<ZohoSignRequestResult>(), It.IsAny<string>()), Times.Never);
            _repository.Verify(r => r.UpdateInterviewerAgreementStatus(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        private InterviewerAgreementService CreateSut() =>
            new(_repository.Object, _zohoSign.Object, _documents.Object, _storage.Object, _logger.Object, Options.Create(new ZohoSignOptions()));

        private static InterviewerAgreementVM Agreement(string status, bool isSigned = false) => new()
        {
            InterviewerProfileId = 7,
            FirstName = "Asha",
            LastName = "Rao",
            EmailId = "asha@test.com",
            AgreementStatus = status,
            IsAgreementSigned = isSigned,
            AgreementZohoRequestId = status == null ? null : RequestId,
            AgreementZohoActionId = status == null ? null : ActionId
        };

        private void ZohoReturns(string requestStatus, string actionStatus, string declineReason = null) =>
            _zohoSign.Setup(z => z.GetRequestDetailsAsync(RequestId, ActionId)).ReturnsAsync(new ZohoSignRequestDetails
            {
                RequestId = RequestId,
                RequestStatus = requestStatus,
                ActionStatus = actionStatus,
                DeclineReason = declineReason
            });

        [Fact]
        public async Task Sync_completed_request_marks_agreement_signed_even_if_pdf_storage_fails()
        {
            _repository.Setup(r => r.GetInterviewerAgreementByZohoRequestId(RequestId)).ReturnsAsync(Agreement(InterviewerAgreementStatus.Sent));
            ZohoReturns("completed", "SIGNED");
            _zohoSign.Setup(z => z.DownloadSignedDocumentAsync(RequestId)).ThrowsAsync(new InvalidOperationException("zoho down"));

            await CreateSut().SyncAgreementStatus(RequestId);

            _repository.Verify(r => r.UpdateInterviewerAgreementStatus(RequestId, InterviewerAgreementStatus.Completed, true,
                It.IsNotNull<DateTime?>(), It.IsNotNull<DateTime?>(), null, null,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Sync_after_interviewer_signs_saves_details_filled_in_on_the_agreement()
        {
            _repository.Setup(r => r.GetInterviewerAgreementByZohoRequestId(RequestId)).ReturnsAsync(Agreement(InterviewerAgreementStatus.Viewed));
            ZohoReturns("inprogress", "SIGNED");
            _zohoSign.Setup(z => z.GetFieldValuesAsync(RequestId)).ReturnsAsync(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["interviewer name"] = " Asha Rao ",
                ["Father Name"] = "Gajendra",
                ["Address"] = "Abhuapura, 283122"
            });

            await CreateSut().SyncAgreementStatus(RequestId);

            _repository.Verify(r => r.UpdateInterviewerAgreementStatus(RequestId, InterviewerAgreementStatus.InterviewerSigned, false,
                It.IsAny<DateTime?>(), null, null, null,
                "Asha Rao", "Gajendra", "Abhuapura, 283122"), Times.Once);
        }

        [Fact]
        public async Task Sync_interviewer_signed_but_request_in_progress_is_not_marked_signed()
        {
            _repository.Setup(r => r.GetInterviewerAgreementByZohoRequestId(RequestId)).ReturnsAsync(Agreement(InterviewerAgreementStatus.Viewed));
            ZohoReturns("inprogress", "SIGNED");

            await CreateSut().SyncAgreementStatus(RequestId);

            _repository.Verify(r => r.UpdateInterviewerAgreementStatus(RequestId, InterviewerAgreementStatus.InterviewerSigned, false,
                It.IsNotNull<DateTime?>(), null, null, null,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _zohoSign.Verify(z => z.DownloadSignedDocumentAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Sync_declined_request_stores_reason()
        {
            _repository.Setup(r => r.GetInterviewerAgreementByZohoRequestId(RequestId)).ReturnsAsync(Agreement(InterviewerAgreementStatus.Sent));
            ZohoReturns("declined", "DECLINED", "Not agreed");

            await CreateSut().SyncAgreementStatus(RequestId);

            _repository.Verify(r => r.UpdateInterviewerAgreementStatus(RequestId, InterviewerAgreementStatus.Declined, false,
                null, null, null, "Not agreed", null, null, null), Times.Once);
            _zohoSign.Verify(z => z.GetFieldValuesAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Sync_unknown_request_does_not_call_zoho()
        {
            _repository.Setup(r => r.GetInterviewerAgreementByZohoRequestId("other")).ReturnsAsync((InterviewerAgreementVM)null);

            var result = await CreateSut().SyncAgreementStatus("other");

            Assert.Null(result);
            _zohoSign.Verify(z => z.GetRequestDetailsAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(InterviewerAgreementStatus.Completed, true)]
        [InlineData(InterviewerAgreementStatus.InterviewerSigned, false)]
        [InlineData(InterviewerAgreementStatus.Sent, false)]
        public async Task Initiate_does_not_create_a_new_request_when_signed_or_open(string status, bool isSigned)
        {
            _repository.Setup(r => r.GetInterviewerAgreement(7)).ReturnsAsync(Agreement(status, isSigned));

            var (agreement, _) = await CreateSut().InitiateAgreement(7, "https://x/return");

            Assert.NotNull(agreement);
            _zohoSign.Verify(z => z.SendInterviewerAgreementAsync(It.IsAny<ZohoSignAgreementRequest>(), It.IsAny<AgreementDocument>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(InterviewerAgreementStatus.Declined)]
        [InlineData(InterviewerAgreementStatus.Expired)]
        public async Task Initiate_creates_and_saves_a_request_when_none_is_usable(string status)
        {
            _repository.Setup(r => r.GetInterviewerAgreement(7)).ReturnsAsync(Agreement(status));
            _zohoSign.Setup(z => z.SendInterviewerAgreementAsync(It.IsAny<ZohoSignAgreementRequest>(), It.IsAny<AgreementDocument>()))
                .ReturnsAsync(new ZohoSignRequestResult { RequestId = "req-2", ActionId = "act-2" });
            _repository.Setup(r => r.SaveInterviewerAgreementRequest(7, It.IsAny<ZohoSignRequestResult>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            var (agreement, message) = await CreateSut().InitiateAgreement(7, "https://x/return");

            Assert.Null(message);
            _zohoSign.Verify(z => z.SendInterviewerAgreementAsync(It.Is<ZohoSignAgreementRequest>(r =>
                r.RecipientName == "Asha Rao" && r.RecipientEmail == "asha@test.com"), It.IsAny<AgreementDocument>()), Times.Once);
            _repository.Verify(r => r.SaveInterviewerAgreementRequest(7, It.Is<ZohoSignRequestResult>(x => x.RequestId == "req-2"),
                "v2"), Times.Once);
        }
    }
}
