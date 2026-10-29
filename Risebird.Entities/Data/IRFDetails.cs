namespace Risebird.Entities.Data
{
    public class SkillChannelBillingRateVM
    {
        public int    IRFAutoId      { get; set; }   // IRF.id (PK)
        public string IRFBusinessKey { get; set; }   // IRF.IRFId (display key)
        public int    ClientId       { get; set; }
        public string ClientName     { get; set; }
        public string JDTitle        { get; set; }
        public string JobCountry     { get; set; }
        public string CompanyType    { get; set; }
        public string Heuristicsscore{ get; set; }
        public int    DefaultRate    { get; set; }   // Client.PerInterviewRate (always shown)
        public int    BillingRate    { get; set; }   // custom rate or DefaultRate when none set
        public bool   IsRateSet      { get; set; }   // true if a row exists in SkillChannelBillingRate
        public string BillingCurrency{ get; set; } = "INR";
        public int    TotalInterviewers  { get; set; }
        public int    ActiveInterviewers { get; set; }
    }

    public class IRFDetails
    {
        public string IRFId { get; set; }
        public string JDTiltle { get; set; }
        public string PrimarySkill { get; set; }
        public string ISCId { get; set; }
        public bool IsGeneralSkillRequired { get; set; }
        public int SkillSetsevaluate { get; set; }
        public string JDTitleTwo { get; set; }
        public int? Heuristicsscore { get; set; }
        public string TechRubricType { get; set; }
        public bool IsAIInterview { get; set; }
        public int? LevelDisplayMode { get; set; }
    }
    public class IRFDetailsList
    {
        public int id { get; set; }
        public string IRFId { get; set; }
        public string ClientName { get; set; }
        public string JDTiltle { get; set; }
        public int SkillSetsevaluate { get; set; }
        public string Heuristicsscore { get; set; }
        public string JobCountry { get; set; }
        public string CompanyType { get; set; }
        public int TotalInterviewers { get; set; }
        public int ActiveInterviewers { get; set; }
        public int InactiveInterviewers { get; set; }
    }
}
