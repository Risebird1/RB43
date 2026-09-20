using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IB2CClientService
    {
        /// <summary>
        /// Creates the user in Azure AD B2C. Returns whether the account was created, already
        /// existed, or could not be created - callers must not treat the last two as the same thing.
        /// </summary>
        Task<B2CProvisioningOutcome> CreateUser(UserList externalUser, bool forcePasswordChangeNextLogin = false);
    }
}
