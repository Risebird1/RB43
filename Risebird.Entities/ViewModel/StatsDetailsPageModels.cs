using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Risebird.Entities.ViewModel
{
    public class StatsDetailsApiResponse
    {
        public bool ShowEvidenceColumn { get; set; }
        public IEnumerable<StatsDetailsClientRowDto> Rows { get; set; } = Array.Empty<StatsDetailsClientRowDto>();

        public static StatsDetailsApiResponse ForClient(IEnumerable<StatsDetailsVM> source)
        {
            return new StatsDetailsApiResponse
            {
                ShowEvidenceColumn = false,
                Rows = (source ?? Enumerable.Empty<StatsDetailsVM>()).Select(MapClientRow).ToList()
            };
        }

        public static StatsDetailsApiResponse ForInternal(IEnumerable<StatsDetailsVM> source)
        {
            return new StatsDetailsApiResponse
            {
                ShowEvidenceColumn = true,
                Rows = (source ?? Enumerable.Empty<StatsDetailsVM>()).Select(MapInternalRow).ToList()
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

        private static StatsDetailsInternalRowDto MapInternalRow(StatsDetailsVM x)
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
                IrfId = x.irfId,
                Evidence = x.interviewId.HasValue
                    ? new StatsDetailsEvidenceMeta
                    {
                        InterviewId = x.interviewId.Value,
                        IrfId = x.irfId
                    }
                    : null
            };
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
    }

    public class StatsDetailsEvidenceMeta
    {
        public int InterviewId { get; set; }
        public int IrfId { get; set; }
    }
}
