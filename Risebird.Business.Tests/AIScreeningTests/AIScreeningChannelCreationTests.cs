using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Risebird.Business;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data.OpenAIModels;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business.Tests.AIScreeningTests
{
    /// <summary>
    /// usp_CreateSkillChannelFromTemplateV1 returns its @flatRate output, not the identity of the
    /// row it inserted. Reading that as the channel id stamps the rubric, prompt ids and ideal
    /// candidate profile onto whatever unrelated channel happens to have id = the flat rate, with
    /// no error anywhere. These pin the id down to the real one.
    /// </summary>
    public class AIScreeningChannelCreationTests
    {
        private const int FlatRateReturnedByProc = 7;
        private const int RealIrfId = 4213;

        private sealed class Fixture
        {
            public Mock<IAIScreeningRepository> Repository { get; } = new(MockBehavior.Loose);
            public Mock<IIRFService> IrfService { get; } = new(MockBehavior.Loose);
            public Mock<IAssistantServiceHelper> Assistant { get; } = new(MockBehavior.Loose);
            public Mock<IApplicationErrorLogger> Logger { get; } = new(MockBehavior.Loose);

            public int? ProvisionedIrfId { get; private set; }
            public IList<TransSkill> ProvisionedRubric { get; private set; }

            public AIScreeningIntakeService CreateSut()
            {
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["AIScreening:CatchAllAssessmentGroupId"] = "99",
                        ["AIScreening:PrimaryPromptId"] = "pmpt_screening_primary",
                        ["AIScreening:EvaluatorPromptId"] = "pmpt_screening_evaluator"
                    })
                    .Build();

                Repository.Setup(x => x.ResolveAssessmentGroupIdByJobTitle(It.IsAny<string>(), It.IsAny<double>()))
                    .ReturnsAsync(0);
                Repository.Setup(x => x.IsTemplateNameTaken(It.IsAny<string>()))
                    .ReturnsAsync(false);
                Repository.Setup(x => x.CreateAIAudioTemplate(
                        It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IList<TransSkill>>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .ReturnsAsync(555);

                Repository.Setup(x => x.SaveAIScreeningProvisioning(
                        It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<TransSkill>>()))
                    .Callback<int, int, string, string, string, IList<TransSkill>>(
                        (irfId, _, _, _, _, rubric) =>
                        {
                            ProvisionedIrfId = irfId;
                            ProvisionedRubric = rubric;
                        })
                    .ReturnsAsync(true);

                IrfService.Setup(x => x.CheckUniqueness(It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns(true);

                // What the proc actually hands back: (success, flat rate).
                IrfService.Setup(x => x.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(
                        It.IsAny<IRFVm>(), It.IsAny<string>(), It.IsAny<string>()))
                    .ReturnsAsync(new Tuple<bool, int>(true, FlatRateReturnedByProc));

                IrfService.Setup(x => x.GetSkillChannelDetailsByName(It.IsAny<string>()))
                    .ReturnsAsync(new IRF { id = RealIrfId });

                Assistant.Setup(x => x.GenerateTechRubricRevision(It.IsAny<string>()))
                    .ReturnsAsync(() => Response(PlanJson));

                return new AIScreeningIntakeService(
                    Repository.Object,
                    IrfService.Object,
                    Assistant.Object,
                    Mock.Of<IOpenAISpeechToTextService>(),
                    Mock.Of<IAzureStorageHelper>(),
                    Mock.Of<IPdfReaderHelper>(),
                    new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
                    configuration,
                    Logger.Object);
            }
        }

        private const string PlanJson = @"{
          ""roleLabel"": ""Senior Backend Engineer - Bangalore"",
          ""durationMinutes"": 20,
          ""sections"": [
            { ""section"": ""RoleFitment"", ""parameters"": [
                { ""Skill"": ""Core Tech Stack"", ""ProficiencyLevel"": ""Expert"", ""Weight"": 100 } ] },
            { ""section"": ""MotivationToJoinFitment"", ""parameters"": [
                { ""Skill"": ""Compensation Feasibility"", ""ProficiencyLevel"": ""Competent"", ""Weight"": 100 } ] }
          ] }";

        private static RunResult Response(string text) => new RunResult
        {
            Data = new List<Datum>
            {
                new Datum { Content = new List<Content> { new Content { Text = new Text { Value = text } } } }
            }
        };

        /// <summary>Walks a draft to the point where the channel can be created.</summary>
        private static async Task<AIScreeningDraft> ReadyDraft(AIScreeningIntakeService sut)
        {
            var draft = await sut.CreateDraftAsync("recruiter@risebird.io");
            await sut.SetInstructionAsync(draft.DraftId, "Budget is sixteen LPA, Bangalore hybrid. Twenty minutes.");
            await sut.ConfirmIdealCandidateAsync(draft.DraftId, "Java, Spring Boot | 5-8 yrs | Bangalore");
            await sut.GeneratePlanAsync(draft.DraftId);
            return draft;
        }

        private static AIScreeningChannelRequest Request(string draftId) => new AIScreeningChannelRequest
        {
            DraftId = draftId,
            ClientId = 12,
            BillingOwner = 34,
            ClientUserIds = new List<int> { 56 }
        };

        [Fact]
        public async Task Provisioning_targets_the_real_IRF_id_not_the_flat_rate()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            var result = await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.True(result.Success, result.Message);
            Assert.Equal(RealIrfId, fixture.ProvisionedIrfId);
            Assert.NotEqual(FlatRateReturnedByProc, fixture.ProvisionedIrfId);
            Assert.Equal(RealIrfId, result.IrfIntId);
        }

        [Fact]
        public async Task The_rubric_rows_are_stamped_with_the_real_IRF_id()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.NotNull(fixture.ProvisionedRubric);
            Assert.NotEmpty(fixture.ProvisionedRubric);
            Assert.All(fixture.ProvisionedRubric, s => Assert.Equal(RealIrfId, s.IRFId));
        }

        [Fact]
        public async Task The_channel_id_is_looked_up_by_the_code_that_was_just_created()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            var result = await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            fixture.IrfService.Verify(x => x.GetSkillChannelDetailsByName(result.IrfId), Times.Once);
        }

        [Fact]
        public async Task Nothing_is_provisioned_when_the_channel_id_cannot_be_read_back()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            fixture.IrfService.Setup(x => x.GetSkillChannelDetailsByName(It.IsAny<string>()))
                .ReturnsAsync((IRF)null);

            var draft = await ReadyDraft(sut);

            var result = await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            // Better to report an incomplete channel than to write the rubric onto an unknown row.
            Assert.False(result.Success);
            Assert.Null(fixture.ProvisionedIrfId);
            fixture.Repository.Verify(x => x.SaveAIScreeningProvisioning(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<TransSkill>>()), Times.Never);
        }

        [Fact]
        public async Task A_retry_after_the_id_lookup_failed_does_not_create_a_second_channel()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            fixture.IrfService.Setup(x => x.GetSkillChannelDetailsByName(It.IsAny<string>()))
                .ReturnsAsync((IRF)null);

            var draft = await ReadyDraft(sut);

            var first = await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));
            Assert.False(first.Success);

            var second = await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.True(second.AlreadyCreated);
            fixture.IrfService.Verify(x => x.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(
                It.IsAny<IRFVm>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task A_double_submit_returns_the_channel_already_created()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            var first = await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));
            var second = await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.True(first.Success);
            Assert.True(second.Success);
            Assert.True(second.AlreadyCreated);
            Assert.Equal(first.IrfId, second.IrfId);
            Assert.Equal(RealIrfId, second.IrfIntId);

            fixture.IrfService.Verify(x => x.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(
                It.IsAny<IRFVm>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task The_channel_is_created_as_AI_Audio_Adaptive()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            IRFVm submitted = null;

            fixture.IrfService.Setup(x => x.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(
                    It.IsAny<IRFVm>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<IRFVm, string, string>((vm, _, _) => submitted = vm)
                .ReturnsAsync(new Tuple<bool, int>(true, FlatRateReturnedByProc));

            var draft = await ReadyDraft(sut);
            await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.NotNull(submitted);
            Assert.True(submitted.IsAIInterview);
            Assert.Equal((int)AIInterviewTypeEnum.Adaptive, submitted.AIInterviewType);
        }
    }
}
