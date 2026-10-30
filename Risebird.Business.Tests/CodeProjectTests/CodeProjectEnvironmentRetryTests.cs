using Moq;
using Risebird.Business;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System.Net;
using Xunit;

namespace Risebird.Business.Tests.CodeProjectTests
{
    /// <summary>
    /// A failure before any machine existed (provisioner unreachable during a restart, configuration
    /// error) can be tried again; once a machine exists or was handed over, a failure is final.
    /// </summary>
    public class CodeProjectEnvironmentRetryTests
    {
        private const string Session = "s1";
        private const int Question = 7;

        private static CodeProjectEnvironment Env(string handle = null, DateTime? handedOver = null) => new CodeProjectEnvironment
        {
            State = CodeProjectEnvironmentState.Failed,
            EnvironmentHandle = handle,
            HandedOverAtUtc = handedOver
        };

        [Fact]
        public void Only_failures_before_any_machine_existed_can_be_retried()
        {
            Assert.True(CodeProjectQuestionService.CanRetryEnvironment(Env()));
            Assert.False(CodeProjectQuestionService.CanRetryEnvironment(Env(handle: "abc")));
            Assert.False(CodeProjectQuestionService.CanRetryEnvironment(Env(handedOver: DateTime.UtcNow)));
            Assert.False(CodeProjectQuestionService.CanRetryEnvironment(new CodeProjectEnvironment { State = CodeProjectEnvironmentState.Provisioning }));
            Assert.False(CodeProjectQuestionService.CanRetryEnvironment(null));
        }

        private static (CodeProjectQuestionService Service, Mock<ICodeProjectEnvironmentProvisioner> Provisioner) Build(CodeProjectEnvironment env)
        {
            var repo = new Mock<ICodeProjectQuestionRepository>();
            repo.Setup(x => x.GetEnvironment(Session, Question)).ReturnsAsync(env);
            repo.Setup(x => x.GetCodeProjectDetails(Question)).ReturnsAsync(new CodeProjectDetails
            {
                ValidationStatus = CodeProjectValidationStatus.Passed, StackImageCode = "rb-python-win", ReadOnlyPathsJson = "[]"
            });
            repo.Setup(x => x.GetActiveStackImages()).ReturnsAsync(new List<StackImage>
            {
                new StackImage { Code = "rb-python-win", GalleryImageId = "/gallery/rb-python-win", OsType = "Windows", IsActive = true }
            });
            repo.Setup(x => x.UpdateEnvironment(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>())).ReturnsAsync(true);
            var blobs = new Mock<ICodeProjectBlobStore>();
            blobs.Setup(x => x.CreateReadUrl(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>())).Returns("https://blob/zip?sas");
            var provisioner = new Mock<ICodeProjectEnvironmentProvisioner>();
            provisioner.SetupGet(x => x.IsConfigured).Returns(true);
            provisioner.Setup(x => x.ProvisionAsync(It.IsAny<CodeProjectProvisionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CodeProjectProvisionResult { Accepted = true, Handle = "h1" });
            var service = new CodeProjectQuestionService(repo.Object, blobs.Object, new Mock<ICodeProjectRunner>().Object,
                provisioner.Object, new Mock<IApplicationErrorLogger>().Object);
            return (service, provisioner);
        }

        [Fact]
        public async Task Retry_provisions_again_when_no_machine_was_created()
        {
            var (service, provisioner) = Build(Env());
            await service.RetryEnvironmentAsync(Session, Question);
            provisioner.Verify(x => x.ProvisionAsync(It.Is<CodeProjectProvisionRequest>(r => r.SessionId == Session && r.QuestionId == Question),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Retry_does_nothing_once_a_machine_existed()
        {
            var (service, provisioner) = Build(Env(handle: "abc"));
            await service.RetryEnvironmentAsync(Session, Question);
            provisioner.Verify(x => x.ProvisionAsync(It.IsAny<CodeProjectProvisionRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private sealed class FlakyHandler : HttpMessageHandler
        {
            public int Calls;
            private readonly int _failures;
            public FlakyHandler(int failures) { _failures = failures; }
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                Calls++;
                return Task.FromResult(Calls <= _failures
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"accepted\":true,\"handle\":\"h9\"}", System.Text.Encoding.UTF8, "application/json") });
            }
        }

        private static HttpCodeProjectEnvironmentProvisioner Client(FlakyHandler handler)
        {
            var settings = new Mock<ICodeProjectSettingsProvider>();
            settings.Setup(x => x.FunctionsBaseUrl()).Returns("https://functions.example/api");
            return new HttpCodeProjectEnvironmentProvisioner(new HttpClient(handler), settings.Object);
        }

        [Fact]
        public async Task The_provision_call_survives_a_brief_restart_of_the_functions_app()
        {
            var handler = new FlakyHandler(failures: 2);
            var result = await Client(handler).ProvisionAsync(new CodeProjectProvisionRequest { SessionId = Session, QuestionId = Question });
            Assert.True(result.Accepted);
            Assert.Equal("h9", result.Handle);
            Assert.Equal(3, handler.Calls);
        }

        [Fact]
        public async Task The_provision_call_gives_up_after_three_attempts()
        {
            var handler = new FlakyHandler(failures: 10);
            var result = await Client(handler).ProvisionAsync(new CodeProjectProvisionRequest { SessionId = Session, QuestionId = Question });
            Assert.False(result.Accepted);
            Assert.Equal("The provisioner did not respond.", result.Error);
            Assert.Equal(3, handler.Calls);
        }
    }
}
