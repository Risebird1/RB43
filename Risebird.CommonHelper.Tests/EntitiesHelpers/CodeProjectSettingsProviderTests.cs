using Microsoft.Extensions.Configuration;
using Moq;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.CommonHelper.Tests.EntitiesHelpers
{
    // The provider caches process-wide, so these tests must not run alongside each other.
    [Collection("CodeProjectSettingsProvider")]
    public class CodeProjectSettingsProviderTests
    {
        private static CodeProjectSettingsProvider Provider(Dictionary<string, string> config, Dictionary<string, (string, bool)> stored)
        {
            var repo = new Mock<ICodeProjectSettingsRepository>();
            repo.Setup(r => r.GetAll()).Returns(new Dictionary<string, (string Value, bool IsSecret)>(stored, StringComparer.OrdinalIgnoreCase));
            var encryption = new Mock<IEncryptionHelper>();
            encryption.Setup(e => e.DecryptData(It.IsAny<string>())).ReturnsAsync((string v) => "decrypted:" + v);
            var provider = new CodeProjectSettingsProvider(new ConfigurationBuilder().AddInMemoryCollection(config).Build(), repo.Object, encryption.Object);
            provider.Invalidate();
            return provider;
        }

        [Fact]
        public void Configuration_wins_over_the_stored_setting()
        {
            var p = Provider(new() { [CodeProjectSettingNames.AzureSubnetId] = "from-config" },
                new() { [CodeProjectSettingNames.AzureSubnetId] = ("from-db", false) });
            Assert.Equal("from-config", p.Get(CodeProjectSettingNames.AzureSubnetId));
        }

        [Fact]
        public void Blank_configuration_falls_back_to_the_stored_setting()
        {
            var p = Provider(new() { [CodeProjectSettingNames.AzureSubnetId] = "" },
                new() { [CodeProjectSettingNames.AzureSubnetId] = ("from-db", false) });
            Assert.Equal("from-db", p.Get(CodeProjectSettingNames.AzureSubnetId));
        }

        [Fact]
        public void Secrets_are_decrypted_and_missing_keys_are_null()
        {
            var p = Provider(new(), new() { [CodeProjectSettingNames.FunctionsKey] = ("cipher", true) });
            Assert.Equal("decrypted:cipher", p.Get(CodeProjectSettingNames.FunctionsKey));
            Assert.Null(p.Get("CodeProjects:Nope"));
        }

        [Fact]
        public void Functions_base_url_prefers_the_stored_url_and_trims_the_slash()
        {
            var p = Provider(new(), new() { [CodeProjectSettingNames.FunctionsBaseUrl] = ("https://fn.azurewebsites.net/api/", false) });
            Assert.Equal("https://fn.azurewebsites.net/api", p.FunctionsBaseUrl());
        }

        [Fact]
        public void Functions_base_url_falls_back_to_the_local_host_only_in_development()
        {
            var previous = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            try
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
                Assert.Equal(CodeProjectSettingNames.LocalFunctionsBaseUrl, Provider(new(), new()).FunctionsBaseUrl());
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
                Assert.Null(Provider(new(), new()).FunctionsBaseUrl());
            }
            finally
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", previous);
            }
        }
    }
}
