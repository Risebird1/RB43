using Risebird.Entities.Data;
using System;
using System.Collections.Generic;
using System.Text;

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
