namespace Risebird.Entities.Data
{
    public class ProjectFunnel
    {
        public int id { get; set; } //IRF Id
        public int IRFId { get; set; } //IRF Id
        public string Name { get; set; }// IRFName 
        public string IRFName { get; set; }// IRFName 
        //public double InvitesRate { get; set; }
        //public double ConfirmationRate { get; set; }
        //public double CompletionRate { get; set; }
        //public double RisebirdSelectionRate { get; set; }
        //public double ApprovalRate { get; set; }
        //public double FinalSelectionRate { get; set; }
        //public double OfferRate { get; set; }
        //public double JoiningRate { get; set; }
        public double WasteRate { get; set; }
        public int TotalIRFs { get; set; }
        public int TotalCandidates { get; set; }
        public int TotalInvites { get; set; }
        public int TotalConfirmations { get; set; }
        public int TotalCompleted { get; set; }
        public int TotalRisebirdSelection { get; set; }
        public int TotalApproved { get; set; }
        public int TotalFinalSelections { get; set; }
        public int TotalOffered { get; set; }
        public int TotalJoined { get; set; }
        public int TotalWaste { get; set; }
    }
}
