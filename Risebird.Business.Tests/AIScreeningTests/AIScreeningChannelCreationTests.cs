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
            public int? TemplateAssessmentGroupId { get; private set; }
            public string ProvisionedJdTitleTwo { get; private set; }
            public string ProvisionedPrimaryPromptId { get; private set; }
            public string ProvisionedEvaluatorPromptId { get; private set; }

            /// <summary>Overrides the in-memory configuration this fixture builds.</summary>
            public Dictionary<string, string> Settings { get; } = new()
            {
                ["AIScreening:PrimaryPromptId"] = "pmpt_screening_primary",
                ["AIScreening:EvaluatorPromptId"] = "pmpt_screening_evaluator"
            };

            /// <summary>
            /// What the nearest-match lookup returns. Set before CreateSut — the mock defaults are
            /// registered there, so a Setup afterwards is easy to get subtly wrong.
            /// </summary>
            public int MatchedAssessmentGroupId { get; set; }

            /// <summary>What the catch-all-by-name lookup returns. Set before CreateSut.</summary>
            public int CatchAllAssessmentGroupIdByName { get; set; }

            public AIScreeningIntakeService CreateSut()
            {
                var configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(Settings)
                    .Build();

                Repository.Setup(x => x.ResolveAssessmentGroupIdByJobTitle(It.IsAny<string>(), It.IsAny<double>()))
                    .ReturnsAsync(() => MatchedAssessmentGroupId);
                Repository.Setup(x => x.GetAssessmentGroupIdByName(It.IsAny<string>()))
                    .ReturnsAsync(() => CatchAllAssessmentGroupIdByName);
                Repository.Setup(x => x.IsTemplateNameTaken(It.IsAny<string>()))
                    .ReturnsAsync(false);
                Repository.Setup(x => x.CreateAIAudioTemplate(
                        It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IList<TransSkill>>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .Callback<string, int, IList<TransSkill>, string, string, bool>(
                        (_, agId, _, _, _, _) => TemplateAssessmentGroupId = agId)
                    .ReturnsAsync(555);

                Repository.Setup(x => x.SaveAIScreeningProvisioning(
                        It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<TransSkill>>(), It.IsAny<string>()))
                    .Callback<int, int, string, string, string, IList<TransSkill>, string>(
                        (irfId, _, primaryPromptId, evaluatorPromptId, _, rubric, jdTitleTwo) =>
                        {
                            ProvisionedIrfId = irfId;
                            ProvisionedRubric = rubric;
                            ProvisionedJdTitleTwo = jdTitleTwo;
                            ProvisionedPrimaryPromptId = primaryPromptId;
                            ProvisionedEvaluatorPromptId = evaluatorPromptId;
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
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IList<TransSkill>>(), It.IsAny<string>()), Times.Never);
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

        #region The prompt ids come off the form

        private static AIScreeningChannelRequest RequestWithPrompts(
            string draftId, string primary, string evaluator)
        {
            var request = Request(draftId);
            request.PrimaryPromptId = primary;
            request.EvaluatorPromptId = evaluator;
            return request;
        }

        [Fact]
        public async Task The_prompt_ids_entered_on_the_form_are_stamped_on_the_channel()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            var result = await sut.CreateChannelAsync(
                RequestWithPrompts(draft.DraftId, "pmpt_form_primary", "pmpt_form_evaluator"),
                "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.True(result.Success, result.Message);
            Assert.Equal("pmpt_form_primary", fixture.ProvisionedPrimaryPromptId);
            Assert.Equal("pmpt_form_evaluator", fixture.ProvisionedEvaluatorPromptId);
        }

        [Fact]
        public async Task The_form_wins_over_the_configured_defaults()
        {
            // The config values only pre-fill the form; whatever was submitted is what the channel carries.
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            await sut.CreateChannelAsync(
                RequestWithPrompts(draft.DraftId, "pmpt_typed_by_hand", "pmpt_also_typed"),
                "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.Equal("pmpt_typed_by_hand", fixture.ProvisionedPrimaryPromptId);
            Assert.NotEqual("pmpt_screening_primary", fixture.ProvisionedPrimaryPromptId);
        }

        [Fact]
        public async Task The_configured_default_is_used_when_the_form_leaves_it_blank()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            await sut.CreateChannelAsync(
                RequestWithPrompts(draft.DraftId, null, null),
                "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.Equal("pmpt_screening_primary", fixture.ProvisionedPrimaryPromptId);
            Assert.Equal("pmpt_screening_evaluator", fixture.ProvisionedEvaluatorPromptId);
        }

        [Fact]
        public async Task Whitespace_is_trimmed_off_the_prompt_ids()
        {
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            await sut.CreateChannelAsync(
                RequestWithPrompts(draft.DraftId, "  pmpt_padded  ", "  pmpt_padded_eval  "),
                "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.Equal("pmpt_padded", fixture.ProvisionedPrimaryPromptId);
            Assert.Equal("pmpt_padded_eval", fixture.ProvisionedEvaluatorPromptId);
        }

        [Fact]
        public async Task An_evaluator_left_blank_stays_blank_so_the_primary_writes_the_report()
        {
            var fixture = new Fixture();
            fixture.Settings["AIScreening:EvaluatorPromptId"] = "";

            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            await sut.CreateChannelAsync(
                RequestWithPrompts(draft.DraftId, "pmpt_form_primary", null),
                "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.Equal("pmpt_form_primary", fixture.ProvisionedPrimaryPromptId);
            Assert.True(string.IsNullOrWhiteSpace(fixture.ProvisionedEvaluatorPromptId));
        }

        [Fact]
        public async Task Creation_is_refused_when_no_primary_prompt_id_can_be_found_anywhere()
        {
            // Mandatory server-side for an AI interview channel: a channel without one cannot be
            // saved from Edit IRF afterwards either.
            var fixture = new Fixture();
            fixture.Settings["AIScreening:PrimaryPromptId"] = "";

            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            var result = await sut.CreateChannelAsync(
                RequestWithPrompts(draft.DraftId, null, null),
                "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.False(result.Success);
            Assert.Contains("Primary Prompt Id", result.Message);
            Assert.Null(fixture.ProvisionedIrfId);
        }

        #endregion

        #region Assessment Group is never a blocker

        [Fact]
        public async Task The_catch_all_is_used_even_when_the_job_title_would_match_a_real_group()
        {
            // Nearest-match resolution is deliberately switched off: which Assessment Group an AI
            // channel belongs to does not affect how it runs, so every screening channel takes the
            // catch-all rather than risking a wrong group on a best guess. If the nearest-match
            // lookup is ever turned back on in ResolveAssessmentGroupAsync, this test should fail.
            var fixture = new Fixture
            {
                MatchedAssessmentGroupId = 42,
                CatchAllAssessmentGroupIdByName = 910
            };
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.Equal(910, fixture.TemplateAssessmentGroupId);
            fixture.Repository.Verify(
                x => x.ResolveAssessmentGroupIdByJobTitle(It.IsAny<string>(), It.IsAny<double>()), Times.Never);
        }

        [Fact]
        public async Task An_explicitly_configured_catch_all_wins_over_the_seeded_one()
        {
            var fixture = new Fixture { CatchAllAssessmentGroupIdByName = 88 };
            fixture.Settings["AIScreening:CatchAllAssessmentGroupId"] = "77";

            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.Equal(77, fixture.TemplateAssessmentGroupId);
        }

        [Fact]
        public async Task The_seeded_AIInterview_catch_all_is_resolved_by_name_when_nothing_matches()
        {
            var fixture = new Fixture { CatchAllAssessmentGroupIdByName = 910 };
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.Equal(910, fixture.TemplateAssessmentGroupId);
        }

        [Fact]
        public async Task The_catch_all_name_can_be_overridden_in_configuration()
        {
            var fixture = new Fixture { CatchAllAssessmentGroupIdByName = 611 };
            fixture.Settings["AIScreening:CatchAllAssessmentGroupName"] = "Generic AI";

            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.Equal(611, fixture.TemplateAssessmentGroupId);

            // The configured name is what gets looked up, not the built-in default.
            fixture.Repository.Verify(x => x.GetAssessmentGroupIdByName("Generic AI"), Times.Once);
        }

        [Fact]
        public async Task The_channel_is_still_created_when_no_assessment_group_resolves_at_all()
        {
            // Which group an AI channel belongs to does not affect how it runs, so an unresolved
            // Assessment Group must never cost the recruiter the channel.
            var fixture = new Fixture();
            var sut = fixture.CreateSut();
            var draft = await ReadyDraft(sut);

            var result = await sut.CreateChannelAsync(
                Request(draft.DraftId), "recruiter@risebird.io", "HR", _ => Task.FromResult("https://link"));

            Assert.True(result.Success, result.Message);
            Assert.Equal(RealIrfId, fixture.ProvisionedIrfId);
            Assert.Equal(0, fixture.TemplateAssessmentGroupId);
        }

        [Fact]
        public async Task An_unresolved_assessment_group_is_left_unset_on_the_channel_rather_than_zero()
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
            Assert.True(string.IsNullOrEmpty(submitted.AssessmentGroupId));
        }

        #endregion

        [Fact]
        public async Task Section_two_gets_a_heading_and_the_job_title_is_left_alone()
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

            // ClientReportV3 heads section two from JDTitleTwo, which nothing else on this route sets.
            Assert.Equal("Motivation to Join Fitment", fixture.ProvisionedJdTitleTwo);

            // JDTiltle stays the role, NOT a section label: the adaptive interviewer is told to
            // interview the candidate for it, and it is the channel's title everywhere else.
            Assert.NotNull(submitted);
            Assert.Equal("Senior Backend Engineer - Bangalore", submitted.JDTiltle);
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
