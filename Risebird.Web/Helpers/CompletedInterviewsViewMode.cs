using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Helpers
{
    /// <summary>View-mode rules for Completed Interviews / Interviewer Stats Details.</summary>
    public static class CompletedInterviewsViewMode
    {
        /// <summary>Profiles allowed to open Evidence column destinations (aligned with Payment SLA ops access).</summary>
        private static readonly HashSet<string> EvidenceAuthorizedProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Admin",
            "HR",
            "SeniorHR",
            "SupportAdmin",
            "Quality Controller",
            "InterviewerManagement",
            "SeniorInterviewerManagement",
            "Finance",
            "Finance Support",
            "Interviewer",
            "MappingAdmin"
        };

        public static bool IsEvidenceAuthorizedProfile(string profileName)
        {
            var p = (profileName ?? string.Empty).Trim().Trim('"');
            return !string.IsNullOrEmpty(p) && EvidenceAuthorizedProfiles.Contains(p);
        }

        /// <summary>True when the user has a portal session with an ops profile that can use Evidence links.</summary>
        public static bool TryAuthorizeEvidenceEndpoints(HttpContext context, out string profileName)
        {
            profileName = context?.Session?.GetString("profileName")?.Trim().Trim('"');
            var email = context?.Session?.GetString("email");
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(profileName))
                return false;
            return IsEvidenceAuthorizedProfile(profileName);
        }

        public static bool ShouldShowEvidenceColumn(HttpContext context, string clientEmailId, string usePl)
        {
            if (!string.IsNullOrEmpty(clientEmailId))
                return false;

            if (string.Equals(usePl, "Public", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!TryAuthorizeEvidenceEndpoints(context, out var profileName))
                return false;

            if (DeliveryHeadInstructionViewMode.IsClientRoleProfile(profileName))
                return false;

            return true;
        }

        public static bool ShouldShowQcVerdictColumn(HttpContext context, string clientEmailId, string usePl, int? irfId = null)
        {
            if (!string.IsNullOrEmpty(clientEmailId))
                return false;

            if (string.Equals(usePl, "Public", StringComparison.OrdinalIgnoreCase))
                return false;

            var profileName = context?.Session?.GetString("profileName")?.Trim().Trim('"');
            var email = context?.Session?.GetString("email");
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(profileName))
                return false;

            if (DeliveryHeadInstructionViewMode.IsClientRoleProfile(profileName))
                return false;

            if (DeliveryHeadInstructionViewMode.IsOperationalProfile(profileName))
                return true;

            if (string.Equals(profileName, "Interviewer", StringComparison.OrdinalIgnoreCase)
                && irfId.HasValue
                && irfId.Value > 0
                && DeliveryHeadInstructionViewMode.IsSmeViewForIrf(context, irfId.Value))
            {
                return true;
            }

            return ShouldShowEvidenceColumn(context, clientEmailId, usePl);
        }

        /// <summary>
        /// Matches existing Completed Interviews / Stats Details redo affordance:
        /// hidden only for Public share links (<c>usePl=Public</c>).
        /// Reuses GET/POST <see cref="Controllers.InterviewController.RedoInterviewDetails"/>.
        /// </summary>
        public static bool ShouldShowRedoInterviewAction(string usePl) =>
            !string.Equals(usePl, "Public", StringComparison.OrdinalIgnoreCase);

        /// <summary>Same role gate as Evidence (item 55).</summary>
        public static bool ShouldShowPaymentDeductionColumn(HttpContext context, string clientEmailId, string usePl) =>
            ShouldShowEvidenceColumn(context, clientEmailId, usePl);

        public static StatsDetailsApiResponse BuildApiResponse(
            IEnumerable<StatsDetailsVM> rows,
            bool showEvidenceColumn,
            bool showQcVerdictColumn,
            bool showPaymentDeductionColumn)
        {
            if (!showEvidenceColumn && !showQcVerdictColumn && !showPaymentDeductionColumn)
                return StatsDetailsApiResponse.ForClient(rows);

            return StatsDetailsApiResponse.ForInternal(rows, showEvidenceColumn, showQcVerdictColumn, showPaymentDeductionColumn);
        }
    }
}
