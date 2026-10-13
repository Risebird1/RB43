using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel.AdvancedScreening;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Risebird.Business.AdvancedScreening
{
    /// <inheritdoc cref="IAdvancedScreeningService"/>
    public class AdvancedScreeningService : IAdvancedScreeningService
    {
        public const string RubricPromptKey = "OpenAI:AdvancedRubricGeneratorPromptId";
        public const string ScorerPromptKey = "OpenAI:AdvancedResumeScorerPromptId";
        public const string WorkHistoryPromptKey = "OpenAI:WorkHistoryExtractorPromptId";

        private const int MaxInstructionsLength = 2000;
        private const int MaxJdChars = 20000;
        private const int MaxResumeChars = 30000;
        private const int MaxConcurrentScreenings = 4;

        private static readonly string[] ConvertibleExtensions = { ".doc", ".docx", ".rtf", ".odt" };

        private readonly IAdvancedScreeningRepository _repository;
        private readonly IOpenAIResponsesHelper _openAI;
        private readonly IPdfReaderHelper _pdfReader;
        private readonly IAzureStorageHelper _storage;
        private readonly IConfiguration _configuration;
        private readonly IApplicationErrorLogger _logger;
        private readonly AuthenticityFactsBuilder _factsBuilder;

        public AdvancedScreeningService(IAdvancedScreeningRepository repository, IOpenAIResponsesHelper openAI,
            IPdfReaderHelper pdfReader, IAzureStorageHelper storage, IConfiguration configuration,
            IApplicationErrorLogger logger, ICompanyRegistryClient registry)
        {
            _repository = repository;
            _openAI = openAI;
            _pdfReader = pdfReader;
            _storage = storage;
            _configuration = configuration;
            _logger = logger;
            _factsBuilder = new AuthenticityFactsBuilder(repository, registry);
        }

        // ================================================================== rubric

        public async Task<IRFScreeningRubricVM> GetRubricAsync(int irfId)
        {
            var vm = await _repository.GetIRFScreeningRubric(irfId);
            if (vm != null && !string.IsNullOrWhiteSpace(vm.RubricJson))
            {
                vm.Rubric = RubricValidator.Normalize(RubricValidator.Parse(vm.RubricJson));
            }
            return vm;
        }

        public async Task<RubricOperationResult> GenerateRubricAsync(int irfId, string user)
        {
            var vm = await GetRubricAsync(irfId);
            if (vm == null)
            {
                return Fail("This skill channel could not be found.");
            }

            await _repository.SaveIRFScreeningRubric(new RubricSaveRequest { IRFId = irfId, User = user, Status = RubricStatus.Generating });
            try
            {
                // The stored JD text wins: it is either an earlier extraction or the recruiter's own
                // correction of one. Only an empty value is (re-)extracted from the JD file.
                var jdText = vm.JDText;
                if (string.IsNullOrWhiteSpace(jdText))
                {
                    jdText = await ExtractTextAsync(vm.JDLink);
                    if (!string.IsNullOrWhiteSpace(jdText))
                    {
                        jdText = Clip(jdText, MaxJdChars);
                        await _repository.SaveIRFScreeningRubric(new RubricSaveRequest { IRFId = irfId, User = user, JDText = jdText });
                    }
                }
                if (string.IsNullOrWhiteSpace(jdText))
                {
                    return await FailGeneration(irfId, user,
                        "No job description text could be read from this IRF's JD file. Paste the JD text on the Screening Rubric page and generate again.");
                }

                var input = AdvancedScreeningPromptBuilder.BuildRubricInput(
                    jdText, vm.IdealCandidateProfile, vm.ApplyInstructionsToRubric ? vm.Instructions : null);

                ScreeningRubric rubric = null;
                string lastProblem = null;
                for (var attempt = 1; attempt <= 2 && rubric == null; attempt++)
                {
                    var raw = await CallJsonAsync(RubricPromptKey, AdvancedScreeningPrompts.RubricGenerator, input, "AdvancedRubricGeneration");
                    var parsed = RubricValidator.Normalize(RubricValidator.Parse(raw));
                    if (parsed == null)
                    {
                        lastProblem = "The rubric generator did not return a readable rubric.";
                        continue;
                    }
                    var missing = RubricValidator.MissingKeys(parsed);
                    if (missing.Count > 0)
                    {
                        lastProblem = "The rubric generator left out: " + string.Join(", ", missing) + ".";
                        continue;
                    }
                    rubric = parsed;
                }
                if (rubric == null)
                {
                    return await FailGeneration(irfId, user, lastProblem);
                }

                // One repair: scale the weights to 300, keeping their proportions.
                if (RubricValidator.Total(rubric) != RubricCriteria.TotalWeight)
                {
                    rubric = RubricValidator.RepairWeights(rubric);
                }
                var errors = RubricValidator.Validate(rubric);
                if (errors.Count > 0)
                {
                    return await FailGeneration(irfId, user, string.Join(" ", errors));
                }
                if (string.IsNullOrWhiteSpace(rubric.JdTitle))
                {
                    rubric.JdTitle = vm.JDTiltle;
                }

                await _repository.SaveIRFScreeningRubric(new RubricSaveRequest
                {
                    IRFId = irfId,
                    User = user,
                    RubricJson = JsonConvert.SerializeObject(rubric),
                    TotalWeight = rubric.TotalWeight,
                    Status = RubricStatus.Generated
                });
                return new RubricOperationResult { Success = true, Rubric = await GetRubricAsync(irfId) };
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return await FailGeneration(irfId, user, "Something went wrong generating the rubric. Try again in a moment.");
            }
        }

        private async Task<RubricOperationResult> FailGeneration(int irfId, string user, string error)
        {
            await _repository.SaveIRFScreeningRubric(new RubricSaveRequest
            {
                IRFId = irfId,
                User = user,
                Status = RubricStatus.Failed,
                LastError = error
            });
            return new RubricOperationResult { Success = false, Error = error, Rubric = await GetRubricAsync(irfId) };
        }

        public async Task<RubricOperationResult> SaveRubricAsync(int irfId, ScreeningRubric rubric, bool approve, string user, string jdText = null)
        {
            var normalized = RubricValidator.Normalize(rubric);
            if (normalized == null)
            {
                return Fail("Nothing to save.");
            }
            if (approve)
            {
                var errors = RubricValidator.Validate(normalized);
                if (errors.Count > 0)
                {
                    return new RubricOperationResult { Success = false, Error = string.Join(" ", errors), Rubric = await GetRubricAsync(irfId) };
                }
            }

            var saved = await _repository.SaveIRFScreeningRubric(new RubricSaveRequest
            {
                IRFId = irfId,
                User = user,
                RubricJson = JsonConvert.SerializeObject(normalized),
                TotalWeight = normalized.TotalWeight,
                Status = approve ? null : RubricStatus.Draft,
                Approve = approve,
                JDText = jdText == null ? null : Clip(jdText, MaxJdChars)
            });
            if (saved == null)
            {
                return Fail("The rubric could not be saved. Try again in a moment.");
            }
            return new RubricOperationResult { Success = true, Rubric = await GetRubricAsync(irfId) };
        }

        public async Task<SaveInstructionsResult> SaveInstructionsAsync(int irfId, string instructions, bool applyToRubric, bool applyToScoring, string user)
        {
            instructions = instructions?.Trim();
            if (instructions != null && instructions.Length > MaxInstructionsLength)
            {
                return new SaveInstructionsResult { Success = false, Error = $"Instructions can be at most {MaxInstructionsLength} characters." };
            }

            var saved = await _repository.SaveIRFScreeningRubric(new RubricSaveRequest
            {
                IRFId = irfId,
                User = user,
                SaveInstructions = true,
                Instructions = instructions ?? string.Empty,
                ApplyInstructionsToRubric = applyToRubric,
                ApplyInstructionsToScoring = applyToScoring
            });
            if (saved == null)
            {
                return new SaveInstructionsResult { Success = false, Error = "The instructions could not be saved." };
            }
            var counts = await _repository.GetAdvancedScreeningCounts(irfId);
            return new SaveInstructionsResult
            {
                Success = true,
                InstructionsVersion = saved.InstructionsVersion,
                StaleCount = counts?.StaleInstructions ?? 0
            };
        }

        public Task<AdvancedScreeningCounts> GetCountsAsync(int irfId) => _repository.GetAdvancedScreeningCounts(irfId);

        public Task<int> QueueAsync(int irfId, ScreenAllScope scope, int? candidateId, string user) =>
            _repository.QueueAdvancedScreening(irfId, scope.ToString(), candidateId, user);

        public Task<CandidateAdvancedScreeningVM> GetCandidateResultAsync(int candidateId) =>
            _repository.GetCandidateAdvancedScreening(candidateId);

        // ================================================================= scoring

        public async Task<int> ScreenPendingBatchAsync(int batchSize = 20, CancellationToken cancellationToken = default)
        {
            var batch = (await _repository.ClaimCandidatesForAdvancedScreening(batchSize)).ToList();
            if (batch.Count == 0)
            {
                return 0;
            }

            using (var gate = new SemaphoreSlim(MaxConcurrentScreenings))
            {
                var tasks = batch.Select(async item =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        await ScreenCandidateAsync(item.CandidateId, string.IsNullOrWhiteSpace(item.TriggeredBy) ? "Job" : item.TriggeredBy);
                    }
                    finally
                    {
                        gate.Release();
                    }
                });
                await Task.WhenAll(tasks);
            }
            return batch.Count;
        }

        public async Task<ScreenCandidateOutcome> ScreenCandidateAsync(int candidateId, string triggeredBy)
        {
            CandidateAdvancedScreeningVM candidate = null;
            try
            {
                candidate = await _repository.GetCandidateAdvancedScreening(candidateId);
                if (candidate == null)
                {
                    return new ScreenCandidateOutcome { Status = AdvancedScreeningStatus.Failed, Error = "Candidate not found." };
                }

                var rubricVm = await GetRubricAsync(candidate.IRFId);
                if (rubricVm == null || !rubricVm.IsApproved || rubricVm.Rubric == null)
                {
                    return await Finish(candidate, AdvancedScreeningStatus.Skipped, "The IRF's screening rubric is not approved.", triggeredBy);
                }

                // 1. Resume text.
                var (resumeText, readableLink, skipReason) = await ExtractResumeAsync(candidate.ResumeLink);
                if (skipReason != null)
                {
                    return await Finish(candidate, AdvancedScreeningStatus.Skipped, skipReason, triggeredBy);
                }
                resumeText = Clip(resumeText, MaxResumeChars);

                // 2. Facts: work history (extracted once per screening), then the computed checks.
                var history = await ExtractWorkHistoryAsync(candidate, resumeText);
                var hidden = readableLink != null ? await _pdfReader.FindHiddenTextAsync(readableLink) : Array.Empty<string>();
                var facts = await _factsBuilder.BuildAsync(candidate, history, resumeText, rubricVm.JDText, hidden, DateTime.UtcNow);
                var factsJson = JsonConvert.SerializeObject(facts);

                // 3. The scorer, validated, retried once.
                var instructions = rubricVm.ApplyInstructionsToScoring ? rubricVm.Instructions : null;
                var input = AdvancedScreeningPromptBuilder.BuildScoringInput(
                    AdvancedScreeningPromptBuilder.RubricJsonForScoring(rubricVm.Rubric), instructions, factsJson, resumeText);

                AdvancedScreeningResult result = null;
                string problem = null;
                for (var attempt = 1; attempt <= 2 && result == null; attempt++)
                {
                    var raw = await CallJsonAsync(ScorerPromptKey, AdvancedScreeningPrompts.ResumeScorer, input, "AdvancedResumeScoring");
                    var parsed = AdvancedScreeningScorer.Parse(raw, out problem);
                    if (parsed == null)
                    {
                        continue;
                    }
                    problem = AdvancedScreeningScorer.Validate(parsed, rubricVm.Rubric);
                    if (problem == null)
                    {
                        result = parsed;
                    }
                }
                if (result == null)
                {
                    return await Finish(candidate, AdvancedScreeningStatus.Failed, problem ?? "The scorer did not respond.", triggeredBy, factsJson);
                }

                // 4. Everything numeric is computed here, never taken from the model.
                AdvancedScreeningScorer.MergeFactFlags(result, facts);
                var totals = AdvancedScreeningScorer.Compute(result, rubricVm.Rubric,
                    rubricVm.RubricVersion, rubricVm.InstructionsVersion, DateTime.UtcNow);

                // 5. Save.
                var saved = await _repository.SaveCandidateAdvancedScreening(new CandidateAdvancedScreeningSave
                {
                    CandidateId = candidate.CandidateId,
                    IRFId = candidate.IRFId,
                    Status = AdvancedScreeningStatus.Completed,
                    RubricVersion = rubricVm.RubricVersion,
                    InstructionsVersion = rubricVm.InstructionsVersion,
                    TotalScore = totals.TotalScore,
                    OverallPercentage = totals.OverallPercentage,
                    Verdict = totals.Verdict,
                    SkillsScore = totals.Sections.FirstOrDefault(s => s.Section == RubricCriteria.Skills)?.Score,
                    WorkScore = totals.Sections.FirstOrDefault(s => s.Section == RubricCriteria.Work)?.Score,
                    EducationScore = totals.Sections.FirstOrDefault(s => s.Section == RubricCriteria.Education)?.Score,
                    AuthenticityRating = totals.AuthenticityRating,
                    AuthenticityFlagCount = totals.AuthenticityFlagCount,
                    ResponseJson = JsonConvert.SerializeObject(result),
                    FactsJson = factsJson,
                    TriggeredBy = triggeredBy
                });
                return saved
                    ? new ScreenCandidateOutcome { Status = AdvancedScreeningStatus.Completed, Result = result }
                    : new ScreenCandidateOutcome { Status = AdvancedScreeningStatus.Failed, Error = "Scored, but the result could not be saved.", Result = result };
            }
            catch (Exception ex)
            {
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                if (candidate != null)
                {
                    return await Finish(candidate, AdvancedScreeningStatus.Failed, "Unexpected error: " + ex.Message, triggeredBy);
                }
                return new ScreenCandidateOutcome { Status = AdvancedScreeningStatus.Failed, Error = ex.Message };
            }
        }

        private async Task<ScreenCandidateOutcome> Finish(CandidateAdvancedScreeningVM candidate, string status, string reason,
            string triggeredBy, string factsJson = null)
        {
            await _repository.SaveCandidateAdvancedScreening(new CandidateAdvancedScreeningSave
            {
                CandidateId = candidate.CandidateId,
                IRFId = candidate.IRFId,
                Status = status,
                ErrorMessage = reason,
                FactsJson = factsJson,
                TriggeredBy = triggeredBy
            });
            return new ScreenCandidateOutcome { Status = status, Error = reason };
        }

        private async Task<WorkHistoryEnvelope> ExtractWorkHistoryAsync(CandidateAdvancedScreeningVM candidate, string resumeText)
        {
            try
            {
                var raw = await CallJsonAsync(WorkHistoryPromptKey, AdvancedScreeningPrompts.WorkHistoryExtractor,
                    AdvancedScreeningPromptBuilder.BuildWorkHistoryInput(resumeText), "WorkHistoryExtraction");
                var history = WorkHistoryParser.Parse(raw);
                if (history != null)
                {
                    await _repository.SaveCandidateWorkHistory(candidate.CandidateId, candidate.CandidateMasterId, candidate.IRFId,
                        JsonConvert.SerializeObject(history));
                }
                return history;
            }
            catch (Exception ex)
            {
                // Without a work history the stability and date facts are reported "not verified";
                // the candidate is still scored.
                await _logger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        // ============================================================= text access

        private static string Extension(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;
            var path = url.Split('?', '#')[0];
            var dot = path.LastIndexOf('.');
            var slash = path.LastIndexOf('/');
            return dot > slash ? path.Substring(dot).ToLowerInvariant() : string.Empty;
        }

        /// <summary>Resume text, the PDF link it was read from (for the hidden-text check), or a reason to skip.</summary>
        private async Task<(string Text, string PdfLink, string SkipReason)> ExtractResumeAsync(string resumeLink)
        {
            if (string.IsNullOrWhiteSpace(resumeLink))
            {
                return (null, null, "No resume on file.");
            }
            var ext = Extension(resumeLink);
            string pdfLink;
            if (ext == ".pdf")
            {
                pdfLink = resumeLink;
            }
            else if (ConvertibleExtensions.Contains(ext) || ext == string.Empty)
            {
                pdfLink = await _storage.DownloadAndConvertFromDocToPdfAsync(resumeLink);
                if (string.IsNullOrWhiteSpace(pdfLink))
                {
                    return (null, null, ext == string.Empty
                        ? "The resume's file type could not be read. Upload a PDF."
                        : $"The {ext} resume could not be converted for reading. Upload a PDF.");
                }
            }
            else
            {
                return (null, null, $"Unsupported resume format ({ext}). Upload a PDF or Word file.");
            }

            var text = await _pdfReader.ReadPdfFromUrlAsync(pdfLink);
            if (string.IsNullOrWhiteSpace(text) || AuthenticityFacts.Words(text).Count < 30)
            {
                return (null, null, "No readable text in the resume; it may be a scanned image. Upload a text PDF.");
            }
            return (text, pdfLink, null);
        }

        private async Task<string> ExtractTextAsync(string link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return null;
            }
            var ext = Extension(link);
            var pdfLink = ext == ".pdf" ? link : await _storage.DownloadAndConvertFromDocToPdfAsync(link);
            return string.IsNullOrWhiteSpace(pdfLink) ? null : await _pdfReader.ReadPdfFromUrlAsync(pdfLink);
        }

        // =================================================================== OpenAI

        /// <summary>
        /// The stored prompt when its id is configured; otherwise the same instructions sent inline,
        /// so the feature works before the stored prompts exist. Both go through
        /// IOpenAIResponsesHelper, which logs every call to OpenAIApiLogs.
        /// </summary>
        private async Task<string> CallJsonAsync(string promptConfigKey, string inlineInstructions, string input, string operationType)
        {
            var promptId = _configuration[promptConfigKey];
            if (!string.IsNullOrWhiteSpace(promptId))
            {
                var run = await _openAI.CreateJsonResponseAsync(input, operationType, promptId);
                return run?.Data?.FirstOrDefault()?.Content?.FirstOrDefault()?.Text?.Value;
            }
            return await _openAI.CreateJsonWithInstructionsAsync(inlineInstructions, input, operationType);
        }

        private static string Clip(string text, int max) =>
            text == null || text.Length <= max ? text : text.Substring(0, max);

        private static RubricOperationResult Fail(string error) => new RubricOperationResult { Success = false, Error = error };
    }
}
