namespace Risebird.Entities.Data
{
    /// <summary>
    /// Entity for table [dbo].[QcInterviewAIevaluationDetails]. One row per red-flag (parameter name + verdict).
    /// </summary>
    public class QcInterviewAIevaluationDetail
    {
        public int id { get; set; }
        public int? RedmarkedQCId { get; set; }
        public int? Interviewid { get; set; }
        public string FlagName { get; set; }
        public string verdict { get; set; }
        public string Remark { get; set; }
        public string SourceType { get; set; }
    }
}
