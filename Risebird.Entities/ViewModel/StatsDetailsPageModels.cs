using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Risebird.Entities.ViewModel
{
    public class StatsDetailsApiResponse
    {
        public bool ShowEvidenceColumn { get; set; }
        public bool ShowQcVerdictColumn { get; set; }
        public bool ShowPaymentDeductionColumn { get; set; }
        public IEnumerable<StatsDetailsClientRowDto> Rows { get; set; } = Array.Empty<StatsDetailsClientRowDto>();

        public static StatsDetailsApiResponse ForClient(IEnumerable<StatsDetailsVM> source)
        {
            return new StatsDetailsApiResponse
            {
                ShowEvidenceColumn = false,
                ShowQcVerdictColumn = false,
                ShowPaymentDeductionColumn = false,
                Rows = (source ?? Enumerable.Empty<StatsDetailsVM>()).Select(MapClientRow).ToList()
            };
        }

        public static StatsDetailsApiResponse ForInternal(
            IEnumerable<StatsDetailsVM> source,
            bool showEvidenceColumn,
            bool showQcVerdictColumn,
            bool showPaymentDeductionColumn)
        {
            return new StatsDetailsApiResponse
            {
                ShowEvidenceColumn = showEvidenceColumn,
                ShowQcVerdictColumn = showQcVerdictColumn,
                ShowPaymentDeductionColumn = showPaymentDeductionColumn,
                Rows = (source ?? Enumerable.Empty<StatsDetailsVM>())
                    .Select(x => MapInternalRow(x, showEvidenceColumn, showQcVerdictColumn, showPaymentDeductionColumn))
                    .ToList()
            };
        }

        private static StatsDetailsClientRowDto MapClientRow(StatsDetailsVM x) => new StatsDetailsClientRowDto
        {
            InterviewDate = x.interviewDate,
            CandidateCodedId = x.candidateCodedId,
            SkillChannel = x.SkillChannel,
            ProjectUniqueid = x.ProjectUniqueid,
            MeetingStatus = x.MeetingStatus,
            InterviewStatus = x.InterviewStatus,
            InterviewId = x.interviewId,
            IrfId = x.irfId
        };

        private static StatsDetailsInternalRowDto MapInternalRow(
            StatsDetailsVM x,
            bool showEvidenceColumn,
            bool showQcVerdictColumn,
            bool showPaymentDeductionColumn)
        {
            var row = new StatsDetailsInternalRowDto
            {
                InterviewDate = x.interviewDate,
                CandidateCodedId = x.candidateCodedId,
                SkillChannel = x.SkillChannel,
                ProjectUniqueid = x.ProjectUniqueid,
                MeetingStatus = x.MeetingStatus,
                InterviewStatus = x.InterviewStatus,
                InterviewId = x.interviewId,
                IrfId = x.irfId
            };

            if (showEvidenceColumn && x.interviewId.HasValue)
            {
                row.Evidence = new StatsDetailsEvidenceMeta
                {
                    InterviewId = x.interviewId.Value,
                    IrfId = x.irfId
                };
            }

            if (showQcVerdictColumn)
                row.QcVerdictDisplay = x.QcVerdictDisplay;

            if (showPaymentDeductionColumn)
                row.PaymentDeductionDisplay = x.PaymentDeductionDisplay;

            return row;
        }
    }

    public class StatsDetailsClientRowDto
    {
        public DateTime InterviewDate { get; set; }
        public string CandidateCodedId { get; set; }
        public string SkillChannel { get; set; }
        public string ProjectUniqueid { get; set; }
        public int? MeetingStatus { get; set; }
        public int? InterviewStatus { get; set; }
        public int? InterviewId { get; set; }
        public int IrfId { get; set; }
    }

    public class StatsDetailsInternalRowDto : StatsDetailsClientRowDto
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public StatsDetailsEvidenceMeta Evidence { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string QcVerdictDisplay { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public StatsDetailsPaymentDeductionDisplay PaymentDeductionDisplay { get; set; }
    }

    public class StatsDetailsEvidenceMeta
    {
        public int InterviewId { get; set; }
        public int IrfId { get; set; }
    }
}
