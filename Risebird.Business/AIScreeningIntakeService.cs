using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.Helpers;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Business
{
    /// <inheritdoc cref="IAIScreeningIntakeService"/>
    public class AIScreeningIntakeService : IAIScreeningIntakeService
    {
        private const string DraftCacheKeyPrefix = "ai-screening-draft:";
        private const string JdContainerName = "resumes";
        private const string AudioContainerName = "audios";
        private const int DefaultDraftLifetimeHours = 24;

        /// <summary>Seeded by Risebird.DB/Tables/AssessmentGroup.sql.</summary>
        private const string DefaultCatchAllAssessmentGroupName = "AIInterview";

        private readonly IAIScreeningRepository _aiScreeningRepository;
        private readonly IIRFService _irfService;
        private readonly IAssistantServiceHelper _assistantServiceHelper;
        private readonly IOpenAISpeechToTextService _speechToTextService;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IPdfReaderHelper _pdfReaderHelper;
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AIScreeningIntakeService(
            IAIScreeningRepository aiScreeningRepository,
            IIRFService irfService,
            IAssistantServiceHelper assistantServiceHelper,
            IOpenAISpeechToTextService speechToTextService,
            IAzureStorageHelper azureStorageHelper,
            IPdfReaderHelper pdfReaderHelper,
            IDistributedCache cache,
            IConfiguration configuration,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _aiScreeningRepository = aiScreeningRepository;
            _irfService = irfService;
            _assistantServiceHelper = assistantServiceHelper;
            _speechToTextService = speechToTextService;
            _azureStorageHelper = azureStorageHelper;
            _pdfReaderHelper = pdfReaderHelper;
            _cache = cache;
            _configuration = configuration;
            _applicationErrorLogger = applicationErrorLogger;
        }

        #region Draft lifecycle

        public async Task<AIScreeningDraft> CreateDraftAsync(string user)
        {
            var draft = new AIScreeningDraft
            {
                DraftId = "d_" + Guid.NewGuid().ToString("N").Substring(0, 5),
                CreatedBy = user,
                CreatedOn = DateTime.Now,
                ModifiedOn = DateTime.Now
            };

            await SaveDraftAsync(draft);
            return draft;
        }

        public async Task<AIScreeningDraft> GetDraftAsync(string draftId)
        {
            if (string.IsNullOrWhiteSpace(draftId)) return null;

            try
            {
                var json = await _cache.GetStringAsync(DraftCacheKeyPrefix + draftId.Trim());
                return string.IsNullOrWhiteSpace(json)
                    ? null
                    : JsonConvert.DeserializeObject<AIScreeningDraft>(json);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        private async Task SaveDraftAsync(AIScreeningDraft draft)
        {
            if (draft == null || string.IsNullOrWhiteSpace(draft.DraftId)) return;

            draft.ModifiedOn = DateTime.Now;
            var lifetimeHours = ReadInt("AIScreening:DraftLifetimeHours", DefaultDraftLifetimeHours);

            await _cache.SetStringAsync(
                DraftCacheKeyPrefix + draft.DraftId,
                JsonConvert.SerializeObject(draft),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(lifetimeHours)
                });
        }

        #endregion

        #region Step 1 — job description and instruction

        public async Task<string> AttachJdAsync(string draftId, IFormFile jd)
        {
            var draft = await GetDraftAsync(draftId);
            if (draft == null || jd == null || jd.Length == 0) return null;

            try
            {
                // Same container the existing JD upload writes to. The blob name is keyed by draft
                // id so two recruiters uploading "JD.pdf" at once do not overwrite each other.
                var blobName = $"{draft.DraftId}_{Path.GetFileName(jd.FileName)}";
                await _azureStorageHelper.InsertBlob(blobName, JdContainerName, jd);

                var containerRef = _azureStorageHelper.GetCloudBlobContainer(JdContainerName);
                var blobUrl = containerRef.GetBlockBlobReference(blobName).Uri.ToString();

                draft.JdBlobUrl = blobUrl;
                draft.JdFileName = jd.FileName;
                draft.JdFileSizeBytes = jd.Length;
                draft.JdText = await ExtractJdTextAsync(jd, blobUrl);
                draft.JobTitle = ExtractJobTitle(draft.JdText, jd.FileName);

                await SaveDraftAsync(draft);
                return blobUrl;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<string> TranscribeInstructionAsync(
            string draftId, Stream audio, string fileName, bool multilingual)
        {
            var draft = await GetDraftAsync(draftId);
            if (draft == null || audio == null) return null;

            try
            {
                // Keep the recording, whatever the transcript turns out to be, so a failed
                // transcription does not cost the recruiter the take.
                draft.InstructionAudioUrl = await StoreInstructionAudioAsync(draft, audio, fileName);

                if (audio.CanSeek) audio.Position = 0;

                var transcript = await _speechToTextService.TranscribeAsync(
                    audio, string.IsNullOrWhiteSpace(fileName) ? "instruction.wav" : fileName, multilingual);

                if (!IsTranscriptUsable(transcript, multilingual))
                {
                    await SaveDraftAsync(draft);
                    return null;
                }

                draft.Instruction = transcript.Trim();
                await SaveDraftAsync(draft);
                return draft.Instruction;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> SetInstructionAsync(string draftId, string instruction)
        {
            var draft = await GetDraftAsync(draftId);
            if (draft == null || string.IsNullOrWhiteSpace(instruction)) return false;

            // Typed instructions write the same field a transcription would, without touching
            // the transcription service.
            draft.Instruction = instruction.Trim();
            await SaveDraftAsync(draft);
            return true;
        }

        private async Task<string> StoreInstructionAudioAsync(AIScreeningDraft draft, Stream audio, string fileName)
        {
            try
            {
                if (audio.CanSeek) audio.Position = 0;

                var buffer = new MemoryStream();
                await audio.CopyToAsync(buffer);
                buffer.Position = 0;

                var extension = Path.GetExtension(fileName);
                if (string.IsNullOrWhiteSpace(extension)) extension = ".wav";

                var blobName = $"ai_screening_instruction_{draft.DraftId}{extension}";
                var url = await _azureStorageHelper.InsertBlobAIInterviewerAudio(
                    blobName, AudioContainerName, buffer, "audio/wav");

                if (audio.CanSeek) audio.Position = 0;
                return url;
            }
            catch (Exception ex)
            {
                // The recording is a convenience; losing it must not fail the transcription.
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return draft.InstructionAudioUrl;
            }
        }

        /// <summary>
        /// Mirrors the check the DHI screen already applies, so a garbled take produces the same
        /// "could not produce a clear transcript" outcome here.
        /// </summary>
        private static bool IsTranscriptUsable(string text, bool multilingual)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (multilingual) return true;

            var cleaned = new string(text.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray());
            return (double)cleaned.Length / text.Length >= 0.5;
        }

        private async Task<string> ExtractJdTextAsync(IFormFile jd, string blobUrl)
        {
            var extension = Path.GetExtension(jd.FileName)?.ToLowerInvariant();

            if (extension == ".txt")
            {
                using var reader = new StreamReader(jd.OpenReadStream());
                return await reader.ReadToEndAsync();
            }

            try
            {
                // The same reader the adaptive interview prompt uses to turn a stored JD into text.
                return await _pdfReaderHelper.ReadPdfFromUrlAsync(blobUrl);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Best-effort job title: the first meaningful line of the JD, else the file name. Only used
        /// as a label and for the nearest-match Assessment Group lookup, so a miss is not fatal.
        /// </summary>
        private static string ExtractJobTitle(string jdText, string fileName)
        {
            if (!string.IsNullOrWhiteSpace(jdText))
            {
                var line = jdText
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .FirstOrDefault(l => l.Length > 3 && l.Length <= 120);

                if (!string.IsNullOrWhiteSpace(line)) return line;
            }

            var name = Path.GetFileNameWithoutExtension(fileName) ?? string.Empty;
            return name.Replace('_', ' ').Replace('-', ' ').Trim();
        }

        #endregion

        #region Step 2 — ideal candidate

        public async Task<AIScreeningIdealCandidate> ProposeIdealCandidateAsync(string draftId)
        {
            var draft = await GetDraftAsync(draftId);
            if (draft == null) return null;

            var prompt = AIScreeningIntakeHelper.BuildIdealCandidatePrompt(
                draft.JobTitle, draft.JdText, draft.Instruction);

            var raw = await RunAssistantAsync(prompt);
            var profile = AIScreeningIntakeHelper.ParseIdealCandidateProfile(raw);

            if (string.IsNullOrWhiteSpace(profile))
            {
                return new AIScreeningIdealCandidate
                {
                    DraftId = draft.DraftId,
                    Profile = draft.ConfirmedIdealCandidateProfile ?? draft.ProposedIdealCandidateProfile,
                    Message = "Could not propose a profile from this job description. Write one below and continue."
                };
            }

            draft.ProposedIdealCandidateProfile = profile;
            await SaveDraftAsync(draft);

            return new AIScreeningIdealCandidate
            {
                DraftId = draft.DraftId,
                Profile = profile,
                Message = "Proposed from the job description — edit anything that is wrong."
            };
        }

        public async Task<bool> ConfirmIdealCandidateAsync(string draftId, string idealCandidateProfile)
        {
            var draft = await GetDraftAsync(draftId);
            if (draft == null || string.IsNullOrWhiteSpace(idealCandidateProfile)) return false;

            draft.ConfirmedIdealCandidateProfile = idealCandidateProfile.Trim();
            await SaveDraftAsync(draft);
            return true;
        }

        #endregion

        #region Step 3 — the plan

        public async Task<AIScreeningPlan> GeneratePlanAsync(string draftId)
        {
            var draft = await GetDraftAsync(draftId);
            if (draft == null) return null;

            var duration = ParseDurationMinutes(draft.Instruction);
            var profile = draft.ConfirmedIdealCandidateProfile ?? draft.ProposedIdealCandidateProfile;

            var prompt = AIScreeningIntakeHelper.BuildPlanPrompt(
                draft.JobTitle, draft.JdText, draft.Instruction, profile, duration);

            var raw = await RunAssistantAsync(prompt);
            var plan = AIScreeningIntakeHelper.ParsePlan(raw, duration);

            if (plan == null || plan.ParameterCount == 0)
                return null;

            if (string.IsNullOrWhiteSpace(plan.RoleLabel))
                plan.RoleLabel = draft.JobTitle;

            AIScreeningIntakeHelper.NormalizeSectionWeights(plan);

            draft.Plan = plan;
            await SaveDraftAsync(draft);
            return plan;
        }

        public async Task<AIScreeningPlanRevision> RevisePlanByVoiceAsync(string draftId, string transcript)
        {
            var draft = await GetDraftAsync(draftId);
            if (draft?.Plan == null || string.IsNullOrWhiteSpace(transcript)) return null;

            var current = draft.Plan;
            var profile = draft.ConfirmedIdealCandidateProfile ?? draft.ProposedIdealCandidateProfile;

            var prompt = AIScreeningIntakeHelper.BuildPlanRevisionPrompt(current, transcript, profile);
            var raw = await RunAssistantAsync(prompt);
            var revised = AIScreeningIntakeHelper.ParsePlan(raw, current.DurationMinutes);

            // A revision that cannot be parsed leaves the plan exactly as it was.
            if (revised == null || revised.ParameterCount == 0)
            {
                return new AIScreeningPlanRevision
                {
                    Plan = current,
                    Changed = false,
                    Message = "The revision could not be applied. The plan is unchanged — try rephrasing the instruction."
                };
            }

            if (string.IsNullOrWhiteSpace(revised.RoleLabel)) revised.RoleLabel = current.RoleLabel;

            var weightsAdjusted = AIScreeningIntakeHelper.NormalizeSectionWeights(revised);
            var revision = AIScreeningIntakeHelper.BuildRevision(current, revised, transcript, weightsAdjusted);

            // An off-topic instruction is a valid response: keep the plan, surface the message.
            draft.Plan = revision.Changed ? revised : current;
            draft.Plan.Message = revision.Message;
            await SaveDraftAsync(draft);

            revision.Plan = draft.Plan;
            return revision;
        }

        /// <summary>Reads "twenty minutes" / "20 min" out of the instruction; falls back to the default.</summary>
        internal static int ParseDurationMinutes(string instruction)
        {
            if (string.IsNullOrWhiteSpace(instruction))
                return AIScreeningIntakeHelper.DefaultDurationMinutes;

            var text = instruction.ToLowerInvariant();

            var digits = Regex.Match(text, @"(\d{1,3})\s*(?:-|\s)?\s*(?:minute|minutes|min|mins)\b");
            if (digits.Success && int.TryParse(digits.Groups[1].Value, out var minutes) && minutes > 0)
                return minutes;

            var words = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["ten"] = 10,
                ["fifteen"] = 15,
                ["twenty"] = 20,
                ["twenty five"] = 25,
                ["thirty"] = 30,
                ["forty"] = 40,
                ["forty five"] = 45,
                ["sixty"] = 60
            };

            foreach (var pair in words.OrderByDescending(p => p.Key.Length))
            {
                if (Regex.IsMatch(text, $@"\b{Regex.Escape(pair.Key)}\s*(?:minute|minutes|min|mins)\b"))
                    return pair.Value;
            }

            return AIScreeningIntakeHelper.DefaultDurationMinutes;
        }

        private async Task<string> RunAssistantAsync(string prompt)
        {
            try
            {
                // GenerateTechRubricRevision is the free-prompt entry point on the rubric assistant;
                // GenerateTechRubric only accepts a bare job title.
                var result = await _assistantServiceHelper.GenerateTechRubricRevision(prompt);
                return result?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        #endregion

        #region Step 4 — create the channel

        public async Task<AIScreeningChannelResult> CreateChannelAsync(
            AIScreeningChannelRequest request,
            string loggedInUser,
            string profile,
            Func<string, Task<string>> schedulingLinkFactory)
        {
            if (request == null)
                return Failed("No request supplied.");

            var draft = await GetDraftAsync(request.DraftId);
            if (draft == null)
                return Failed("This draft has expired. Start a new screening.");

            // Idempotent on draft id: a double submit returns the channel already created.
            // Keyed on the channel code rather than the id, because the code is recorded the moment
            // the channel exists — a retry after provisioning failed half way must not mint a second
            // channel just because the id was never resolved.
            if (!string.IsNullOrWhiteSpace(draft.CreatedIrfId))
            {
                return new AIScreeningChannelResult
                {
                    Success = true,
                    AlreadyCreated = true,
                    IrfIntId = draft.CreatedIrfIntId,
                    IrfId = draft.CreatedIrfId,
                    TemplateId = draft.CreatedTemplateId,
                    Message = $"Screening channel '{draft.CreatedIrfId}' already created for this draft."
                };
            }

            if (draft.Plan == null || draft.Plan.ParameterCount == 0)
                return Failed("Draft the interview plan before creating the channel.");

            if (request.ClientId <= 0)
                return Failed("Select a client.");

            // The two prompts that run this channel, exactly as a skill channel already carries them:
            // the Primary Prompt Id conducts the adaptive interview and the Interview Evaluator
            // Prompt Id writes the report. Both are supplied on the intake form; the configured
            // defaults only pre-fill it, so whatever the recruiter submitted wins.
            //
            // IRF.OpenAIPromptId is mandatory for an AI interview channel and guarded server-side,
            // so the provisioning path always stamps it rather than leaving the channel to the
            // runtime config fallback.
            var primaryPromptId = Trimmed(request.PrimaryPromptId)
                                  ?? Trimmed(_configuration["AIScreening:PrimaryPromptId"]);
            var usingSharedPrompt = false;

            if (string.IsNullOrWhiteSpace(primaryPromptId))
            {
                // OpenAI:AIInterviewerPromptId is shared by every AI interview running today, so
                // editing its content in the OpenAI account changes behaviour for all of them.
                primaryPromptId = Trimmed(_configuration["OpenAI:AIInterviewerPromptId"]);
                usingSharedPrompt = !string.IsNullOrWhiteSpace(primaryPromptId);
            }

            if (string.IsNullOrWhiteSpace(primaryPromptId))
            {
                return Failed(
                    "Enter the Primary Prompt Id that runs the interview. It is mandatory for an AI " +
                    "interview channel and cannot be filled in later without it.");
            }

            // Optional. Left blank, the Primary Prompt Id generates the report too — which is how
            // every other AI channel already behaves.
            var evaluatorPromptId = Trimmed(request.EvaluatorPromptId)
                                    ?? Trimmed(_configuration["AIScreening:EvaluatorPromptId"]);

            try
            {
                var rubric = draft.Plan.ToTransSkills();

                var assessmentGroupId = await ResolveAssessmentGroupAsync(draft);

                var templateName = await BuildUniqueTemplateNameAsync(draft);
                var templateId = await _aiScreeningRepository.CreateAIAudioTemplate(
                    templateName, assessmentGroupId, rubric, loggedInUser);

                if (templateId <= 0)
                    return Failed("Could not create the AI Audio template for this screening.");

                draft.CreatedTemplateId = templateId;
                await SaveDraftAsync(draft);

                var irfId = await BuildUniqueChannelCodeAsync(draft);
                var schedulingLink = schedulingLinkFactory == null
                    ? string.Empty
                    : await schedulingLinkFactory(irfId);

                var irfVm = BuildChannelViewModel(draft, request, irfId, templateId, assessmentGroupId, schedulingLink);

                var created = await _irfService.CreateSkillChannelUsingTemplateByInterviewLinkPopUpV1(
                    irfVm, loggedInUser, profile);

                if (created == null || !created.Item1)
                    return Failed("Could not create the screening channel.");

                // The channel exists from here on. Record that on the draft before anything else can
                // fail, so a retry returns this channel instead of minting a second one.
                draft.CreatedIrfId = irfId;
                await SaveDraftAsync(draft);

                // Item2 is the flat rate the proc computes into its @flatRate output parameter, NOT
                // the new row's id — usp_CreateSkillChannelFromTemplateV1 does not return an
                // identity. Resolve the real IRF.id by the channel code, the way the existing
                // create-from-template action does. Using Item2 here would stamp the rubric, prompt
                // ids and ideal candidate profile onto whatever unrelated channel happened to have
                // id = the flat rate.
                var createdChannel = await _irfService.GetSkillChannelDetailsByName(irfId);
                var irfIntId = createdChannel?.id ?? 0;

                if (irfIntId <= 0)
                {
                    return Failed(
                        $"Screening channel '{irfId}' was created, but its id could not be read back, " +
                        "so the rubric, prompt ids and ideal candidate profile were not applied. " +
                        "Open the channel from Edit IRF and save it to finish setting it up.");
                }

                // V1 is shared with the human-panel route and left unmodified, so the two-section
                // count, the prompt ids, the ideal candidate profile and the sectioned rubric are
                // stamped here.
                await _aiScreeningRepository.SaveAIScreeningProvisioning(
                    irfIntId,
                    skillSetsEvaluate: 2,
                    openAIPromptId: primaryPromptId,
                    openAIEvaluatorPromptId: evaluatorPromptId,
                    idealCandidateProfile: draft.ConfirmedIdealCandidateProfile ?? draft.ProposedIdealCandidateProfile,
                    rubric: draft.Plan.ToTransSkills(irfIntId),
                    // Heading for section two on the report. Section one is headed by JDTiltle, which
                    // stays the job title — it is what the adaptive interviewer is told to interview for.
                    jdTitleTwo: AIScreeningIntakeHelper.GetSectionTitle(
                        AIScreeningSectionEnum.MotivationToJoinFitment));

                if (usingSharedPrompt)
                {
                    // Stamped, so the channel does not depend on the config fallback at run time —
                    // but it is the prompt every other AI interview uses, and editing it in the
                    // OpenAI account will change all of them.
                    await _applicationErrorLogger.LogMessage(
                        LogLevelInfo.Warning,
                        $"AI screening channel '{irfId}' was provisioned with the shared " +
                        $"OpenAI:AIInterviewerPromptId because no Primary Prompt Id was supplied on the " +
                        $"intake form and AIScreening:PrimaryPromptId is not set. Set one on the channel.");
                }

                draft.CreatedIrfIntId = irfIntId;
                draft.CreatedIrfId = irfId;
                await SaveDraftAsync(draft);

                return new AIScreeningChannelResult
                {
                    Success = true,
                    IrfIntId = irfIntId,
                    IrfId = irfId,
                    TemplateId = templateId,
                    CandidateLink = schedulingLink,
                    Message = $"Screening channel '{irfId}' created."
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Failed("An error occurred while creating the screening channel: " + ex.Message);
            }
        }

        /// <summary>
        /// Picks the Assessment Group to stamp on the channel.
        ///
        /// Which group an AI Audio Adaptive channel belongs to does not affect how it runs — there
        /// is no interviewer panel to staff — so this never blocks creation. It only tries to keep
        /// the channel visible: roughly 19 procs INNER JOIN AssessmentGroup on IRF.AssessmentGroupId,
        /// and an unmatched value quietly drops the channel from those surfaces.
        ///
        /// In order: the nearest group by name, an explicitly configured catch-all, then the seeded
        /// "AIInterview" catch-all. If none resolve, the channel is still created and a warning is
        /// logged — an unstamped channel is a reporting problem, not a reason to lose the work.
        /// </summary>
        private async Task<int> ResolveAssessmentGroupAsync(AIScreeningDraft draft)
        {
            //var matched = await _aiScreeningRepository.ResolveAssessmentGroupIdByJobTitle(
            //    draft.JobTitle,
            //    ReadDouble("AIScreening:AssessmentGroupMatchThreshold", AssessmentGroupMatcher.DefaultMinimumScore));

            //if (matched > 0) return matched;

            var configured = ReadInt("AIScreening:CatchAllAssessmentGroupId", 0);
            if (configured > 0) return configured;

            // Resolved by name rather than by a pinned id, so the seed script is the only place the
            // catch-all is defined and no environment needs its identity value in configuration.
            var catchAllName = _configuration["AIScreening:CatchAllAssessmentGroupName"];
            if (string.IsNullOrWhiteSpace(catchAllName)) catchAllName = DefaultCatchAllAssessmentGroupName;

            var catchAll = await _aiScreeningRepository.GetAssessmentGroupIdByName(catchAllName);
            if (catchAll > 0) return catchAll;

            await _applicationErrorLogger.LogMessage(
                LogLevelInfo.Warning,
                $"AI screening: no Assessment Group could be resolved for job title " +
                $"'{draft.JobTitle}', and the '{catchAllName}' catch-all does not exist. The channel " +
                "is being created without one, so it will not appear on surfaces that join on the " +
                "Assessment Group. Run the AssessmentGroup.sql seed to create the catch-all.");

            return 0;
        }

        private IRFVm BuildChannelViewModel(
            AIScreeningDraft draft,
            AIScreeningChannelRequest request,
            string irfId,
            int templateId,
            int assessmentGroupId,
            string schedulingLink)
        {
            return new IRFVm
            {
                IRFId = irfId,
                JDTiltle = string.IsNullOrWhiteSpace(draft.Plan?.RoleLabel) ? draft.JobTitle : draft.Plan.RoleLabel,
                JDLink = draft.JdBlobUrl,
                ClientId = request.ClientId,
                ClientUserIdPopUp = request.ClientUserIds?.ToList(),
                BillingOwner = request.BillingOwner,
                CreatedFromTemplateId = templateId,
                // Left unset rather than "0" when nothing resolved — the joins treat both the same,
                // and a real id is never 0, so a stray zero would only read as a broken reference.
                AssessmentGroupId = assessmentGroupId > 0
                    ? assessmentGroupId.ToString(CultureInfo.InvariantCulture)
                    : null,
                CalendlyLink = schedulingLink,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(3),
                NoOfIntterview = 1,

                // The AI Audio Adaptive state. Not a third AIInterviewType: "AI Audio Adaptive" is
                // IsAIInterview = 1 AND AIInterviewType = Adaptive, which is what ~15 procs read.
                IsAIInterview = true,
                AIInterviewType = (int)AIInterviewTypeEnum.Adaptive
            };
        }

        /// <summary>
        /// Template names are UNIQUE (UC_ClonableSkillChannel_Name) and these are generated per job
        /// description, so the draft id is part of the name rather than the job title alone.
        /// </summary>
        private async Task<string> BuildUniqueTemplateNameAsync(AIScreeningDraft draft)
        {
            var title = Sanitize(draft.JobTitle, 120);
            if (string.IsNullOrWhiteSpace(title)) title = "AI Screening";

            var candidate = $"{title} - AI Screening {draft.DraftId}";
            if (candidate.Length > 200) candidate = candidate.Substring(0, 200);

            if (!await _aiScreeningRepository.IsTemplateNameTaken(candidate))
                return candidate;

            // The draft id already makes a collision unlikely; this covers a retried draft.
            for (var attempt = 2; attempt <= 20; attempt++)
            {
                var retry = $"{candidate} ({attempt})";
                if (retry.Length > 200) retry = retry.Substring(0, 200);
                if (!await _aiScreeningRepository.IsTemplateNameTaken(retry))
                    return retry;
            }

            return $"AI Screening {draft.DraftId} {DateTime.Now:yyyyMMddHHmmss}";
        }

        /// <summary>Channel code, unique across IRF.</summary>
        private async Task<string> BuildUniqueChannelCodeAsync(AIScreeningDraft draft)
        {
            var initials = BuildInitials(draft.JobTitle);
            var suffix = draft.DraftId?.Replace("d_", string.Empty).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(suffix)) suffix = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

            var candidate = $"HRSCR-{initials}-{suffix}";
            if (_irfService.CheckUniqueness(candidate))
                return candidate;

            for (var attempt = 2; attempt <= 20; attempt++)
            {
                var retry = $"HRSCR-{initials}-{suffix}-{attempt}";
                if (_irfService.CheckUniqueness(retry))
                    return retry;
            }

            return $"HRSCR-{Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant()}";
        }

        private static string BuildInitials(string jobTitle)
        {
            if (string.IsNullOrWhiteSpace(jobTitle)) return "GEN";

            var initials = new string(jobTitle
                .Split(new[] { ' ', '-', '_', '/', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => char.IsLetter(w[0]))
                .Take(3)
                .Select(w => char.ToUpperInvariant(w[0]))
                .ToArray());

            return string.IsNullOrWhiteSpace(initials) ? "GEN" : initials;
        }

        private static string Sanitize(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var cleaned = new string(value
                .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '.' || c == '(' || c == ')')
                .ToArray())
                .Trim();

            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            return cleaned.Length > maxLength ? cleaned.Substring(0, maxLength).Trim() : cleaned;
        }

        private static AIScreeningChannelResult Failed(string message) =>
            new AIScreeningChannelResult { Success = false, Message = message };

        /// <summary>Trimmed value, or null when there is nothing there — so ?? chains read correctly.</summary>
        private static string Trimmed(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private int ReadInt(string key, int fallback) =>
            int.TryParse(_configuration[key], out var value) && value > 0 ? value : fallback;

        private double ReadDouble(string key, double fallback) =>
            double.TryParse(
                _configuration[key], NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                && value > 0d && value <= 1d
                    ? value
                    : fallback;

        #endregion
    }
}
