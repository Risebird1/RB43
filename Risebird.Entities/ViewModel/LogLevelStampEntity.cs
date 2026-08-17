using Microsoft.WindowsAzure.Storage.Table;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Lightweight Azure Table entity used only for day/level count scans.
    /// </summary>
    public class LogLevelStampEntity : TableEntity
    {
        public int LogeLevel { get; set; }
    }
}
