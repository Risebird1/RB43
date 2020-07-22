using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Business
{
    public class ProfileService : IProfileService
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        public ProfileService(IProfileRepository profileRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _profileRepository = profileRepository;
            _applicationErrorLogger = applicationErrorLogger;
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

        public Profile GetProfile(int id)
        {
            try
            {
                var profile = _profileRepository.GetProfile(id);

                if (profile == null)
                    return null;
                else
                    return profile.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool AddProfile(Profile profile)
        {
            try
            {
                return _profileRepository.AddProfile(profile).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool UpdateProfile(Profile profile)
        {
            try
            {
                return _profileRepository.UpdateProfile(profile).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public bool DeleteProfile(int id)
        {
            try
            {
                return _profileRepository.DeleteProfile(id).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
    }
}
