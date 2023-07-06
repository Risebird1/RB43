using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risebird.Business.Interface
{
    public interface IProfileService
    {
        Task<List<Profile>> GetProfile();
        bool AddProfile(Profile profile);
        Profile GetProfile(int id);
        bool UpdateProfile(Profile profile);
        bool DeleteProfile(int id);


    }
}
