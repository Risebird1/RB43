using System;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Keys shared by configuration and the CodeProjectSettings table. Configuration always wins;
    /// the table is the fallback that the one-click setup fills in.
    /// </summary>
    public static class CodeProjectSettingNames
    {
        public const string AzureResourceGroup = "CodeProjects:Azure:ResourceGroup";
        public const string AzureLocation = "CodeProjects:Azure:Location";
        public const string AzureSubnetId = "CodeProjects:Azure:SubnetId";
        public const string AzureBastionName = "CodeProjects:Azure:BastionName";
        public const string AzureBastionResourceGroup = "CodeProjects:Azure:BastionResourceGroup";

        public const string RunnerResourceGroup = "CodeProjects:Runner:ResourceGroup";
        public const string RunnerJobPrefix = "CodeProjects:Runner:Job:";      // + stack code
        public const string RunnerImagePrefix = "CodeProjects:Runner:Image:";  // + stack code

        /// <summary>https://{functions-app}/api — where the web app finds the runner, provisioner and setup.</summary>
        public const string FunctionsBaseUrl = "CodeProjects:FunctionsBaseUrl";
        public const string FunctionsKey = "CodeProjects:FunctionsKey";

        public const string RunnerUrl = "CodeProjects:RunnerUrl";
        public const string RunnerKey = "CodeProjects:RunnerKey";
        public const string ProvisionerUrl = "CodeProjects:ProvisionerUrl";
        public const string ProvisionerKey = "CodeProjects:ProvisionerKey";

        /// <summary>Used when nothing else is configured and the app runs locally (func start listens on 7071).</summary>
        public const string LocalFunctionsBaseUrl = "http://localhost:7071/api";
    }

    public static class CodeProjectSetupStatus
    {
        public const string Running = "running";
        public const string Completed = "completed";
        public const string Failed = "failed";
        /// <summary>Needs a human (usually a missing permission); Start resumes it.</summary>
        public const string Blocked = "blocked";
    }

    public static class CodeProjectSetupStepStatus
    {
        public const string Pending = "pending";
        public const string Running = "running";
        public const string Done = "done";
        public const string Skipped = "skipped";
        public const string Failed = "failed";
        public const string Blocked = "blocked";
    }

    public class CodeProjectSetupRun
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public string StateJson { get; set; }
        public string StartedBy { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public class CodeProjectSetupStep
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Status { get; set; } = CodeProjectSetupStepStatus.Pending;
        public string Detail { get; set; }
        public DateTime? StartedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int Attempts { get; set; }
    }

    /// <summary>Everything the setup remembers between ticks. Serialized into CodeProjectInfraSetupRuns.StateJson.</summary>
    public class CodeProjectSetupState
    {
        public List<CodeProjectSetupStep> Steps { get; set; } = new List<CodeProjectSetupStep>();
        /// <summary>Resource ids, names and intermediate handles (image builder run ids, ACR run ids, …).</summary>
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public List<string> Log { get; set; } = new List<string>();
    }
}
