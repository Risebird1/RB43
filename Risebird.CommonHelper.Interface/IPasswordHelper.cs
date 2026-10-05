using Risebird.Entities.Data;
using System.Threading.Tasks;

namespace Risebird.CommonHelper.Interface
{
    public interface IPasswordHelper
    {
        /// <summary>
        /// Generates a password and provisions the user into Azure AD B2C. The result says whether a
        /// new account was created (password usable), one already existed (password NOT applied), or
        /// provisioning failed (caller must abort).
        /// </summary>
        Task<B2CProvisioningResult> GenerateUserPassword(UserDetails user, bool forcePasswordChangeNextLogin = false);
    }
}
