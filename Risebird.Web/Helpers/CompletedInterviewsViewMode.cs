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

        public static StatsDetailsApiResponse BuildApiResponse(
            IEnumerable<StatsDetailsVM> rows,
            bool showEvidenceColumn)
        {
            if (showEvidenceColumn)
                return StatsDetailsApiResponse.ForInternal(rows);

            return StatsDetailsApiResponse.ForClient(rows);
        }
    }
}
