using Microsoft.AspNetCore.Http;
using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.CommonHelper
{
    /// <summary>
    /// Context-aware candidate field masking for HM/DM Approval drilldown → stats → interview report.
    /// </summary>
    public static class CandidateReportMaskingHelper
    {
        public const string SessionKey = "DmApprovalMaskCandidateContext";

        public static void ActivateMaskContext(ISession session)
        {
            if (session == null)
            {
                return;
            }

            session.SetString(SessionKey, "1");
        }

        public static bool IsMaskCandidateParam(string maskCandidate)
        {
            if (string.IsNullOrWhiteSpace(maskCandidate))
            {
                return false;
            }

            var value = maskCandidate.Trim();
            return value == "1"
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Mask when explicit param is set, or when drilldown session + stats-page referrer (URL param removal cannot bypass).
        /// </summary>
        public static bool ShouldMaskCandidate(HttpRequest request, ISession session, string maskCandidate)
        {
            if (IsMaskCandidateParam(maskCandidate))
            {
                return true;
            }

            if (session?.GetString(SessionKey) != "1")
            {
                return false;
            }

            var referer = request?.Headers["Referer"].ToString();
            if (string.IsNullOrWhiteSpace(referer))
            {
                return false;
            }

            return referer.IndexOf("InterviewerStatsDetails", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static async Task ApplyMaskingAsync(InterviewReport model, ICandidateService candidateService, string createdBy = null)
        {
            if (model == null)
            {
                return;
            }

            string codedId = null;
            var email = model.CandidateEmail?.Trim();
            if (!string.IsNullOrWhiteSpace(email))
            {
                var pseudonyms = await candidateService.EnsureCandidatePseudonymsAsync(
                    new[] { email },
                    createdBy);

                if (pseudonyms != null)
                {
                    if (pseudonyms.TryGetValue(email, out var id))
                    {
                        codedId = id;
                    }
                    else
                    {
                        codedId = pseudonyms
                            .FirstOrDefault(kv => string.Equals(kv.Key, email, StringComparison.OrdinalIgnoreCase))
                            .Value;
                    }
                }
            }

            model.CandidateName = !string.IsNullOrWhiteSpace(codedId) ? codedId : "Candidate";
            model.CandidateEmail = null;
            model.resumeLink = null;
            model.encodedresumeLink = null;
            model.CodedResumeLink = null;
        }
    }
}
