using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewPlanner
    {

        public string IRFName { get; set; }
        public int IRFId { get; set; }
        public int Overall { get; set; }
        public int TotalInvites { get; set; }
        public int ConfirmationTotal { get; set; }
        public int CompletionTotal { get; set; }
        public int RisebirdSelectionTotal { get; set; }
        public int ApprovalTotal { get; set; }
        public int FinalSelectionTotal { get; set; }
        public int OfferAcceptanceTotal { get; set; }
        public int JoiningTotal { get; set; }
        public double InvitePercentage { get; set; }
        public double ConfirmationPercentage { get; set; }
        public double CompletionPercentage { get; set; }
        public double RisebirdSelectionPercentage { get; set; }
        public double ApprovalPercentage { get; set; }
        public double FinalSelectionPercentage { get; set; }
        public double OfferAcceptancePercentage { get; set; }
        public double JoiningPercentage { get; set; }
        public double OverallPercentage { get; set; }
    }
}
