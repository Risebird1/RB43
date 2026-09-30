using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>Provisioner-side state of one candidate machine (CodeProjectVmProvisioning row).</summary>
    public class CodeProjectVm
    {
        public string Handle { get; set; }
        public string SessionId { get; set; }
        public int QuestionId { get; set; }
        public int? VMPropertyId { get; set; }
        public string ResourceGroupName { get; set; }
        public string Location { get; set; }
        public string VmName { get; set; }
        public string NicName { get; set; }
        public string StackImageCode { get; set; }
        public string GalleryImageId { get; set; }
        public string OsType { get; set; }
        /// <summary>See <see cref="CodeProjectVmStep"/>.</summary>
        public string Step { get; set; }
        public string Message { get; set; }
        /// <summary>Short-lived SAS of the candidate zip; cleared once setup has run.</summary>
        public string ProjectZipUrl { get; set; }
        public string ReadOnlyPathsJson { get; set; }
        public string CandidateUserName { get; set; }
        public string CandidatePasswordProtected { get; set; }
        public string AdminPasswordProtected { get; set; }
        public string ConnectUrl { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? ReadyAtUtc { get; set; }
        public DateTime? ReleasedAtUtc { get; set; }
    }

    /// <summary>
    /// A machine is built in steps, each an Azure long-running operation:
    /// requested → nic → vm → setup (Run Command) → link (Bastion) → ready.
    /// </summary>
    public static class CodeProjectVmStep
    {
        public const string Requested = "requested";
        public const string Nic = "nic";
        public const string Vm = "vm";
        public const string Setup = "setup";
        public const string Link = "link";
        public const string Ready = "ready";
        public const string Failed = "failed";
        public const string Released = "released";

        public static bool IsTerminal(string step) => step == Ready || step == Failed || step == Released;
    }
}
