using System;
using System.Collections.Generic;
using System.Text;

namespace Risebird.Entities.ViewModel
{
    public class InterviewPlanner
    {

        public string IRFName { get; set; }
        public string InviteOwnedBy { get; set; }
        public string ConfirmationOwnedBy { get; set; }
        public string CompletionOwnedBy { get; set; }
        public string RisebirdSelectionOwnedBy { get; set; }
        public string ApprovalOwnedBy { get; set; }
        public string FinalSelectionOwnedBy { get; set; }
        public string OfferOwnedBy { get; set; }
        public string JoiningOwnedBy { get; set; }
        public DateTime IRFCreatedOn { get; set; }
        public int IRFId { get; set; }
        public int OverAllProcessingTime { get; set; }

        public int InviteProcessingTime { get; set; }
        public int ConfirmationProcessingTime { get; set; }

        public int CompletionProcessingTime { get; set; }
        public int RisebirdSelectionProcessingTime { get; set; }
        public int ApprovalProcessingTime { get; set; }
        public int FinalSelectionProcessingTime { get; set; }
        public int OfferProcessingTime { get; set; }
        public int JoiningProcessingTime { get; set; }
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
