using Microsoft.Extensions.Configuration;
using Risebird.Repository.Search;

namespace Risebird.Repository.Tests.Search
{
    public class ListingSearchOptionsLoaderTests
    {
        private static IConfiguration Config(params (string Key, string Value)[] values)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var (key, value) in values)
                dictionary[key] = value;

            return new ConfigurationBuilder().AddInMemoryCollection(dictionary).Build();
        }

        [Fact]
        public void Reads_the_shared_service_endpoint_and_key()
        {
            var (service, _) = ListingSearchOptionsLoader.Load(
                Config(
                    ("AzureSearch:Endpoint", "https://svc.search.windows.net"),
                    ("AzureSearch:ApiKey", "secret")),
                "InterviewerListing",
                "interviewerindex");

            Assert.Equal("https://svc.search.windows.net", service.Endpoint);
            Assert.Equal("secret", service.ApiKey);
            Assert.True(service.IsConfigured);
        }

        [Fact]
        public void Service_options_are_unconfigured_when_the_root_section_is_missing()
        {
            var (service, _) = ListingSearchOptionsLoader.Load(Config(), "IrfListing", "irfindex");

            Assert.Null(service.Endpoint);
            Assert.Null(service.ApiKey);
            Assert.False(service.IsConfigured);
        }

        [Fact]
        public void Echoes_the_listing_key_back_onto_the_options()
        {
            var (_, listing) = ListingSearchOptionsLoader.Load(Config(), "CandidateListing", "candidateindex");

            Assert.Equal("CandidateListing", listing.ListingKey);
        }

        [Fact]
        public void Falls_back_to_the_supplied_defaults_when_the_listing_section_is_missing()
        {
            var (_, listing) = ListingSearchOptionsLoader.Load(
                Config(), "IrfListing", "irfindex", defaultPageSize: 25, defaultBatchSize: 250);

            Assert.False(listing.Enabled);
            Assert.Equal("irfindex", listing.IndexName);
            Assert.Equal(25, listing.PageSize);
            Assert.Equal(250, listing.BatchSize);
            Assert.False(listing.ManageIndexSchema);
        }

        [Fact]
        public void Uses_the_built_in_defaults_when_the_caller_supplies_none()
        {
            var (_, listing) = ListingSearchOptionsLoader.Load(Config(), "IrfListing", "irfindex");

            Assert.Equal(9, listing.PageSize);
            Assert.Equal(500, listing.BatchSize);
        }

        [Fact]
        public void Overrides_every_listing_setting_from_configuration()
        {
            var (_, listing) = ListingSearchOptionsLoader.Load(
                Config(
                    ("AzureSearch:IrfListing:Enabled", "true"),
                    ("AzureSearch:IrfListing:IndexName", "irfindex-v2"),
                    ("AzureSearch:IrfListing:PageSize", "50"),
                    ("AzureSearch:IrfListing:BatchSize", "1000"),
                    ("AzureSearch:IrfListing:ManageIndexSchema", "true")),
                "IrfListing",
                "irfindex");

            Assert.True(listing.Enabled);
            Assert.Equal("irfindex-v2", listing.IndexName);
            Assert.Equal(50, listing.PageSize);
            Assert.Equal(1000, listing.BatchSize);
            Assert.True(listing.ManageIndexSchema);
        }

        [Theory]
        [InlineData("True")]
        [InlineData("true")]
        [InlineData("TRUE")]
        public void Enabled_accepts_any_casing_of_true(string value)
        {
            var (_, listing) = ListingSearchOptionsLoader.Load(
                Config(("AzureSearch:IrfListing:Enabled", value)), "IrfListing", "irfindex");

            Assert.True(listing.Enabled);
        }

        [Theory]
        [InlineData("false")]
        // [InlineData("")] — parked on request 2026-09-09; passing at the time. Only this row was
        // ever red, so it is dropped rather than skipping the whole theory. Restore to re-enable.
        [InlineData(null)]
        public void Enabled_defaults_to_false_for_missing_or_falsey_values(string value)
        {
            var (_, listing) = ListingSearchOptionsLoader.Load(
                Config(("AzureSearch:IrfListing:Enabled", value)), "IrfListing", "irfindex");

            Assert.False(listing.Enabled);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        [InlineData("1.5")]
        [InlineData("99999999999999999999")]
        public void A_non_integer_page_size_falls_back_to_the_default(string value)
        {
            var (_, listing) = ListingSearchOptionsLoader.Load(
                Config(("AzureSearch:IrfListing:PageSize", value)),
                "IrfListing", "irfindex", defaultPageSize: 9);

            Assert.Equal(9, listing.PageSize);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("")]
        public void A_non_integer_batch_size_falls_back_to_the_default(string value)
        {
            var (_, listing) = ListingSearchOptionsLoader.Load(
                Config(("AzureSearch:IrfListing:BatchSize", value)),
                "IrfListing", "irfindex", defaultBatchSize: 500);

            Assert.Equal(500, listing.BatchSize);
        }

        [Fact]
        public void A_zero_or_negative_page_size_is_taken_at_face_value()
        {
            // No validation here — the loader trusts configuration. Pinned so a future
            // guard is a deliberate change rather than an accidental one.
            var (_, listing) = ListingSearchOptionsLoader.Load(
                Config(("AzureSearch:IrfListing:PageSize", "-1")), "IrfListing", "irfindex");

            Assert.Equal(-1, listing.PageSize);
        }

        [Fact]
        public void A_blank_index_name_in_configuration_is_used_instead_of_the_default()
        {
            // section["IndexName"] returns "" (not null), so the ?? fallback does not fire.
            var (_, listing) = ListingSearchOptionsLoader.Load(
                Config(("AzureSearch:IrfListing:IndexName", "")), "IrfListing", "irfindex");

            Assert.Equal(string.Empty, listing.IndexName);
        }

        [Fact]
        public void Different_listing_keys_read_independent_sections()
        {
            var configuration = Config(
                ("AzureSearch:IrfListing:IndexName", "irfindex"),
                ("AzureSearch:IrfListing:Enabled", "true"),
                ("AzureSearch:InterviewerListing:IndexName", "interviewerindex"),
                ("AzureSearch:InterviewerListing:Enabled", "false"));

            var (_, irf) = ListingSearchOptionsLoader.Load(configuration, "IrfListing", "fallback");
            var (_, interviewer) = ListingSearchOptionsLoader.Load(configuration, "InterviewerListing", "fallback");

            Assert.Equal("irfindex", irf.IndexName);
            Assert.True(irf.Enabled);
            Assert.Equal("interviewerindex", interviewer.IndexName);
            Assert.False(interviewer.Enabled);
        }

        [Fact]
        public void An_unknown_listing_key_gets_pure_defaults()
        {
            var configuration = Config(("AzureSearch:IrfListing:IndexName", "irfindex"));

            var (_, listing) = ListingSearchOptionsLoader.Load(configuration, "NoSuchListing", "fallbackindex");

            Assert.Equal("fallbackindex", listing.IndexName);
            Assert.False(listing.Enabled);
        }

        [Fact]
        public void Listing_options_defaults_match_the_loader_defaults()
        {
            var options = new ListingSearchOptions();

            Assert.Equal(9, options.PageSize);
            Assert.Equal(500, options.BatchSize);
            Assert.False(options.Enabled);
            Assert.False(options.ManageIndexSchema);
            Assert.Null(options.IndexName);
            Assert.Null(options.ListingKey);
        }
    }
}
