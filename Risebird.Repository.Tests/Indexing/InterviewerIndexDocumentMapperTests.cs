using Azure.Search.Documents.Models;
using Risebird.Entities.Search;
using Risebird.Repository.Indexing;

namespace Risebird.Repository.Tests.Indexing
{
    /// <summary>
    /// Mapping coverage for the interviewer Azure Search index. Requires
    /// InternalsVisibleTo("Risebird.Repository.Tests") on Risebird.Repository.
    /// </summary>
    public class InterviewerIndexDocumentMapperFromDatabaseRowTests
    {
        private static InterviewerIndexDocumentRow Row() => new()
        {
            InterviewerProfileId = 55,
            UserId = 9,
            FirstName = "Ada",
            LastName = "Lovelace",
            NickName = "AdLo1234",
            EmailId = "ada@risebird.io",
            MobileNumber = "+919876543210",
            PrimarySkill = "C#,SQL",
            AssessmentGroupId = "1,2",
            AssessmentGroupNames = "Backend;Data",
            Senioritylevel = "Senior",
            PerHourRateIndia = "1500",
            PerHourRateOutside = "40",
            CompentencyLevel = "L3",
            ProfileType = "Interviewer",
            CompanyType = "Product",
            Country = "India",
            IsLeadInterviewer = true,
            IsActive = true,
            IsInterviewerAccess = true,
            IsApproved = true,
            IsInterested = true,
            IsDeleted = false,
            Isavailable = 1,
            AvailabilityStatusValue = "Available",
            VideoConfirmation = 2,
            ResumeLink = "https://blob/r.pdf",
            CodedResumeLink = "https://blob/resumesummary_55.json",
            City = "Bengaluru",
            CurrentRole = "Staff Engineer",
            Experience = 12.5m,
            PreferredDomain = "Fintech",
            uniqueid = "uid-55",
            AvailabilityResponseDate = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc),
            IsHoldPayment = false
        };

        [Fact]
        public void Throws_for_a_null_row() =>
            Assert.Throws<ArgumentNullException>(() => InterviewerIndexDocumentMapper.FromDatabaseRow(null));

        [Fact]
        public void Copies_every_scalar_field_across()
        {
            var document = InterviewerIndexDocumentMapper.FromDatabaseRow(Row());

            Assert.Equal(55, document.InterviewerProfileId);
            Assert.Equal(9, document.UserId);
            Assert.Equal("Ada", document.FirstName);
            Assert.Equal("Lovelace", document.LastName);
            Assert.Equal("AdLo1234", document.NickName);
            Assert.Equal("ada@risebird.io", document.EmailId);
            Assert.Equal("+919876543210", document.MobileNumber);
            Assert.Equal("C#,SQL", document.PrimarySkill);
            Assert.Equal("1,2", document.AssessmentGroupId);
            Assert.Equal("Senior", document.Senioritylevel);
            Assert.Equal("India", document.Country);
            Assert.Equal(12.5m, document.Experience);
            Assert.Equal("uid-55", document.uniqueid);
            Assert.True(document.IsLeadInterviewer);
            Assert.False(document.IsDeleted);
            Assert.Equal(1, document.Isavailable);
            Assert.Equal(2, document.VideoConfirmation);
        }

        [Fact]
        public void Splits_the_primary_skill_csv_into_the_searchable_skills_collection()
        {
            var document = InterviewerIndexDocumentMapper.FromDatabaseRow(Row());

            Assert.Equal(new[] { "C#", "SQL" }, document.Skills);
        }

        [Fact]
        public void Splits_assessment_group_names_on_semicolons_too()
        {
            var document = InterviewerIndexDocumentMapper.FromDatabaseRow(Row());

            Assert.Equal(new[] { "Backend", "Data" }, document.AssessmentGroupNames);
        }

        [Theory]
        [InlineData("C# , SQL ,  Azure ")]
        [InlineData("C#;SQL;Azure")]
        [InlineData("C#,SQL;Azure")]
        [InlineData(",C#,,SQL,;Azure,")]
        public void Csv_splitting_trims_and_drops_empty_entries(string csv)
        {
            var row = Row();
            row.PrimarySkill = csv;

            var document = InterviewerIndexDocumentMapper.FromDatabaseRow(row);

            Assert.Equal(new[] { "C#", "SQL", "Azure" }, document.Skills);
        }

