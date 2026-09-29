using Risebird.Entities.Storage;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Lightweight Azure Table entity used only for day/level count scans.
    /// </summary>
    public class LogLevelStampEntity : RbTableEntity
    {
        public int LogeLevel { get; set; }
    }
}
