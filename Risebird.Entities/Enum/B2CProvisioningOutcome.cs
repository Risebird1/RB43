namespace Risebird.Entities.Enum
{
    /// <summary>
    /// Result of trying to create an Azure AD B2C account. "Already exists" has to be distinguished
    /// from "failed": the former is a normal case when re-adding someone who is already in the
    /// directory, while the latter must stop provisioning so the user is not written to the portal
    /// database with no account to sign in with.
    /// </summary>
    public enum B2CProvisioningOutcome
    {
        /// <summary>A new B2C account was created and the generated password applies to it.</summary>
        Created = 0,

        /// <summary>
        /// The account was already in the directory. Provisioning may continue, but the generated
        /// password was NOT applied - the user keeps their existing credentials.
        /// </summary>
        AlreadyExists = 1,

        /// <summary>The account could not be created. Provisioning must not continue.</summary>
        Failed = 2
    }
}
