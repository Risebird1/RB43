using Microsoft.Extensions.Options;
using Risebird.Entities;

namespace Risebird.CommonHelper.Tests.HttpSuites
{
    /// <summary>
    /// <c>HttpClientHelper</c> hands the micro-service base address to every caller that wants a
    /// client. Getting the base address wrong sends portal traffic nowhere in particular, and the
    /// method is defensive about a missing configuration section in ways worth pinning — the
    /// options object can legitimately be absent in some hosting paths.
    /// </summary>
    public sealed class HttpClientHelperTests
    {
        private static IOptions<ConfigurationOptions> Options(string webApiUrl) =>
            Microsoft.Extensions.Options.Options.Create(new ConfigurationOptions
            {
                MicroServiceSettings = new MicroServiceSettingsOptions { WebApiUrl = webApiUrl }
            });

        [Fact]
        public void A_configured_url_becomes_the_client_base_address()
        {
            var helper = new HttpClientHelper(null, Options("https://api.risebird.test/"));

            Assert.Equal("https://api.risebird.test/", helper.GetHttpClient().BaseAddress.ToString());
        }

        [Fact]
        public void A_url_without_a_trailing_slash_is_kept_as_given()
        {
            // Relevant because HttpClient drops the last path segment when combining a relative
            // path with a base address that has no trailing slash.
            var helper = new HttpClientHelper(null, Options("https://api.risebird.test/v1"));

            Assert.Equal("https://api.risebird.test/v1", helper.GetHttpClient().BaseAddress.ToString());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void A_blank_url_leaves_the_base_address_unset(string webApiUrl)
        {
            var helper = new HttpClientHelper(null, Options(webApiUrl));

            Assert.Null(helper.GetHttpClient().BaseAddress);
        }

        [Fact]
        public void A_whitespace_url_is_rejected_because_only_null_and_empty_are_skipped()
        {
            // The guard is IsNullOrEmpty, so a whitespace-only setting reaches the Uri
            // constructor and fails loudly at startup rather than silently doing nothing.
            Assert.Throws<UriFormatException>(() => new HttpClientHelper(null, Options("   ")));
        }

        [Fact]
        public void A_malformed_url_throws_rather_than_being_silently_ignored()
        {
            Assert.Throws<UriFormatException>(() => new HttpClientHelper(null, Options("not a url")));
        }

        [Fact]
        public void A_null_options_object_is_tolerated_and_leaves_the_base_address_unset()
        {
            var helper = new HttpClientHelper(null, null);

            Assert.NotNull(helper.GetHttpClient());
            Assert.Null(helper.GetHttpClient().BaseAddress);
        }

        [Fact]
        public void A_null_options_value_is_tolerated()
        {
            var helper = new HttpClientHelper(null, new NullValueOptions());

            Assert.Null(helper.GetHttpClient().BaseAddress);
        }

        [Fact]
        public void A_null_MicroServiceSettings_section_throws_which_is_the_one_gap_in_the_null_guards()
        {
            // The chain is options?.Value?.MicroServiceSettings.WebApiUrl — the last hop has no
            // null-conditional, so an options object whose section was never bound throws here.
            // Recorded as a defect rather than worked around; the fix is one extra "?".
            var options = Microsoft.Extensions.Options.Options.Create(
                new ConfigurationOptions { MicroServiceSettings = null });

            Assert.Throws<NullReferenceException>(() => new HttpClientHelper(null, options));
        }

        [Fact]
        public void The_same_client_instance_is_returned_on_every_call()
        {
            // Callers rely on this: a helper that handed out a fresh HttpClient per call would
            // exhaust sockets under load.
            var helper = new HttpClientHelper(null, Options("https://api.risebird.test/"));

            Assert.Same(helper.GetHttpClient(), helper.GetHttpClient());
        }

        [Fact]
        public void Two_helpers_do_not_share_a_client()
        {
            var first = new HttpClientHelper(null, Options("https://api.risebird.test/"));
            var second = new HttpClientHelper(null, Options("https://api.risebird.test/"));

            Assert.NotSame(first.GetHttpClient(), second.GetHttpClient());
        }

        [Fact]
        public void Constructing_the_helper_makes_no_network_call()
        {
            // A constructor that reached out would make every DI container build slow and
            // network-dependent. There is nothing to assert but the absence of a hang, so this
            // simply exercises the constructor under the suite's timeout.
            var helper = new HttpClientHelper(null, Options("https://api.risebird.test/"));

            Assert.NotNull(helper.GetHttpClient());
        }

        [Fact]
        public void A_default_ConfigurationOptions_has_a_settings_section_with_no_url()
        {
            // The parameterless constructor creates the section, which is why the missing "?"
            // above does not bite in the normal binding path.
            var options = new ConfigurationOptions();

            Assert.NotNull(options.MicroServiceSettings);
            Assert.Null(options.MicroServiceSettings.WebApiUrl);
        }

        private sealed class NullValueOptions : IOptions<ConfigurationOptions>
        {
            public ConfigurationOptions Value => null;
        }
    }
}
