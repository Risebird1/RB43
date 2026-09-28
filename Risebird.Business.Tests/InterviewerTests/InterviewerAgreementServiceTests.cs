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
        private readonly Mock<IInterviewerAgreementRepository> _repository = new();
        private readonly Mock<IAzureStorageHelper> _storage = new();
        private readonly Mock<IApplicationErrorLogger> _logger = new();

        private InterviewerAgreementService CreateSut() =>
            new(_repository.Object, _storage.Object, _logger.Object, Options.Create(new ServiceAgreementOptions()));

        private static InterviewerAgreementSignRequest Request() => new()
        {
            SignerName = "Asha Rao",
            FatherName = "Gajendra",
            Address = "12 Shanti Nagar, Agra, Uttar Pradesh - 283122",
            SignatureImage = new byte[] { 0x89, 0x50, 0x4E, 0x47, 1 }
        };

        [Fact]
        public async Task SignAgreement_does_nothing_when_already_signed()
        {
            _repository.Setup(r => r.GetInterviewerAgreement(7)).ReturnsAsync(new InterviewerAgreementVM { InterviewerProfileId = 7, IsAgreementSigned = true });

            var error = await CreateSut().SignAgreement(7, Request());

            Assert.Contains("already signed", error);
            _storage.Verify(s => s.GetCloudBlockBlob(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _repository.Verify(r => r.SaveInterviewerAgreement(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SignAgreement_is_not_saved_when_the_signature_cannot_be_uploaded()
        {
            _repository.Setup(r => r.GetInterviewerAgreement(7)).ReturnsAsync(new InterviewerAgreementVM { InterviewerProfileId = 7 });
            // GetCloudBlockBlob is not set up, so the upload fails.

            var error = await CreateSut().SignAgreement(7, Request());

            Assert.NotNull(error);
            _repository.Verify(r => r.SaveInterviewerAgreement(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SignAgreement_reports_a_missing_profile()
        {
            _repository.Setup(r => r.GetInterviewerAgreement(7)).ReturnsAsync((InterviewerAgreementVM)null);

            var error = await CreateSut().SignAgreement(7, Request());

            Assert.NotNull(error);
        }
    }
}