        [Fact]
        public void Csv_splitting_de_duplicates_case_insensitively_keeping_the_first_spelling()
        {
            var row = Row();
            row.PrimarySkill = "C#,c#,C#,SQL,sql";

            var document = InterviewerIndexDocumentMapper.FromDatabaseRow(row);

            Assert.Equal(new[] { "C#", "SQL" }, document.Skills);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(",")]
        [InlineData(", ;  ,")]
        public void A_blank_csv_becomes_an_empty_collection_not_null(string csv)
        {
            var row = Row();
            row.PrimarySkill = csv;
            row.AssessmentGroupNames = csv;

            var document = InterviewerIndexDocumentMapper.FromDatabaseRow(row);

            Assert.NotNull(document.Skills);
            Assert.Empty(document.Skills);
            Assert.NotNull(document.AssessmentGroupNames);
            Assert.Empty(document.AssessmentGroupNames);
        }

        [Fact]
        public void Null_strings_are_carried_through_as_null_at_this_stage()
        {
            var row = new InterviewerIndexDocumentRow { InterviewerProfileId = 1, UserId = 2 };

            var document = InterviewerIndexDocumentMapper.FromDatabaseRow(row);

            Assert.Null(document.FirstName);
            Assert.Null(document.EmailId);
            Assert.Null(document.ResumeLink);
        }

