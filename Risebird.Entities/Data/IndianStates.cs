using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Entities.Data
{
    /// <summary>Indian states and union territories, for address dropdowns and validation.</summary>
    public static class IndianStates
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            // States
            "Andhra Pradesh", "Arunachal Pradesh", "Assam", "Bihar", "Chhattisgarh", "Goa", "Gujarat", "Haryana",
            "Himachal Pradesh", "Jharkhand", "Karnataka", "Kerala", "Madhya Pradesh", "Maharashtra", "Manipur",
            "Meghalaya", "Mizoram", "Nagaland", "Odisha", "Punjab", "Rajasthan", "Sikkim", "Tamil Nadu", "Telangana",
            "Tripura", "Uttar Pradesh", "Uttarakhand", "West Bengal",
            // Union territories
            "Andaman and Nicobar Islands", "Chandigarh", "Dadra and Nagar Haveli and Daman and Diu", "Delhi",
            "Jammu and Kashmir", "Ladakh", "Lakshadweep", "Puducherry"
        };

        public static bool IsValid(string state) =>
            !string.IsNullOrWhiteSpace(state) && All.Contains(state.Trim(), StringComparer.OrdinalIgnoreCase);

        /// <summary>The list's own spelling of a state (case-insensitive match), or null.</summary>
        public static string Normalize(string state) =>
            All.FirstOrDefault(s => string.Equals(s, state?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
