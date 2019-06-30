using System;
using Risebird.Business.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business
{
    public class ProfileService: IProfileService
    {
        private IProfileRepository _profileRepository;
        public ProfileService(IProfileRepository profileRepository)
        {
            _profileRepository = profileRepository;
        }
        public List<Profile> GetProfile()
        {
            try
            {
                return _profileRepository.GetProfiles().ToList();
            }
            catch (Exception e)
            {
                return null;
            }
            
        }
    }
}