        [Fact]
        public void A_null_availability_response_date_stays_null()
        {
            var row = Row();
            row.AvailabilityResponseDate = null;

            var document = InterviewerIndexDocumentMapper.FromDatabaseRow(row);

            Assert.Null(document.AvailabilityResponseDate);
        }
    }

    public class InterviewerIndexDocumentMapperToAzureDocumentTests
    {
        private static InterviewerIndexDocument Document() => new()
        {
            InterviewerProfileId = 55,
            UserId = 9,
            FirstName = "Ada",
            LastName = "Lovelace",
            EmailId = "ada@risebird.io",
            Skills = new List<string> { "C#", "SQL" },
            AssessmentGroupNames = new List<string> { "Backend" },
            Experience = 12.5m,
            IsActive = true,
            AvailabilityResponseDate = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc)
        };

        [Fact]
        public void Throws_for_a_null_document() =>
            Assert.Throws<ArgumentNullException>(() => InterviewerIndexDocumentMapper.ToAzureDocument(null));

        [Fact]
        public void Writes_the_key_as_a_string_because_azure_keys_must_be_strings()
        {
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(Document());

            Assert.Equal("55", azure["interviewerProfileId"]);
            Assert.IsType<string>(azure["interviewerProfileId"]);
        }

        [Fact]
        public void Writes_the_user_id_as_a_number()
        {
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(Document());

            Assert.Equal(9, azure["userId"]);
        }

        [Fact]
        public void Coalesces_every_null_string_to_an_empty_string()
        {
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(
                new InterviewerIndexDocument { InterviewerProfileId = 1 });

            foreach (var field in new[]
                     {
                         "firstName", "lastName", "nickName", "emailId", "mobileNumber", "primarySkill",
                         "assessmentGroupId", "senioritylevel", "perHourRateIndia", "perHourRateOutside",
                         "compentencyLevel", "profileType", "companyType", "country",
                         "availabilityStatusValue", "resumeLink", "codedResumeLink", "city",
                         "currentRole", "preferredDomain", "uniqueid"
                     })
            {
                Assert.Equal(string.Empty, azure[field]);
            }
        }

        [Fact]
        public void Coalesces_null_collections_to_empty_lists()
        {
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(
                new InterviewerIndexDocument { InterviewerProfileId = 1 });

            Assert.Empty((List<string>)azure["skills"]);
            Assert.Empty((List<string>)azure["assessmentGroupNames"]);
        }

        [Fact]
        public void Writes_experience_as_a_double_because_the_index_field_is_edm_double()
        {
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(Document());

            Assert.Equal(12.5d, azure["experience"]);
            Assert.IsType<double>(azure["experience"]);
        }

        [Fact]
        public void Includes_the_availability_response_date_only_when_it_is_set()
        {
            var withDate = InterviewerIndexDocumentMapper.ToAzureDocument(Document());
            Assert.True(withDate.ContainsKey("availabilityResponseDate"));

            var document = Document();
            document.AvailabilityResponseDate = null;
            var withoutDate = InterviewerIndexDocumentMapper.ToAzureDocument(document);
            Assert.False(withoutDate.ContainsKey("availabilityResponseDate"));
        }

        [Fact]
        public void A_utc_availability_date_is_written_as_a_zero_offset_instant()
        {
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(Document());

            var offset = Assert.IsType<DateTimeOffset>(azure["availabilityResponseDate"]);
            Assert.Equal(TimeSpan.Zero, offset.Offset);
            Assert.Equal(new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc), offset.UtcDateTime);
        }

        [Fact]
        public void An_unspecified_availability_date_is_assumed_to_be_utc()
        {
            var document = Document();
            document.AvailabilityResponseDate = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Unspecified);

            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(document);

            var offset = Assert.IsType<DateTimeOffset>(azure["availabilityResponseDate"]);
            Assert.Equal(TimeSpan.Zero, offset.Offset);
            Assert.Equal(new DateTime(2026, 09, 09, 10, 00, 00), offset.DateTime);
        }

        [Fact]
        public void A_default_availability_date_becomes_the_minimum_offset_rather_than_throwing()
        {
            var document = Document();
            document.AvailabilityResponseDate = default(DateTime);

            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(document);

            Assert.Equal(DateTimeOffset.MinValue, azure["availabilityResponseDate"]);
        }

        [Fact]
        public void Writes_every_boolean_flag_as_a_boolean()
        {
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(Document());

            foreach (var field in new[]
                     {
                         "isLeadInterviewer", "isActive", "isInterviewerAccess", "isApproved",
                         "isInterested", "isDeleted", "isHoldPayment"
                     })
            {
                Assert.IsType<bool>(azure[field]);
            }
        }

        [Fact]
        public void Skills_are_copied_not_aliased_so_later_edits_do_not_leak_into_the_payload()
        {
            var document = Document();
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(document);

            document.Skills.Add("Azure");

            Assert.Equal(2, ((List<string>)azure["skills"]).Count);
        }
    }

    public class InterviewerIndexDocumentMapperToListViewModelTests
    {
        private static SearchDocument Azure() =>
            InterviewerIndexDocumentMapper.ToAzureDocument(new InterviewerIndexDocument
            {
                InterviewerProfileId = 55,
                UserId = 9,
                FirstName = "Ada",
                LastName = "Lovelace",
                NickName = "AdLo1234",
                EmailId = "ada@risebird.io",
                MobileNumber = "+919876543210",
                PrimarySkill = "C#,SQL",
                AssessmentGroupId = "1,2",
                Senioritylevel = "Senior",
                PerHourRateIndia = "1500",
                PerHourRateOutside = "40",
                CompentencyLevel = "L3",
                ProfileType = "Interviewer",
                CompanyType = "Product",
                Country = "India",
                IsLeadInterviewer = true,
                IsActive = true,
                IsInterviewerAccess = true,
                IsApproved = true,
                IsInterested = true,
                IsDeleted = false,
                Isavailable = 1,
                AvailabilityStatusValue = "Available",
                VideoConfirmation = 2,
                ResumeLink = "https://blob/r.pdf",
                CodedResumeLink = "https://blob/resumesummary_55.json",
                City = "Bengaluru",
                CurrentRole = "Staff Engineer",
                Experience = 12.5m,
                PreferredDomain = "Fintech",
                uniqueid = "uid-55",
                AvailabilityResponseDate = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc),
                IsHoldPayment = false
            });

        [Fact]
        public void Throws_for_a_null_document() =>
            Assert.Throws<ArgumentNullException>(() => InterviewerIndexDocumentMapper.ToListViewModel(null));

        [Fact]
        public void Round_trips_an_azure_document_back_into_the_list_view_model()
        {
            var vm = InterviewerIndexDocumentMapper.ToListViewModel(Azure());

            Assert.Equal(55, vm.id);
            Assert.Equal(9, vm.UserId);
            Assert.Equal("Ada", vm.FirstName);
            Assert.Equal("Lovelace", vm.LastName);
            Assert.Equal("AdLo1234", vm.NickName);
            Assert.Equal("ada@risebird.io", vm.EmailId);
            Assert.Equal("+919876543210", vm.MobileNumber);
            Assert.Equal("C#,SQL", vm.PrimarySkill);
            Assert.Equal("1,2", vm.AssessmentGroupId);
            Assert.Equal("Senior", vm.Senioritylevel);
            Assert.Equal("India", vm.Country);
            Assert.True(vm.IsLeadInterviewer);
            Assert.True(vm.IsActive);
            Assert.False(vm.IsDeleted);
            Assert.Equal(1, vm.Isavailable);
            Assert.Equal(2, vm.VideoConfirmation);
            Assert.Equal("Bengaluru", vm.City);
            Assert.Equal(12.5m, vm.Experience);
            Assert.Equal("uid-55", vm.uniqueid);
            Assert.False(vm.IsHoldPayment);
        }

        [Fact]
        public void Parses_the_string_key_back_into_an_integer_id()
        {
            var vm = InterviewerIndexDocumentMapper.ToListViewModel(Azure());

            Assert.Equal(55, vm.id);
        }

        [Fact]
        public void Reads_the_availability_response_date_back()
        {
            var vm = InterviewerIndexDocumentMapper.ToListViewModel(Azure());

            Assert.NotNull(vm.AvailabilityResponseDate);
            Assert.Equal(
                new DateTime(2026, 09, 09, 10, 00, 00),
                vm.AvailabilityResponseDate.Value.ToUniversalTime());
        }

        [Fact]
        public void An_absent_availability_response_date_reads_back_as_null()
        {
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(
                new InterviewerIndexDocument { InterviewerProfileId = 1 });

            var vm = InterviewerIndexDocumentMapper.ToListViewModel(azure);

            Assert.Null(vm.AvailabilityResponseDate);
        }

        [Fact]
        public void A_sparse_document_maps_to_defaults_rather_than_throwing()
        {
            var vm = InterviewerIndexDocumentMapper.ToListViewModel(new SearchDocument());

            Assert.Equal(0, vm.id);
            Assert.Equal(0, vm.UserId);
            Assert.Null(vm.FirstName);
            Assert.False(vm.IsActive);
            Assert.Equal(0m, vm.Experience);
            Assert.Null(vm.AvailabilityResponseDate);
        }

        [Fact]
        public void Full_pipeline_from_sql_row_to_list_view_model_preserves_the_identifying_fields()
        {
            var row = new InterviewerIndexDocumentRow
            {
                InterviewerProfileId = 77,
                UserId = 8,
                FirstName = "Grace",
                LastName = "Hopper",
                EmailId = "grace@risebird.io",
                PrimarySkill = "COBOL",
                Experience = 40m,
                IsActive = true
            };

            var vm = InterviewerIndexDocumentMapper.ToListViewModel(
                InterviewerIndexDocumentMapper.ToAzureDocument(
                    InterviewerIndexDocumentMapper.FromDatabaseRow(row)));

            Assert.Equal(77, vm.id);
            Assert.Equal(8, vm.UserId);
            Assert.Equal("Grace", vm.FirstName);
            Assert.Equal("Hopper", vm.LastName);
            Assert.Equal("grace@risebird.io", vm.EmailId);
            Assert.Equal("COBOL", vm.PrimarySkill);
            Assert.Equal(40m, vm.Experience);
            Assert.True(vm.IsActive);
        }
    }

    public class InterviewerIndexFieldsTests
    {
        [Fact]
        public void Field_names_are_distinct()
        {
            var fields = InterviewerIndexFields.ListViewSelect;

            Assert.Equal(fields.Count, fields.Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public void The_key_field_is_selected_for_the_list_view() =>
            Assert.Contains(InterviewerIndexFields.Key, InterviewerIndexFields.ListViewSelect);

        [Fact]
        public void Every_searchable_field_is_also_selected()
        {
            foreach (var field in InterviewerIndexFields.ListViewSearch)
                Assert.Contains(field, InterviewerIndexFields.ListViewSelect);
        }

        [Fact]
        public void Every_selected_field_is_present_on_a_fully_populated_azure_document()
        {
            var azure = InterviewerIndexDocumentMapper.ToAzureDocument(new InterviewerIndexDocument
            {
                InterviewerProfileId = 1,
                AvailabilityResponseDate = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc)
            });

            foreach (var field in InterviewerIndexFields.ListViewSelect)
                Assert.True(azure.ContainsKey(field), $"ToAzureDocument does not write '{field}'");
        }

        [Fact]
        public void Field_names_are_camel_case_with_no_whitespace()
        {
            foreach (var field in InterviewerIndexFields.ListViewSelect)
            {
                Assert.False(string.IsNullOrWhiteSpace(field));
                Assert.DoesNotContain(" ", field);
                Assert.True(char.IsLower(field[0]), $"'{field}' should start lower-case");
            }
        }
    }
}
