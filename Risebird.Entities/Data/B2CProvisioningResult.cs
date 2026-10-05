using Risebird.Entities.Enum;

namespace Risebird.Entities.Data
{
    /// <summary>
    /// What happened when a user was provisioned into Azure AD B2C, and the password to store if a
    /// new account was actually created.
    /// </summary>
    public class B2CProvisioningResult
    {
        public B2CProvisioningOutcome Outcome { get; set; }

        /// <summary>
        /// The generated password. Populated only when <see cref="Outcome"/> is
        /// <see cref="B2CProvisioningOutcome.Created"/> - for an account that already existed the
        /// generated password was never applied, so sending it to the user would be wrong.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>True when provisioning must stop and nothing should be written.</summary>
        public bool ShouldAbort => Outcome == B2CProvisioningOutcome.Failed;

        /// <summary>True when a usable password was produced and may be stored or emailed.</summary>
        public bool HasUsablePassword =>
            Outcome == B2CProvisioningOutcome.Created && !string.IsNullOrEmpty(Password);
    }
}
