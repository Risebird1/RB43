using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IAzureStorageService
    {
        Task<bool> SaveAzurStorageDetails(AzureStorageInfoModelVM model);
    }
}
