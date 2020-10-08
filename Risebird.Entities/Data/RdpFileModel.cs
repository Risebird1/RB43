using System;

namespace Risebird.Entities.Data
{
    [Serializable]
    public class RdpFileModel
    {
        public RdpFileModel(string fileName, object fileContent)
        {
            FileName = fileName;
            FileContent = fileContent;
        }
        public string FileName { get; set; }
        public object FileContent { get; set; }
    }
}
