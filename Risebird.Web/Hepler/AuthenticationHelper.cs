using System;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Threading.Tasks;

namespace Risebird.Web.Hepler
{
    public class AuthenticationHelper
    {
        public static string token;

        /// <summary>
        /// Async task to acquire token for Application.
        /// </summary>
        /// <returns>Async Token for application.</returns>
        public static async Task<string> AcquireTokenAsync()
        {
            if (token == null)
            {
                throw new Exception("Authorization Required.");
            }
            return token;
        }

        /// <summary>
        ///     Get Active Directory Client for Application.
        /// </summary>
        /// <returns>ActiveDirectoryClient for Application.</returns>
        public static ActiveDirectoryClient GetActiveDirectoryClient()
        {
            try
            {
                string ResourceUrl = "https://graph.windows.net/";
                string TenantId = "76a2ae5a-9f00-4f6b-95ed-5d33d77c4d61";
                Uri baseServiceUri = new Uri(ResourceUrl);
                ActiveDirectoryClient activeDirectoryClient =
               new ActiveDirectoryClient(new Uri(baseServiceUri, TenantId),
                   async () => await AcquireTokenAsync());
                return activeDirectoryClient;
            }
            catch (Exception)
            {

                throw;
            }
        }

    }
}
