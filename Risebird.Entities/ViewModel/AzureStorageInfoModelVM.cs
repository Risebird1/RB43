using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Entities.ViewModel
{
    public class AzureStorageInfoModelVM
    {
        public List<StorageItem> BlobContainers { get; set; }
        //public List<StorageItem> Tables { get; set; }
        //public List<StorageItem> Queues { get; set; }
        //public List<StorageItem> FileShares { get; set; }
    }

    public class StorageItem
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public decimal Price { get; set; }
    }
}
