using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Business
{
    /// <summary>
    /// Expert review of AI code interview reports: assignment, the editable expert copy of the AI
    /// report, and the field by field comparison that records what the expert changed.
    /// </summary>
    public class AICodeInterviewService : IAICodeInterviewService
    {
        /// <summary>
        /// The overall technical score is carried as a report level rubric dimension so it is
        /// compared and stored by the same code path as every other dimension.
        /// </summary>
        public const string OverallTechnicalScoreDimension = "Overall Technical Score";

        /// <summary>Ratings differing by less than this are treated as equal.</summary>
        private const decimal RatingEpsilon = 0.0001m;

        private readonly IAICodeInterviewRepository _repository;
        private readonly IAIInterviewerService _aiInterviewerService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public AICodeInterviewService(IAICodeInterviewRepository repository,
            IAIInterviewerService aiInterviewerService,
            IApplicationErrorLogger applicationErrorLogger)
        {
            _repository = repository;
            _aiInterviewerService = aiInterviewerService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        public Task<List<AICodeInterviewQCItemVM>> GetOpenQCItems(string expertEmail)
            => _repository.GetOpenQCItems(expertEmail);

        public Task<List<AICodeInterviewQCItemVM>> GetMyQCItems(string expertEmail, bool? isCompleted = null)
            => _repository.GetMyQCItems(expertEmail, isCompleted);

        public Task<(int AssignmentId, int Status)> AssignExpert(Guid sessionId, string expertEmail, string assignedBy)
            => _repository.AssignExpert(sessionId.ToString(), expertEmail, assignedBy);

        public async Task<AICodeInterviewAuditReportVM> GetAuditReport(Guid sessionId)
        {
            var audit = await _repository.GetAuditReport(sessionId.ToString());
            if (audit == null) return null;

            // The AI copy is materialised on first open so there is always a stable baseline to
            // diff against, even if the assistant's stored evaluation changes later.
            if (audit.AIReport == null)
            {
                var seeded = await BuildAIReportFromEvaluation(sessionId, audit);
                if (seeded != null)
                {
                    await _repository.SaveReport(seeded, "System");
                    audit = await _repository.GetAuditReport(sessionId.ToString());
                }
            }

            HydrateRubricRatings(audit?.AIReport);
            HydrateRubricRatings(audit?.ExpertReport);

            try
            {
                var meeting = await _aiInterviewerService.GetAIInterviewDetailsBySessionId(sessionId);
                var channelSkills = ParseChannelRubric(meeting?.TechSkillsInJson);
                var questionSkills = ParseQuestionMappedSkills(meeting?.QuestionAnswerSetInJson);
                ApplyChannelRubric(audit?.AIReport, channelSkills);
                ApplyChannelRubric(audit?.ExpertReport, channelSkills);
                ApplyQuestionMaxes(audit?.AIReport, channelSkills, questionSkills);
                ApplyQuestionMaxes(audit?.ExpertReport, channelSkills, questionSkills);
            }
            catch (Exception)
            {
                // Channel rubric overlay is display/accumulation; a miss must not hide the audit.
            }

            try
            {
                var evaluation = await _aiInterviewerService.GetParsedAIInterviewEvaluationBySessionId(sessionId);
                if (audit != null)
                    audit.LlmDetection = evaluation?.LlmDetection;
            }
            catch (Exception)
            {
                // LLM detection is informational; a parse miss must not block the audit page.
            }

            return audit;
        }

        public async Task<AICodeInterviewAuditSubmitResultVM> SubmitExpertAudit(AICodeInterviewReportVM expertReport, string user)
        {
            if (expertReport == null || string.IsNullOrWhiteSpace(expertReport.SessionId))
            {
                return new AICodeInterviewAuditSubmitResultVM { Success = false, Message = "Nothing to submit." };
            }

            if (!Guid.TryParse(expertReport.SessionId, out var sessionId))
            {
                return new AICodeInterviewAuditSubmitResultVM { Success = false, Message = "Invalid interview session." };
            }

            try
            {
                var audit = await GetAuditReport(sessionId);
                if (audit?.AIReport == null)
                {
                    return new AICodeInterviewAuditSubmitResultVM
                    {
                        Success = false,
                        Message = "The AI report for this interview could not be loaded."
                    };
                }

                if (audit.Assignment == null)
                {
                    return new AICodeInterviewAuditSubmitResultVM
                    {
                        Success = false,
                        Message = "This interview is not assigned for expert review."
                    };
                }

                // Identity of the report and of each question comes from the stored AI baseline, not
                // from the posted form, so the submission cannot repoint rows at another interview.
                expertReport.ReportSource = (int)AICodeInterviewReportSourceEnum.Expert;
                expertReport.SessionId = audit.AIReport.SessionId;
                expertReport.InterviewId = audit.InterviewId;
                expertReport.OverallVerdict = AIInterviewVerdict.Normalize(expertReport.OverallVerdict);
                AlignExpertRowsToBaseline(audit.AIReport, expertReport);
                AlignExpertRubricToBaseline(audit.AIReport, expertReport);

                expertReport.RubricRatingsJson = SerializeRubricRatings(expertReport.RubricRatings);
                foreach (var row in expertReport.QuestionRatings ?? new List<AICodeInterviewReportQuestionRatingVM>())
                {
                    row.RubricRatingsJson = SerializeRubricRatings(row.RubricRatings);
                }

                var expertReportId = await _repository.SaveReport(expertReport, user);
                if (expertReportId == 0)
                {
                    return new AICodeInterviewAuditSubmitResultVM
                    {
                        Success = false,
                        Message = "The expert report could not be saved."
                    };
                }

                var discrepancies = ComputeDiscrepancies(audit.AIReport, expertReport);
                var outcome = await _repository.SaveDiscrepancies(
                    expertReport.SessionId, audit.Assignment.id, expertReportId, discrepancies);

                return new AICodeInterviewAuditSubmitResultVM
                {
                    Success = true,
                    Message = "Expert review submitted.",
                    ExpertReportId = expertReportId,
                    WasDiscrepancyFound = outcome.WasDiscrepancyFound,
                    MaxSeverity = outcome.MaxSeverity
                };
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return new AICodeInterviewAuditSubmitResultVM
                {
                    Success = false,
                    Message = "The expert review could not be submitted."
                };
            }
        }

        public List<AICodeInterviewReportDiscrepancyVM> ComputeDiscrepancies(
            AICodeInterviewReportVM aiReport, AICodeInterviewReportVM expertReport)
        {
            var rows = new List<AICodeInterviewReportDiscrepancyVM>();
            if (aiReport == null || expertReport == null) return rows;

            var sessionId = aiReport.SessionId;

            AddTextRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.OverallVerdict,
                null, null, null,
                AIInterviewVerdict.Normalize(aiReport.OverallVerdict),
                AIInterviewVerdict.Normalize(expertReport.OverallVerdict),
                AICodeInterviewDiscrepancySeverityEnum.Major);

            // Comments are recorded side by side whether or not they changed: a reworded summary is
            // context for the reader, not a scoring disagreement, so severity stays None.
            AddCommentRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.OverallComment,
                null, null, null, aiReport.OverallComment, expertReport.OverallComment);

            AddRatingRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.OverallCommunicationRating,
                null, null, null, aiReport.OverallCommunicationRating, expertReport.OverallCommunicationRating);

            AddCommentRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.OverallCommunicationComment,
                null, null, null, aiReport.OverallCommunicationComment, expertReport.OverallCommunicationComment);

            AddRubricRows(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.OverallRubricRating,
                null, null, aiReport.RubricRatings, expertReport.RubricRatings);

            var aiRows = aiReport.QuestionRatings ?? new List<AICodeInterviewReportQuestionRatingVM>();
            var expertRows = expertReport.QuestionRatings ?? new List<AICodeInterviewReportQuestionRatingVM>();

            foreach (var aiRow in aiRows.OrderBy(r => r.DisplayOrder))
            {
                var expertRow = expertRows.FirstOrDefault(r => r.DisplayOrder == aiRow.DisplayOrder);
                if (expertRow == null) continue;

                var questionId = aiRow.QuestionId;
                var order = aiRow.DisplayOrder;

                AddRatingRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.QuestionRating,
                    questionId, order, null, aiRow.Rating, expertRow.Rating);

                AddTextRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.QuestionVerdict,
                    questionId, order, null, aiRow.Verdict, expertRow.Verdict,
                    AICodeInterviewDiscrepancySeverityEnum.Major);

                AddCommentRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.QuestionComment,
                    questionId, order, null, aiRow.Comment, expertRow.Comment);

                AddRatingRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.QuestionCommunicationRating,
                    questionId, order, null, aiRow.CommunicationRating, expertRow.CommunicationRating);

                AddCommentRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.QuestionCommunicationComment,
                    questionId, order, null, aiRow.CommunicationComment, expertRow.CommunicationComment);

                AddRubricRows(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.QuestionRubricRating,
                    questionId, order, aiRow.RubricRatings, expertRow.RubricRatings);

                // A corrected transcript is a text fix rather than a scoring disagreement, so it is
                // flagged but only ever Minor.
                AddTextRow(rows, sessionId, AICodeInterviewDiscrepancyFieldTypeEnum.TranscriptText,
                    questionId, order, null, aiRow.TranscriptText, expertRow.TranscriptText,
                    AICodeInterviewDiscrepancySeverityEnum.Minor);
            }

            return rows;
        }

        #region comparison primitives

        private static void AddRatingRow(List<AICodeInterviewReportDiscrepancyVM> rows, string sessionId,
            AICodeInterviewDiscrepancyFieldTypeEnum fieldType, int? questionId, int? displayOrder,
            string fieldKey, decimal? aiValue, decimal? expertValue)
        {
            var severity = CompareRating(aiValue, expertValue);
            if (severity == null) return;

            rows.Add(new AICodeInterviewReportDiscrepancyVM
            {
                SessionId = sessionId,
                FieldType = fieldType.ToString(),
                QuestionId = questionId,
                DisplayOrder = displayOrder,
                FieldKey = fieldKey,
                AIValue = FormatRating(aiValue),
                ExpertValue = FormatRating(expertValue),
                IsDiscrepancy = true,
                Severity = (int)severity.Value
            });
        }

        /// <summary>
        /// Rating comparison: a one point gap is Minor, two points Major, three or more Critical.
        /// A value present on only one side is treated as Major because the reviewers disagree on
        /// whether the dimension applies at all. Returns null when the values match.
        /// </summary>
        private static AICodeInterviewDiscrepancySeverityEnum? CompareRating(decimal? aiValue, decimal? expertValue)
        {
            if (!aiValue.HasValue && !expertValue.HasValue) return null;
            if (!aiValue.HasValue || !expertValue.HasValue) return AICodeInterviewDiscrepancySeverityEnum.Major;

            var delta = Math.Abs(aiValue.Value - expertValue.Value);
            if (delta <= RatingEpsilon) return null;
            if (delta < 1.5m) return AICodeInterviewDiscrepancySeverityEnum.Minor;
            if (delta < 2.5m) return AICodeInterviewDiscrepancySeverityEnum.Major;
            return AICodeInterviewDiscrepancySeverityEnum.Critical;
        }

        private static void AddTextRow(List<AICodeInterviewReportDiscrepancyVM> rows, string sessionId,
            AICodeInterviewDiscrepancyFieldTypeEnum fieldType, int? questionId, int? displayOrder,
            string fieldKey, string aiValue, string expertValue,
            AICodeInterviewDiscrepancySeverityEnum severityOnMismatch)
        {
            if (TextEquals(aiValue, expertValue)) return;

            rows.Add(new AICodeInterviewReportDiscrepancyVM
            {
                SessionId = sessionId,
                FieldType = fieldType.ToString(),
                QuestionId = questionId,
                DisplayOrder = displayOrder,
                FieldKey = fieldKey,
                AIValue = aiValue,
                ExpertValue = expertValue,
                IsDiscrepancy = true,
                Severity = (int)severityOnMismatch
            });
        }

        private static void AddCommentRow(List<AICodeInterviewReportDiscrepancyVM> rows, string sessionId,
            AICodeInterviewDiscrepancyFieldTypeEnum fieldType, int? questionId, int? displayOrder,
            string fieldKey, string aiValue, string expertValue)
        {
            if (string.IsNullOrWhiteSpace(aiValue) && string.IsNullOrWhiteSpace(expertValue)) return;

            rows.Add(new AICodeInterviewReportDiscrepancyVM
            {
                SessionId = sessionId,
                FieldType = fieldType.ToString(),
                QuestionId = questionId,
                DisplayOrder = displayOrder,
                FieldKey = fieldKey,
                AIValue = aiValue,
                ExpertValue = expertValue,
                IsDiscrepancy = !TextEquals(aiValue, expertValue),
                Severity = (int)AICodeInterviewDiscrepancySeverityEnum.None
            });
        }

        private static void AddRubricRows(List<AICodeInterviewReportDiscrepancyVM> rows, string sessionId,
            AICodeInterviewDiscrepancyFieldTypeEnum fieldType, int? questionId, int? displayOrder,
            List<AICodeInterviewRubricRatingVM> aiRatings, List<AICodeInterviewRubricRatingVM> expertRatings)
        {
            var ai = aiRatings ?? new List<AICodeInterviewRubricRatingVM>();
            var expert = expertRatings ?? new List<AICodeInterviewRubricRatingVM>();

            var dimensions = ai.Select(r => r.Dimension)
                .Concat(expert.Select(r => r.Dimension))
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var dimension in dimensions)
            {
                var aiRating = ai.FirstOrDefault(r => string.Equals(r.Dimension, dimension, StringComparison.OrdinalIgnoreCase));
                var expertRating = expert.FirstOrDefault(r => string.Equals(r.Dimension, dimension, StringComparison.OrdinalIgnoreCase));

                AddRatingRow(rows, sessionId, fieldType, questionId, displayOrder, dimension,
                    aiRating?.Rating, expertRating?.Rating);
            }
        }

        private static bool TextEquals(string left, string right)
        {
            var a = string.IsNullOrWhiteSpace(left) ? string.Empty : left.Trim();
            var b = string.IsNullOrWhiteSpace(right) ? string.Empty : right.Trim();
            return string.Equals(a, b, StringComparison.Ordinal);
        }

        private static string FormatRating(decimal? value)
            => value?.ToString("0.##", CultureInfo.InvariantCulture);

        #endregion

        #region seeding the AI baseline

        /// <summary>
        /// Builds the AI copy of the report from the assistant's stored evaluation. The recorded
        /// answers are the spine: they define how many question rows exist and supply the transcript
        /// text, with the assistant's rating for each position layered on top.
        /// </summary>
        private async Task<AICodeInterviewReportVM> BuildAIReportFromEvaluation(Guid sessionId, AICodeInterviewAuditReportVM audit)
        {
            var evaluation = await _aiInterviewerService.GetParsedAIInterviewEvaluationBySessionId(sessionId);
            var videos = await _aiInterviewerService.GetBotInterviewVideoDetails(sessionId);
            if (evaluation == null && (videos == null || videos.Count == 0)) return null;

            var meeting = await _aiInterviewerService.GetAIInterviewDetailsBySessionId(sessionId);
            var channelSkills = ParseChannelRubric(meeting?.TechSkillsInJson);
            var questionSkills = ParseQuestionMappedSkills(meeting?.QuestionAnswerSetInJson);

            var report = new AICodeInterviewReportVM
            {
                SessionId = sessionId.ToString(),
                InterviewId = audit?.InterviewId ?? 0,
                ReportSource = (int)AICodeInterviewReportSourceEnum.AI,
                OverallVerdict = AIInterviewVerdict.Normalize(evaluation?.FinalVerdict),
                OverallComment = evaluation?.FinalComment,
                OverallCommunicationRating = ToRating(evaluation?.OverallCommunicationScore),
                OverallCommunicationComment = evaluation?.OverallCommunicationScoreReasoning,
                RubricRatings = BuildReportRubricRatings(evaluation, channelSkills),
                QuestionRatings = new List<AICodeInterviewReportQuestionRatingVM>()
            };
            report.RubricRatingsJson = SerializeRubricRatings(report.RubricRatings);

            var aiQuestionRatings = evaluation?.QuestionRatings ?? new List<QuestionRating>();
            var answers = videos ?? new List<BotInterviewQuestionVideos>();

            for (var index = 0; index < answers.Count; index++)
            {
                var answer = answers[index];
                var rating = index < aiQuestionRatings.Count ? aiQuestionRatings[index] : null;

                report.QuestionRatings.Add(new AICodeInterviewReportQuestionRatingVM
                {
                    QuestionId = answer.DeliveryHeadQuestionId == 0 ? (int?)null : answer.DeliveryHeadQuestionId,
                    BotInterviewQuestionVideoId = answer.id,
                    DisplayOrder = index,
                    QuestionText = string.IsNullOrWhiteSpace(answer.question) ? rating?.Question : answer.question,
                    Rating = ToRating(rating?.Score),
                    Verdict = rating?.Verdict,
                    Comment = rating?.Comment,
                    CommunicationRating = ToRating(rating?.CommunicationScore),
                    CommunicationComment = rating?.CommunicationComment,
                    TranscriptText = answer.CandidateAnswer,
                    RubricRatings = new List<AICodeInterviewRubricRatingVM>()
                });
            }

            ApplyQuestionMaxes(report, channelSkills, questionSkills);

            return report;
        }

        /// <summary>
        /// Report-level dimensions: the HM / Delivery Manager score rubric is the spine (name +
        /// weightage as max). AI <see cref="RubricScoreSummary"/> supplies actuals/observations.
        /// When the channel has no skills, fall back to the assistant's rubric summary.
        /// </summary>
        private static List<AICodeInterviewRubricRatingVM> BuildReportRubricRatings(
            AIInterviewEvaluation evaluation, List<ChannelRubricSkill> channelSkills)
        {
            var aiByName = (evaluation?.RubricScoreSummary ?? new List<RubricScoreSummary>())
                .Where(r => !string.IsNullOrWhiteSpace(r.RubricItem))
                .GroupBy(r => r.RubricItem.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            if (channelSkills != null && channelSkills.Count > 0)
            {
                var fromChannel = new List<AICodeInterviewRubricRatingVM>();
                foreach (var skill in channelSkills)
                {
                    aiByName.TryGetValue(skill.SkillName, out var ai);
                    fromChannel.Add(new AICodeInterviewRubricRatingVM
                    {
                        Dimension = skill.SkillName,
                        Rating = ToRating(ai?.Score),
                        ExpectedLevel = FirstNonEmpty(ai?.ExpectedLevel, skill.ExpectedLevel),
                        ObservedLevel = ai?.ObservedLevel,
                        IsTechSkill = true,
                        MaxScore = skill.MaxScore
                    });
                }
                return fromChannel;
            }

            var ratings = new List<AICodeInterviewRubricRatingVM>();
            foreach (var rubric in evaluation?.RubricScoreSummary ?? new List<RubricScoreSummary>())
            {
                if (string.IsNullOrWhiteSpace(rubric.RubricItem)) continue;

                ratings.Add(new AICodeInterviewRubricRatingVM
                {
                    Dimension = rubric.RubricItem.Trim(),
                    Rating = ToRating(rubric.Score),
                    ExpectedLevel = rubric.ExpectedLevel,
                    ObservedLevel = rubric.ObservedLevel,
                    IsTechSkill = false,
                    MaxScore = rubric.MaxScore.HasValue && rubric.MaxScore.Value > 0
                        ? ToRating(rubric.MaxScore.Value)
                        : TryParseMaxScore(rubric.ExpectedLevel)
                });
            }

            if (evaluation != null)
            {
                ratings.Add(new AICodeInterviewRubricRatingVM
                {
                    Dimension = OverallTechnicalScoreDimension,
                    Rating = ToRating(evaluation.OverallTechnicalScore),
                    IsTechSkill = true
                });
            }

            return ratings;
        }

        private static void ApplyChannelRubric(AICodeInterviewReportVM report, List<ChannelRubricSkill> channelSkills)
        {
            if (report == null || channelSkills == null || channelSkills.Count == 0) return;

            var existing = report.RubricRatings ?? new List<AICodeInterviewRubricRatingVM>();
            var aligned = new List<AICodeInterviewRubricRatingVM>();

            foreach (var skill in channelSkills)
            {
                var match = existing.FirstOrDefault(r =>
                    string.Equals(r.Dimension, skill.SkillName, StringComparison.OrdinalIgnoreCase));
                aligned.Add(new AICodeInterviewRubricRatingVM
                {
                    Dimension = skill.SkillName,
                    Rating = match?.Rating,
                    ExpectedLevel = FirstNonEmpty(match?.ExpectedLevel, skill.ExpectedLevel),
                    ObservedLevel = match?.ObservedLevel,
                    IsTechSkill = true,
                    MaxScore = skill.MaxScore
                });
            }

            report.RubricRatings = aligned;
        }

        private static void ApplyQuestionMaxes(AICodeInterviewReportVM report,
            List<ChannelRubricSkill> channelSkills, List<QuestionMappedSkill> questionSkills)
        {
            if (report?.QuestionRatings == null) return;

            var skills = channelSkills ?? new List<ChannelRubricSkill>();
            var mapped = questionSkills ?? new List<QuestionMappedSkill>();

            foreach (var row in report.QuestionRatings)
            {
                var skillName = mapped
                    .FirstOrDefault(m => m.DisplayOrder == row.DisplayOrder)?.SkillName;
                if (string.IsNullOrWhiteSpace(skillName) && !string.IsNullOrWhiteSpace(row.QuestionText))
                {
                    skillName = mapped.FirstOrDefault(m =>
                        string.Equals(m.QuestionText, row.QuestionText, StringComparison.OrdinalIgnoreCase))
                        ?.SkillName;
                }

                ChannelRubricSkill skill = null;
                if (!string.IsNullOrWhiteSpace(skillName))
                {
                    skill = skills.FirstOrDefault(s =>
                        string.Equals(s.SkillName, skillName, StringComparison.OrdinalIgnoreCase));
                }

                row.MaxScore = skill?.MaxScore;
            }
        }

        private static void AlignExpertRubricToBaseline(AICodeInterviewReportVM aiReport, AICodeInterviewReportVM expertReport)
        {
            var baseline = aiReport.RubricRatings ?? new List<AICodeInterviewRubricRatingVM>();
            var posted = expertReport.RubricRatings ?? new List<AICodeInterviewRubricRatingVM>();
            if (posted.Count == 0) return;

            var aligned = new List<AICodeInterviewRubricRatingVM>();

            foreach (var baseRow in baseline)
            {
                var postedRow = posted.FirstOrDefault(r =>
                    string.Equals(r.Dimension, baseRow.Dimension, StringComparison.OrdinalIgnoreCase));
                if (postedRow == null)
                {
                    aligned.Add(new AICodeInterviewRubricRatingVM
                    {
                        Dimension = baseRow.Dimension,
                        Rating = baseRow.Rating,
                        ExpectedLevel = baseRow.ExpectedLevel,
                        ObservedLevel = baseRow.ObservedLevel,
                        IsTechSkill = baseRow.IsTechSkill,
                        MaxScore = baseRow.MaxScore
                    });
                    continue;
                }

                postedRow.Dimension = baseRow.Dimension;
                postedRow.IsTechSkill = baseRow.IsTechSkill;
                postedRow.MaxScore = baseRow.MaxScore;
                postedRow.ExpectedLevel = baseRow.ExpectedLevel;
                aligned.Add(postedRow);
            }

            expertReport.RubricRatings = aligned;
        }

        private sealed class ChannelRubricSkill
        {
            public string SkillName { get; set; }
            public decimal? MaxScore { get; set; }
            public string ExpectedLevel { get; set; }
        }

        private sealed class QuestionMappedSkill
        {
            public int DisplayOrder { get; set; }
            public string SkillName { get; set; }
            public string QuestionText { get; set; }
        }

        private static List<ChannelRubricSkill> ParseChannelRubric(string techSkillsInJson)
        {
            var list = new List<ChannelRubricSkill>();
            if (string.IsNullOrWhiteSpace(techSkillsInJson)) return list;

            try
            {
                var parsed = JsonConvert.DeserializeObject<JArray>(techSkillsInJson);
                foreach (var item in parsed ?? new JArray())
                {
                    var name = (item?["SkillName"] ?? item?["skillName"] ?? item?["Skill"])?.ToString();
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    decimal? max = null;
                    var weightToken = item?["Weightage"] ?? item?["weightage"] ?? item?["Weight"];
                    if (weightToken != null
                        && decimal.TryParse(weightToken.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var weight)
                        && weight > 0)
                    {
                        max = Math.Round(weight, 2);
                    }

                    var expected = (item?["LevelName"] ?? item?["levelName"])?.ToString();
                    if (string.IsNullOrWhiteSpace(expected) && item?["LevelId"] != null)
                        expected = item["LevelId"].ToString();

                    list.Add(new ChannelRubricSkill
                    {
                        SkillName = name.Trim(),
                        MaxScore = max,
                        ExpectedLevel = string.IsNullOrWhiteSpace(expected) ? null : expected.Trim()
                    });
                }
            }
            catch (JsonException)
            {
                return list;
            }

            return list;
        }

        private static List<QuestionMappedSkill> ParseQuestionMappedSkills(string questionAnswerSetInJson)
        {
            var list = new List<QuestionMappedSkill>();
            if (string.IsNullOrWhiteSpace(questionAnswerSetInJson)) return list;

            try
            {
                var parsed = JsonConvert.DeserializeObject<JArray>(questionAnswerSetInJson);
                var index = 0;
                foreach (var item in parsed ?? new JArray())
                {
                    var skill = (item?["mapped_rubric_parameters"] ?? item?["SkillName"] ?? item?["skillName"])?.ToString();
                    var question = (item?["question"] ?? item?["Question"])?.ToString();
                    list.Add(new QuestionMappedSkill
                    {
                        DisplayOrder = index,
                        SkillName = string.IsNullOrWhiteSpace(skill) ? null : skill.Trim(),
                        QuestionText = string.IsNullOrWhiteSpace(question) ? null : question.Trim()
                    });
                    index += 1;
                }
            }
            catch (JsonException)
            {
                return list;
            }

            return list;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            }
            return null;
        }

        #endregion

        #region rubric json plumbing

        /// <summary>
        /// Copies the identity of each question row from the AI baseline onto the matching posted
        /// row, and drops posted rows that have no counterpart.
        /// </summary>
        private static void AlignExpertRowsToBaseline(AICodeInterviewReportVM aiReport, AICodeInterviewReportVM expertReport)
        {
            var baseline = aiReport.QuestionRatings ?? new List<AICodeInterviewReportQuestionRatingVM>();
            var posted = expertReport.QuestionRatings ?? new List<AICodeInterviewReportQuestionRatingVM>();
            var aligned = new List<AICodeInterviewReportQuestionRatingVM>();

            foreach (var baseRow in baseline.OrderBy(r => r.DisplayOrder))
            {
                var postedRow = posted.FirstOrDefault(r => r.DisplayOrder == baseRow.DisplayOrder);
                if (postedRow == null) continue;

                postedRow.QuestionId = baseRow.QuestionId;
                postedRow.BotInterviewQuestionVideoId = baseRow.BotInterviewQuestionVideoId;
                postedRow.DisplayOrder = baseRow.DisplayOrder;
                if (string.IsNullOrWhiteSpace(postedRow.QuestionText))
                {
                    postedRow.QuestionText = baseRow.QuestionText;
                }

                aligned.Add(postedRow);
            }

            expertReport.QuestionRatings = aligned;
        }

        private static void HydrateRubricRatings(AICodeInterviewReportVM report)
        {
            if (report == null) return;

            report.RubricRatings = DeserializeRubricRatings(report.RubricRatingsJson);
            foreach (var row in report.QuestionRatings ?? new List<AICodeInterviewReportQuestionRatingVM>())
            {
                row.RubricRatings = DeserializeRubricRatings(row.RubricRatingsJson);
            }
        }

        private static List<AICodeInterviewRubricRatingVM> DeserializeRubricRatings(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<AICodeInterviewRubricRatingVM>();

            try
            {
                return JsonConvert.DeserializeObject<List<AICodeInterviewRubricRatingVM>>(json)
                       ?? new List<AICodeInterviewRubricRatingVM>();
            }
            catch (JsonException)
            {
                return new List<AICodeInterviewRubricRatingVM>();
            }
        }

        private static string SerializeRubricRatings(List<AICodeInterviewRubricRatingVM> ratings)
        {
            var cleaned = (ratings ?? new List<AICodeInterviewRubricRatingVM>())
                .Where(r => !string.IsNullOrWhiteSpace(r.Dimension))
                .ToList();

            return cleaned.Count == 0 ? null : JsonConvert.SerializeObject(cleaned);
        }

        private static decimal? ToRating(float? value)
            => value.HasValue ? Math.Round((decimal)value.Value, 2) : (decimal?)null;

        private static decimal? ToRating(float value)
            => Math.Round((decimal)value, 2);

        private static decimal? TryParseMaxScore(string expectedLevel)
        {
            if (string.IsNullOrWhiteSpace(expectedLevel)) return null;
            if (decimal.TryParse(expectedLevel.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var max)
                && max > 0)
            {
                return Math.Round(max, 2);
            }
            return null;
        }

        #endregion
    }
}
