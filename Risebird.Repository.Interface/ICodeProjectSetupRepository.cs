using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    /// <summary>The CodeProjectSettings table: configuration fallback written by the one-click setup.</summary>
    public interface ICodeProjectSettingsRepository
    {
        /// <summary>Name → (value, isSecret). Empty when the table does not exist yet.</summary>
        Dictionary<string, (string Value, bool IsSecret)> GetAll();

        Task Set(string name, string value, bool isSecret = false);
    }

    /// <summary>State of the one-click infrastructure setup, plus the database work it performs.</summary>
    public interface ICodeProjectSetupRepository
    {
        Task<int> StartOrResume(string startedBy, string initialStateJson);

        Task<CodeProjectSetupRun> GetLatest();

        Task<bool> TryLease(int id, int leaseSeconds = 180);

        Task Save(int id, string status, string stateJson, bool releaseLease = true);

        /// <summary>Runs a migration script, splitting it on GO lines. Scripts are idempotent.</summary>
        Task ApplySqlScript(string script);

        Task UpsertStackImage(string code, string displayName, string toolchain, string galleryImageId, string osType);

        /// <summary>"server / database" of the connection, shown before anything is changed.</summary>
        string DescribeDatabase();
    }
}
