using System.ComponentModel;
using Risebird.Entities.Enum;

namespace Risebird.CommonHelper.Tests.Helpers
{
    public class EnumExtensionMethodsTests
    {
        private enum Sample
        {
            [Description("Plain vanilla")]
            Described,

            NotDescribed,

            [Description("")]
            EmptyDescription,

            [Description("  spaced  ")]
            SpacedDescription
        }

        [Fact]
        public void Returns_the_description_attribute_when_present() =>
            Assert.Equal("Plain vanilla", Sample.Described.GetDescription());

        [Fact]
        public void Falls_back_to_the_member_name_when_no_attribute_is_present() =>
            Assert.Equal("NotDescribed", Sample.NotDescribed.GetDescription());

        [Fact]
        public void Returns_an_empty_description_verbatim_rather_than_the_member_name() =>
            Assert.Equal(string.Empty, Sample.EmptyDescription.GetDescription());

        [Fact]
        public void Does_not_trim_the_description() =>
            Assert.Equal("  spaced  ", Sample.SpacedDescription.GetDescription());

        [Fact]
        public void Falls_back_to_the_numeric_string_for_an_undefined_value()
        {
            // GetMember("999") finds nothing, so the ToString() fallback is returned.
            var undefined = (Sample)999;

            Assert.Equal("999", undefined.GetDescription());
        }

        [Fact]
        public void Works_on_a_real_domain_enum_without_descriptions() =>
            Assert.Equal("Exception", LogLevelInfo.Exception.GetDescription());

        [Fact]
        public void Every_LogLevelInfo_member_resolves_to_a_non_empty_label()
        {
            foreach (LogLevelInfo level in System.Enum.GetValues<LogLevelInfo>())
                Assert.False(string.IsNullOrWhiteSpace(level.GetDescription()));
        }
    }

    public class JobLevelHelperTests
    {
        [Fact]
        public void Returns_the_six_canonical_experience_bands()
        {
            var options = JobLevelHelper.SelectListOptions().ToList();

            Assert.Equal(6, options.Count);
        }

        [Fact]
        public void Returns_the_bands_in_ascending_experience_order()
        {
            var ids = JobLevelHelper.SelectListOptions().Select(o => o.ID).ToList();

            Assert.Equal(
                new[]
                {
                    (int)JobLevels.Years_1_3,
                    (int)JobLevels.Years_3_6,
                    (int)JobLevels.Years_6_9,
                    (int)JobLevels.Years_9_12,
                    (int)JobLevels.Years_12_15,
                    (int)JobLevels.Years_15_Plus
                },
                ids);
        }

        [Fact]
        public void Every_option_has_a_distinct_id()
        {
            var ids = JobLevelHelper.SelectListOptions().Select(o => o.ID).ToList();

            Assert.Equal(ids.Count, ids.Distinct().Count());
        }

        [Fact]
        public void Every_option_has_a_non_blank_display_name()
        {
            foreach (var option in JobLevelHelper.SelectListOptions())
                Assert.False(string.IsNullOrWhiteSpace(option.Name));
        }

        [Fact]
        public void Names_come_from_the_enum_description_attribute()
        {
            var options = JobLevelHelper.SelectListOptions().ToList();

            Assert.Equal(JobLevels.Years_1_3.GetDescription(), options[0].Name);
            Assert.Equal(JobLevels.Years_15_Plus.GetDescription(), options[^1].Name);
        }

        [Fact]
        public void Enumerating_twice_yields_the_same_sequence()
        {
            var first = JobLevelHelper.SelectListOptions().ToList();
            var second = JobLevelHelper.SelectListOptions().ToList();

            Assert.Equal(first, second);
        }
    }
}
