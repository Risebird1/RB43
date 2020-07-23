using Risebird.Entities.Data;
using System.Collections.Generic;

namespace Risebird.Business.Interface
{
    public interface IProfileService
    {
        List<Profile> GetProfile();
        bool AddProfile(Profile profile);
        Profile GetProfile(int id);
        bool UpdateProfile(Profile profile);
        bool DeleteProfile(int id);


    }
}
