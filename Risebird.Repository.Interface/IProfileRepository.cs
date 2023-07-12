using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Repository.Interface
{
    public interface IProfileRepository
    {
        Task<IEnumerable<Profile>> GetProfiles();
        Task<bool> AddProfile(Profile profile);
        Task<Profile> GetProfile(int id);
        Task<bool> UpdateProfile(Profile entity);
        Task<bool> DeleteProfile(int id);


    }
}
