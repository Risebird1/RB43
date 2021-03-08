using Newtonsoft.Json;
using System.Collections.Generic;

namespace Risebird.Entities.ViewModel
{
    public class GraphUserVM
    {
        public override string ToString()
        {
            if (this.SignInNames != null && this.SignInNames.Count > 0)
            {
                return this.SignInNames[0].Value;
            }
            return base.ToString();
        }



        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        public bool ShouldSerializeObjectId()
        {
            return !string.IsNullOrEmpty(ObjectId);
        }


        [JsonProperty("otherMails")]
        public List<string> OtherMails { get; set; } = new List<string>();

        [JsonProperty("accountEnabled")]
        public bool AccountEnabled { get; set; }

        [JsonProperty("creationType")]
        public string CreationType { get; set; }

        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("mobile")]
        public string Mobile { get; set; }
        [JsonProperty("postalCode")]
        public string PostalCode { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("streetAddress")]
        public string StreetAddress { get; set; }

        [JsonProperty("telephoneNumber")]
        public string TelefphoneNumber { get; set; }

        [JsonProperty("mail")]
        public string Mail { get; set; }

        [JsonProperty("userPrincipalName")]
        public string UserPrincipalName { get; set; }

        [JsonProperty("surname")]
        public string Surname { get; set; }

        [JsonProperty("givenName")]
        public string GivenName { get; set; }

        [JsonProperty("mailNickname")]
        public string MailNickname { get; set; }

        [JsonProperty("passwordPolicies")]
        public string PasswordPolicies { get; set; }

        [JsonProperty("signInNames")]
        public List<GraphSignInNameVM> SignInNames { get; set; }

        [JsonProperty("passwordProfile")]
        public GraphPasswordProfileVM PasswordProfile { get; set; }
    }
}
