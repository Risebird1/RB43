using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Web.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    /// <summary>
    /// The AI Audio Adaptive HR screening intake.
    ///
    /// This lives on its own route and does not touch the existing skill-channel create flow —
    /// that isolation is the point of the feature. Channel creation still runs through the shared,
    /// unmodified usp_CreateSkillChannelFromTemplateV1.
    /// </summary>
    public class AIScreeningController : Controller
    {
        private readonly IAIScreeningIntakeService _aiScreeningIntakeService;
        private readonly IClientService _clientService;
        private readonly IIRFService _irfService;
        private readonly IInterviewSchedulerService _interviewSchedulerService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IConfiguration _configuration;

        public AIScreeningController(
            IAIScreeningIntakeService aiScreeningIntakeService,
            IClientService clientService,
            IIRFService irfService,
            IInterviewSchedulerService interviewSchedulerService,
            IFileMimeTypeHelper fileMimeTypeHelper,
            IApplicationErrorLogger applicationErrorLogger,
            IConfiguration configuration)
        {
            _aiScreeningIntakeService = aiScreeningIntakeService;
            _clientService = clientService;
            _irfService = irfService;
            _interviewSchedulerService = interviewSchedulerService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _applicationErrorLogger = applicationErrorLogger;
            _configuration = configuration;
        }

        #region Screen

        /// <summary>Screen 1-4: the whole intake, one page with four steps.</summary>
        [HttpGet]
        public async Task<IActionResult> New()
        {
            if (!IsSignedIn()) return RedirectToAction("Index", "Account");
            if (!IsAllowed()) return RedirectToAction("AccessDenied", "Account");

            var draft = await _aiScreeningIntakeService.CreateDraftAsync(CurrentUser());
            if (draft == null)
            {
                TempData["AIScreeningMessage"] = "Could not start a new screening draft. Try again.";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Clients = (await _clientService.GetAllClients())?.ToList();

            // Pre-fill only. Whatever the recruiter submits on the form is what the channel carries.
            ViewBag.DefaultPrimaryPromptId = _configuration["AIScreening:PrimaryPromptId"];
            ViewBag.DefaultEvaluatorPromptId = _configuration["AIScreening:EvaluatorPromptId"];

            return View(draft);
        }

        #endregion

        #region Step 1 — job description and instruction

        /// <summary>Pin 1: stores the JD blob and keeps the reference on the draft.</summary>
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        [RequestSizeLimit(104857600)]
        public async Task<JsonResult> AttachJd(string draftId)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            try
            {
                var file = Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "Attach a job description first." });

                var mimeType = _fileMimeTypeHelper.GetFileMimeType(file);
                if (!IsSupportedJdType(mimeType))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Unsupported file. Upload the job description as PDF, Word or plain text."
                    });
                }

                var blobUrl = await _aiScreeningIntakeService.AttachJdAsync(draftId, file);
                if (string.IsNullOrWhiteSpace(blobUrl))
                    return Json(new { success = false, message = "Could not read that job description. Try another file." });

                var draft = await _aiScreeningIntakeService.GetDraftAsync(draftId);
                return Json(new
                {
                    success = true,
                    jdUrl = blobUrl,
                    fileName = draft?.JdFileName,
                    fileSizeKb = draft == null ? 0 : (int)Math.Ceiling(draft.JdFileSizeBytes / 1024d),
                    jobTitle = draft?.JobTitle,
                    // The recruiter should see when a JD produced no readable text, because every
                    // prompt downstream is weaker without it.
                    hasText = !string.IsNullOrWhiteSpace(draft?.JdText)
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Upload failed: " + ex.Message });
            }
        }

        /// <summary>Pin 2, spoken: transcribes the recording and writes it to the draft.</summary>
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        [RequestSizeLimit(104857600)]
        public async Task<JsonResult> TranscribeInstruction(string draftId, bool isMultiLingual = false)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            try
            {
                var file = Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "No recording received. Record the instruction first." });

                await using var stream = file.OpenReadStream();
                var transcript = await _aiScreeningIntakeService.TranscribeInstructionAsync(
                    draftId, stream, file.FileName, isMultiLingual);

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    // The same message the DHI screen returns, and the recording is kept either way.
                    return Json(new
                    {
                        success = false,
                        message = "Could not produce a clear transcript. Try re-recording in a quiet environment."
                    });
                }

                return Json(new { success = true, transcript });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Transcription failed: " + ex.Message });
            }
        }

        /// <summary>
        /// Pin 2, typed: writes the same field a transcription would, without touching the
        /// transcription service.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> SetInstruction(string draftId, string instruction)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            if (string.IsNullOrWhiteSpace(instruction))
                return Json(new { success = false, message = "Type or record an instruction first." });

            var saved = await _aiScreeningIntakeService.SetInstructionAsync(draftId, instruction);
            return saved
                ? Json(new { success = true, instruction = instruction.Trim() })
                : Json(new { success = false, message = "This draft has expired. Start a new screening." });
        }

        #endregion

        #region Step 2 — ideal candidate

        /// <summary>Pin 3: proposes the profile from the job description.</summary>
        [HttpPost]
        public async Task<JsonResult> ProposeIdealCandidate(string draftId)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            try
            {
                var proposal = await _aiScreeningIntakeService.ProposeIdealCandidateAsync(draftId);
                if (proposal == null)
                    return Json(new { success = false, message = "This draft has expired. Start a new screening." });

                return Json(new { success = true, profile = proposal.Profile, message = proposal.Message });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not propose a profile: " + ex.Message });
            }
        }

        /// <summary>Pin 3: accepts the recruiter's edits. Lands on IRF.IdealCandidateProfile.</summary>
        [HttpPost]
        public async Task<JsonResult> ConfirmIdealCandidate(string draftId, string idealCandidateProfile)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            if (string.IsNullOrWhiteSpace(idealCandidateProfile))
                return Json(new { success = false, message = "The ideal candidate profile cannot be empty." });

            var saved = await _aiScreeningIntakeService.ConfirmIdealCandidateAsync(draftId, idealCandidateProfile);
            return saved
                ? Json(new { success = true })
                : Json(new { success = false, message = "This draft has expired. Start a new screening." });
        }

        #endregion

        #region Step 3 — the plan

        /// <summary>Pin 4: drafts both sections.</summary>
        [HttpPost]
        public async Task<JsonResult> GeneratePlan(string draftId)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            try
            {
                var plan = await _aiScreeningIntakeService.GeneratePlanAsync(draftId);
                if (plan == null)
                    return Json(new { success = false, message = "Could not draft the interview plan. Try again." });

                return Json(new { success = true, plan = ToPlanPayload(plan) });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not draft the interview plan: " + ex.Message });
            }
        }

        /// <summary>Pin 5: applies a spoken or typed revision and returns the diff.</summary>
        [HttpPost]
        public async Task<JsonResult> RevisePlan(string draftId, string transcript)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            if (string.IsNullOrWhiteSpace(transcript))
                return Json(new { success = false, message = "Say or type what should change." });

            try
            {
                var revision = await _aiScreeningIntakeService.RevisePlanByVoiceAsync(draftId, transcript);
                if (revision == null)
                    return Json(new { success = false, message = "Draft the plan before revising it." });

                // An instruction that specifies no rubric change is a valid response, not an error:
                // success stays true and the message explains why nothing moved.
                return Json(new
                {
                    success = true,
                    changed = revision.Changed,
                    added = revision.Added,
                    removed = revision.Removed,
                    modified = revision.Modified,
                    warnings = revision.Warnings,
                    weightsAdjusted = revision.WeightsAdjusted,
                    message = revision.Message,
                    plan = ToPlanPayload(revision.Plan)
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not apply the revision: " + ex.Message });
            }
        }

        /// <summary>Transcribes a spoken revision without writing it to the draft instruction.</summary>
        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        [RequestSizeLimit(104857600)]
        public async Task<JsonResult> TranscribeRevision(string draftId, bool isMultiLingual = false)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            try
            {
                var file = Request.Form.Files.FirstOrDefault();
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "No recording received." });

                await using var stream = file.OpenReadStream();

                // Deliberately not SetInstructionAsync: a revision refines the plan, it does not
                // replace the original instruction the plan was drafted from.
                var draft = await _aiScreeningIntakeService.GetDraftAsync(draftId);
                if (draft == null)
                    return Json(new { success = false, message = "This draft has expired. Start a new screening." });

                var transcript = await _aiScreeningIntakeService.TranscribeInstructionAsync(
                    draftId, stream, file.FileName, isMultiLingual);

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Could not produce a clear transcript. Try re-recording in a quiet environment."
                    });
                }

                // Put the original instruction back — only the plan revision uses this transcript.
                await _aiScreeningIntakeService.SetInstructionAsync(draftId, draft.Instruction ?? transcript);
                return Json(new { success = true, transcript });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Transcription failed: " + ex.Message });
            }
        }

        #endregion

        #region Step 4 — create

        /// <summary>Client users and billing owners for the chosen client.</summary>
        [HttpGet]
        public JsonResult ClientUsers(int clientId)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            var users = _irfService.GetClientUser(clientId)?.ToList();
            return Json(new
            {
                success = true,
                users = (users ?? new List<Users>()).Select(u => new { id = u.id, name = u.EmailId })
            });
        }

        /// <summary>
        /// The single "Create screening channel" click. Idempotent on draft id — a double submit
        /// returns the channel already created rather than creating a second one.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> CreateChannel(
            string draftId, int clientId, int billingOwner, List<int> clientUserIds,
            string primaryPromptId, string evaluatorPromptId, bool? candidateTranscriptReview = null)
        {
            if (!IsSignedIn()) return Json(Unauthorized_());
            if (!IsAllowed()) return Json(Forbidden_());

            try
            {
                var request = new AIScreeningChannelRequest
                {
                    DraftId = draftId,
                    ClientId = clientId,
                    BillingOwner = billingOwner,
                    ClientUserIds = clientUserIds ?? new List<int>(),
                    PrimaryPromptId = primaryPromptId,
                    EvaluatorPromptId = evaluatorPromptId,
                    // Absent means the recruiter left it on "use the environment default", which
                    // writes NULL and keeps the appsettings switch in charge for this channel.
                    CandidateTranscriptReview = candidateTranscriptReview
                };

                var result = await _aiScreeningIntakeService.CreateChannelAsync(
                    request,
                    CurrentUser(),
                    CurrentProfile(),
                    irfId => BuildSchedulingLinkAsync(irfId, request.DraftId));

                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    irfId = result.IrfId,
                    irfIntId = result.IrfIntId,
                    templateId = result.TemplateId,
                    candidateLink = result.CandidateLink,
                    alreadyCreated = result.AlreadyCreated,
                    editUrl = result.Success ? Url.Action("EditIRF", "IRF", new { id = result.IrfIntId }) : null
                });
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return Json(new { success = false, message = "Could not create the screening channel: " + ex.Message });
            }
        }

        /// <summary>
        /// Builds the candidate scheduling link for the new channel. Mirrors
        /// IRFController.AutoGenerateSchedulingLink for an AI interview: 15-minute slots, no buffer.
        /// </summary>
        private async Task<string> BuildSchedulingLinkAsync(string irfId, string draftId)
        {
            try
            {
                var wide = new List<perdaytime> { new perdaytime { Start = "8", End = "23" } };
                var standard = new List<perdaytime> { new perdaytime { Start = "10", End = "23" } };

                var week = new List<daytimes_second>
                {
                    new daytimes_second { Daytype = "sunday", dayCheck = true, dayvalue = wide },
                    new daytimes_second { Daytype = "monday", dayCheck = true, dayvalue = standard },
                    new daytimes_second { Daytype = "tuesday", dayCheck = true, dayvalue = standard },
                    new daytimes_second { Daytype = "wednesday", dayCheck = true, dayvalue = standard },
                    new daytimes_second { Daytype = "thursday", dayCheck = true, dayvalue = standard },
                    new daytimes_second { Daytype = "friday", dayCheck = true, dayvalue = standard },
                    new daytimes_second { Daytype = "saturday", dayCheck = true, dayvalue = wide }
                };

                var start = DateTime.Now;
                var model = new daytimes_report
                {
                    setvalue = week,
                    StartDate = start,
                    EndDate = start.AddDays(60),
                    duration = 15,
                    BufferTime = 0,
                    EventName = irfId + "_Scheduling",
                    SkillChannel = irfId,
                    Concurrency = 10,
                    TimeSlotDays = 6,
                    sourcevalue = "AIScreeningIntake"
                };

                var eventId = await _interviewSchedulerService.AutoCreateEvent(model);
                var created = await _interviewSchedulerService.FetchEventById(eventId);

                return created == null
                    ? string.Empty
                    : $"https://{Request.Host}/InterviewScheduler/ScheduleEvent?id={created.Uniqueid}";
            }
            catch (Exception ex)
            {
                // A missing link must not cost the channel — it can be regenerated from Edit IRF.
                await _applicationErrorLogger.LogMessage(
                    LogLevelInfo.Exception, $"AI screening scheduling link failed for draft {draftId}: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Shapes a plan for the screen: each section carries its own running weight total, so a
        /// revision that unbalances one is visible before submit rather than at scoring time.
        /// </summary>
        private static object ToPlanPayload(AIScreeningPlan plan)
        {
            if (plan == null) return null;

            return new
            {
                roleLabel = plan.RoleLabel,
                durationMinutes = plan.DurationMinutes,
                parameterCount = plan.ParameterCount,
                message = plan.Message,
                sections = (plan.Sections ?? new List<AIScreeningPlanSection>()).Select(s => new
                {
                    section = (int)s.Section,
                    title = s.Title,
                    weightTotal = s.WeightTotal,
                    isBalanced = s.IsBalanced,
                    parameters = (s.Parameters ?? new List<TransSkill>()).Select(p => new
                    {
                        name = p.SkillName,
                        weight = p.Weightage,
                        mandatory = p.isEliminationskill,
                        minutes = p.Timelimit > 0 ? p.Timelimit : 3,
                        level = p.LevelName,
                        expectations = p.Natureofquestion ?? string.Empty,
                        concepts = p.Concepts ?? string.Empty
                    })
                })
            };
        }

        private static bool IsSupportedJdType(string mimeType) =>
            mimeType == "application/pdf"
            || mimeType == "application/msword"
            || mimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            || mimeType == "text/plain";

        private bool IsSignedIn() => HttpContext.Session.GetString("email") != null;

        private string CurrentUser() =>
            HttpContext.Session.GetString("email")?.Trim().Replace("\"", "");

        private string CurrentProfile() =>
            HttpContext.Session.GetString("profileName")?.Trim().Replace("\"", "");

        // Shared with the views that offer the entry point, so a button is never shown to someone
        // this would turn away.
        private bool IsAllowed() => AIScreeningAccess.IsAllowed(CurrentProfile());

        private static object Unauthorized_() => new { success = false, message = "Unauthorized" };

        private static object Forbidden_() => new { success = false, message = "Forbidden" };

        #endregion
    }
}
