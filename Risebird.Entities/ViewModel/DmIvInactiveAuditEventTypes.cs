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
    }
}
