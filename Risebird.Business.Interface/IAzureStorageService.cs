using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IAzureStorageService
    {
        Task<bool> SaveAzurStorageDetails(AzureStorageInfoModelVM model);
    }
}
