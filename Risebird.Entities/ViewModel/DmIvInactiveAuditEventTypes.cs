namespace Risebird.Entities.ViewModel
{
    /// <summary>Event types stored in TransInterviewerAuditLog for DM inactive-reason resolution.</summary>
    public static class DmIvInactiveAuditEventTypes
    {
        public const string RbVetFlippedToNo = "RbVetFlippedToNo";
        public const string RbVetFlippedToYes = "RbVetFlippedToYes";
        public const string ClientVetFlippedToNo = "ClientVetFlippedToNo";
        public const string ClientVetFlippedToYes = "ClientVetFlippedToYes";
        public const string AutoQcRemoval = "AutoQcRemoval";
        /// <summary>Bad-quality flag set (no deactivation) — forward trigger or per-IRF backfill.</summary>
        public const string AutoQcFlag = "AutoQcFlag";
        /// <summary>Manual removal from IRF Audit risk breakdown (ops-initiated).</summary>
        public const string RiskAuditRemoval = "RiskAuditRemoval";
    }
}
