using Risebird.Entities.ViewModel;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IAzureStorageRepository
    {
        Task<bool> SaveAzurStorageDetails(AzureStorageInfoModelVM model);
    }
}
