using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Risebird.CommonHelper;
using Risebird.Entities.Data;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Helpers
{
    /// <summary>
    /// Centralizes Delivery Head Instruction view mode: internal/DM, client share (Type=ublc), and SME.
    /// </summary>
    public static class DeliveryHeadInstructionViewMode
    {
        public const string ClientShareTypeParam = "ublc";
        public const string ClientShareSessionKey = "DHI_ClientShareIrfId";
        public const string SmeSessionKey = "DHI_SmeIrfId";

        public static bool IsClientShareType(string type) =>
            string.Equals(type, ClientShareTypeParam, StringComparison.OrdinalIgnoreCase);

        public static bool IsClientShareType(HttpContext context) =>
            context != null && IsClientShareType(context.Request.Query["Type"]);

        public static bool HasClientShareSession(HttpContext context)
        {
            if (context?.Session == null) return false;
            return !string.IsNullOrEmpty(context.Session.GetString(ClientShareSessionKey));
        }

        public static bool IsClientShareForIrf(HttpContext context, int irfId)
        {
            if (context == null || irfId <= 0) return false;
            if (IsClientShareType(context)) return true;
            if (context.Session == null) return false;
            var sessionIrf = context.Session.GetString(ClientShareSessionKey);
            return int.TryParse(sessionIrf, out var id) && id == irfId;
        }

        public static void SetClientShareSession(HttpContext context, int irfId)
        {
            if (context?.Session == null || irfId <= 0) return;
            context.Session.SetString(ClientShareSessionKey, irfId.ToString());
        }

        public static bool IsSmeViewForIrf(HttpContext context, int irfId)
        {
            if (context == null || irfId <= 0 || context.Session == null) return false;
            var sessionIrf = context.Session.GetString(SmeSessionKey);
            return int.TryParse(sessionIrf, out var id) && id == irfId;
        }

        public static void SetSmeSession(HttpContext context, int irfId)
        {
            if (context?.Session == null || irfId <= 0) return;
            context.Session.SetString(SmeSessionKey, irfId.ToString());
            CandidateReportMaskingHelper.ActivateMaskContext(context.Session);
        }

        public static bool IsClientRoleProfile(string profileName)
        {
            var p = (profileName ?? string.Empty).Trim().Trim('"');
            return p.Equals("Public", StringComparison.OrdinalIgnoreCase)
                || p.Equals("Client", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsOperationalProfile(string profileName)
        {
            var p = (profileName ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrEmpty(p)) return false;
            if (IsClientRoleProfile(p)) return false;
            if (p.Equals("Interviewer", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        public static bool WantsClientShareEntry(HttpContext context, string type) =>
            IsClientShareType(type) || HasClientShareSession(context) || IsClientShareType(context);

        public static string ExtractClientVisibleAiReasoning(string openAiResponse)
        {
            if (string.IsNullOrWhiteSpace(openAiResponse)) return null;
            var text = openAiResponse.Trim();
            if (text.StartsWith("{", StringComparison.Ordinal))
            {
                try
                {
                    var parsed = JsonConvert.DeserializeObject<InterviewerResumeFitmentAnalysisSchema>(text);
                    if (!string.IsNullOrWhiteSpace(parsed?.completeFitmentAnalysis))
                        return parsed.completeFitmentAnalysis.Trim();
                }
                catch
                {
                    // fall through to raw text
                }
            }

            return text;
        }

        public static void ApplyClientViewFilter(IRFVm model)
        {
            if (model == null) return;
            model.InterviewerCollection = model.InterviewerCollection?
                .Where(iv => iv != null && iv.IsActive)
                .Select(StripInterviewerForClientView)
                .ToList() ?? new List<TransInterviewer>();
        }

        public static void ApplySmeViewFilter(IRFVm model)
        {
            if (model == null) return;
            model.InterviewerCollection = model.InterviewerCollection?
                .Where(iv => iv != null && iv.IsActive)
                .Select(StripInterviewerForSmeView)
                .ToList() ?? new List<TransInterviewer>();
        }

        public static TransInterviewer StripInterviewerForClientView(TransInterviewer iv)
        {
            if (iv == null) return iv;

            var reasoning = ExtractClientVisibleAiReasoning(iv.OpenAIScreeningResponse);

            iv.EmailId = null;
            iv.PerSuccessRate = 0;
            iv.Surcharge = 0;
            iv.VideoLink = null;
            iv.IsVideoApproved = false;
            iv.ResumeLink = null;
            iv.ConvertedFromDocResumeLink = null;
            iv.RedMarkerReviewComment = null;
            iv.ChannelOwner = null;
            iv.ClientHr = null;
            iv.InterviewerProfileAddedBy = null;
            iv.CompanyName = null;
            iv.EmploymentComment = null;
            iv.LastLoginAt = null;
            iv.LastInterviewConductedAt = null;
            iv.TotalCompletedMeetings = 0;
            iv.TotalSelected = 0;
            iv.TotalRejected = 0;
            iv.TotalFuture = 0;
            iv.InterviewCapacity = null;
            iv.PrimarySkill = null;
            iv.TotalInterview = 0;
            iv.ModifiedAt = null;
            iv.OpenAIScreeningResponse = reasoning;

            return iv;
        }

        public static TransInterviewer StripInterviewerForSmeView(TransInterviewer iv)
        {
            if (iv == null) return iv;
            iv.EmailId = null;
            iv.PerSuccessRate = 0;
            iv.Surcharge = 0;
            iv.ClientAction = null;
            iv.ChannelOwner = null;
            iv.ClientHr = null;
            iv.InterviewerProfileAddedBy = null;
            iv.RedMarkerReviewComment = null;
            return iv;
        }

        public static object BuildClientPanelMetricsEntry(TransInterviewer iv)
        {
            if (iv == null) return null;
            return new
            {
                userId = iv.UserId,
                nickName = iv.NickName ?? string.Empty,
                codedCvLink = iv.CodedResumeLink ?? string.Empty,
                cvCode = !string.IsNullOrEmpty(iv.CodedResumeLink) ? (iv.NickName ?? "CV") : string.Empty,
                aiVerdict = iv.AIVerdict ?? string.Empty,
                aiScore = iv.AIScreeningScore,
                aiReasoning = ExtractClientVisibleAiReasoning(iv.OpenAIScreeningResponse) ?? string.Empty,
                clientAction = iv.ClientAction ?? string.Empty,
                clientVetEnabled = iv.IsSMEVerified || iv.IsSMEVerifiedStored == true
            };
        }

        public static object BuildClientCvStatePayload(TransInterviewer iv)
        {
            if (iv == null) return new { ok = false };
            return new
            {
                ok = true,
                codedCvLink = iv.CodedResumeLink ?? string.Empty,
                cvCode = !string.IsNullOrEmpty(iv.CodedResumeLink) ? (iv.NickName ?? "CV") : string.Empty,
                aiVerdict = iv.AIVerdict ?? string.Empty,
                aiScore = iv.AIScreeningScore,
                aiReasoning = ExtractClientVisibleAiReasoning(iv.OpenAIScreeningResponse) ?? string.Empty
            };
        }

        public static void StripInterviewerInsightsForClientView(InterviewerInsightsVM vm)
        {
            if (vm == null) return;
            vm.Emailid = null;
            vm.interviewerName = null;
            vm.MobileNumber = null;
            vm.ResumeLink = null;
            vm.PaymentSLA = 0;
            vm.Penalty = 0;
        }

        public static void StripInterviewerInsightsForSmeView(InterviewerInsightsVM vm)
        {
            StripInterviewerInsightsForClientView(vm);
        }
    }
}
