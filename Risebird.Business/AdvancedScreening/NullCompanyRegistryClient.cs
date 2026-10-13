using Risebird.Business.Interface;
using Risebird.Entities.ViewModel.AdvancedScreening;
using System.Threading.Tasks;

namespace Risebird.Business.AdvancedScreening
{
    /// <summary>
    /// The registry client until a provider is chosen. It looks nothing up, so company facts go to
    /// the scorer as "not verified" and it scores companyGenuineness at 50% - rather than a missing
    /// provider reading as "company not found". The internal blocklist still applies.
    /// Replace with a real client and one DI registration; nothing else changes.
    /// </summary>
    public class NullCompanyRegistryClient : ICompanyRegistryClient
    {
        public bool IsConfigured => false;

        public string ProviderName => "none-configured";

        public Task<CompanyVerificationRecord> LookupAsync(string companyName) => Task.FromResult<CompanyVerificationRecord>(null);
    }
}
