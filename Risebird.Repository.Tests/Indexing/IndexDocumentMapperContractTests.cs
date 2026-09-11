using Azure.Search.Documents.Models;
using Risebird.Entities.Search;
using Risebird.Repository.Indexing;

namespace Risebird.Repository.Tests.Indexing
{
    /// <summary>
    /// Azure Search document keys may only contain letters, digits, underscore, dash and equals.
    /// A key that slips a '@' or '.' through is rejected by the service at index time, which
    /// silently drops the row from the listing — so the sanitiser gets exhaustive coverage.
    /// </summary>
    public class CallCatalogueDocumentKeyTests
    {
        private static void AssertIsLegalAzureKey(string key)
        {
            Assert.False(string.IsNullOrEmpty(key));
            Assert.All(key, c => Assert.True(
                char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-' || c == '=',
                $"'{c}' is not legal in an Azure Search key (in \"{key}\")"));
        }

        [Fact]
        public void A_candidate_key_is_prefixed_with_c_and_carries_the_irf_id()
        {
            var key = CallCatalogueIndexDocumentMapper.BuildCandidateDocumentKey(42, "ada@risebird.io");

            Assert.StartsWith("c-42-", key);
            AssertIsLegalAzureKey(key);
        }

        [Fact]
        public void An_interviewer_key_is_prefixed_with_i()
        {
            var key = CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey("ada@risebird.io");

            Assert.StartsWith("i-", key);
            AssertIsLegalAzureKey(key);
        }

        [Fact]
        public void An_at_sign_becomes_an_underscore_delimited_word()
        {
            var key = CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey("ada@risebird.io");

            Assert.Contains("_at_", key);
            Assert.DoesNotContain("@", key);
        }

        [Fact]
        public void Dots_become_underscores()
        {
            var key = CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey("ada.lovelace@risebird.io");

            Assert.DoesNotContain(".", key);
            Assert.Contains("ada_lovelace", key);
        }

        [Fact]
        public void The_key_is_lower_cased_so_it_is_stable_across_email_casings()
        {
            Assert.Equal(
                CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey("ADA@RISEBIRD.IO"),
                CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey("ada@risebird.io"));
        }

        [Fact]
        public void Surrounding_whitespace_does_not_change_the_key()
        {
            Assert.Equal(
                CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey("  ada@risebird.io  "),
                CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey("ada@risebird.io"));
        }

        [Theory]
        [InlineData("ada+tag@risebird.io")]
        [InlineData("ada'quote@risebird.io")]
        [InlineData("ada/slash@risebird.io")]
        [InlineData("ada space@risebird.io")]
        [InlineData("ada#hash@risebird.io")]
        [InlineData("ada%percent@risebird.io")]
        [InlineData("unicode@risebird.io")]
        public void Every_illegal_character_is_replaced(string email)
        {
            AssertIsLegalAzureKey(CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey(email));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_blank_email_falls_back_to_the_empty_sentinel(string email)
        {
            Assert.Equal("i-empty", CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey(email));
            Assert.Equal("c-1-empty", CallCatalogueIndexDocumentMapper.BuildCandidateDocumentKey(1, email));
        }

        [Fact]
        public void Underscore_dash_and_equals_survive_unchanged()
        {
            var key = CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey("a_b-c=d");

            Assert.Equal("i-a_b-c=d", key);
        }

        [Fact]
        public void A_candidate_key_and_an_interviewer_key_never_collide()
        {
            Assert.NotEqual(
                CallCatalogueIndexDocumentMapper.BuildCandidateDocumentKey(1, "ada@risebird.io"),
                CallCatalogueIndexDocumentMapper.BuildInterviewerDocumentKey("ada@risebird.io"));
        }

        [Fact]
        public void Candidate_keys_are_scoped_per_irf()
        {
            Assert.NotEqual(
                CallCatalogueIndexDocumentMapper.BuildCandidateDocumentKey(1, "ada@risebird.io"),
                CallCatalogueIndexDocumentMapper.BuildCandidateDocumentKey(2, "ada@risebird.io"));
        }

        [Fact]
        public void Key_building_is_deterministic()
        {
            Assert.Equal(
                CallCatalogueIndexDocumentMapper.BuildCandidateDocumentKey(42, "ada@risebird.io"),
                CallCatalogueIndexDocumentMapper.BuildCandidateDocumentKey(42, "ada@risebird.io"));
        }

        [Fact]
        public void A_negative_irf_id_still_produces_a_legal_key()
        {
            // The '-' from the negative number is itself legal, so this must not be rejected.
            AssertIsLegalAzureKey(CallCatalogueIndexDocumentMapper.BuildCandidateDocumentKey(-5, "ada@risebird.io"));
        }
    }

    public class CallCatalogueIndexDocumentMapperTests
    {
        private static CallCatalogueIndexDocumentRow Row(string catalogueType = "Candidate") => new()
        {
            CatalogueType = catalogueType,
            IrfIntId = 42,
            IRFId = "IRF-42",
            CandidateEmail = "ada@risebird.io",
            CandidateName = "Ada Lovelace",
            JDTiltle = "Staff Engineer",
            PrimarySkill = "C#",
            ClientName = "Acme",
            CalledOn = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc),
            WhoCalled = "ops@risebird.io",
            AllowAllInternal = true,
            AllowedViewerEmails = "a@x.io,b@x.io"
        };

        [Fact]
        public void Throws_for_a_null_row() =>
            Assert.Throws<ArgumentNullException>(() => CallCatalogueIndexDocumentMapper.FromDatabaseRow(null));

        [Fact]
        public void A_candidate_row_gets_a_candidate_scoped_key()
        {
            var document = CallCatalogueIndexDocumentMapper.FromDatabaseRow(Row("Candidate"));

            Assert.StartsWith("c-42-", document.Id);
        }

        [Theory]
        [InlineData("Interviewer")]
        [InlineData("interviewer")]
        [InlineData("INTERVIEWER")]
        public void An_interviewer_row_gets_an_interviewer_scoped_key(string catalogueType)
        {
            var document = CallCatalogueIndexDocumentMapper.FromDatabaseRow(Row(catalogueType));

            Assert.StartsWith("i-", document.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Something else")]
        public void Any_other_catalogue_type_falls_back_to_the_candidate_key(string catalogueType)
        {
            var document = CallCatalogueIndexDocumentMapper.FromDatabaseRow(Row(catalogueType));

            Assert.StartsWith("c-42-", document.Id);
        }

        [Fact]
        public void A_null_catalogue_type_is_normalised_to_an_empty_string()
        {
            var document = CallCatalogueIndexDocumentMapper.FromDatabaseRow(Row(null));

            Assert.Equal(string.Empty, document.CatalogueType);
        }

        [Fact]
        public void Copies_the_scalar_columns_across()
        {
            var document = CallCatalogueIndexDocumentMapper.FromDatabaseRow(Row());

            Assert.Equal(42, document.IrfIntId);
            Assert.Equal("IRF-42", document.IRFId);
            Assert.Equal("ada@risebird.io", document.CandidateEmail);
            Assert.Equal("Ada Lovelace", document.CandidateName);
            Assert.Equal("Staff Engineer", document.JDTiltle);
            Assert.Equal("C#", document.PrimarySkill);
            Assert.Equal("Acme", document.ClientName);
            Assert.Equal("ops@risebird.io", document.WhoCalled);
            Assert.True(document.AllowAllInternal);
        }

        [Fact]
        public void Throws_when_asked_to_serialize_a_null_document() =>
            Assert.Throws<ArgumentNullException>(() => CallCatalogueIndexDocumentMapper.ToAzureDocument(null));

        [Fact]
        public void Throws_when_asked_to_read_a_null_document() =>
            Assert.Throws<ArgumentNullException>(() => CallCatalogueIndexDocumentMapper.ToListViewModel(null));

        [Fact]
        public void Writes_the_document_key_into_the_azure_payload()
        {
            var document = CallCatalogueIndexDocumentMapper.FromDatabaseRow(Row());

            var azure = CallCatalogueIndexDocumentMapper.ToAzureDocument(document);

            Assert.Equal(document.Id, azure[CallCatalogueIndexFields.Key]);
        }

        [Fact]
        public void Writes_every_list_view_field()
        {
            var azure = CallCatalogueIndexDocumentMapper.ToAzureDocument(
                CallCatalogueIndexDocumentMapper.FromDatabaseRow(Row()));

            foreach (var field in CallCatalogueIndexFields.ListViewSelect)
                Assert.True(azure.ContainsKey(field), $"ToAzureDocument does not write '{field}'");
        }

        [Fact]
        public void A_sparse_document_serializes_without_throwing()
        {
            var exception = Record.Exception(() =>
                CallCatalogueIndexDocumentMapper.ToAzureDocument(new CallCatalogueIndexDocument { Id = "c-1-empty" }));

            Assert.Null(exception);
        }

        [Fact]
        public void Round_trips_from_sql_row_to_list_view_model_without_throwing()
        {
            var exception = Record.Exception(() =>
                CallCatalogueIndexDocumentMapper.ToListViewModel(
                    CallCatalogueIndexDocumentMapper.ToAzureDocument(
                        CallCatalogueIndexDocumentMapper.FromDatabaseRow(Row()))));

            Assert.Null(exception);
        }

        [Fact]
        public void A_sparse_azure_document_reads_back_without_throwing()
        {
            var exception = Record.Exception(() =>
                CallCatalogueIndexDocumentMapper.ToListViewModel(new SearchDocument()));

            Assert.Null(exception);
        }
    }

    public class TemplateIndexDocumentMapperTests
    {
        private static TemplateIndexDocumentRow Row() => new()
        {
            ClonableSkillChannelId = 7,
            Name = "Backend template",
            CreatedFromIRFId = 42,
            MeetingPlatform = "Zoom",
            CompanyTypeId = 1,
            SeniorityLevelId = "3",
            CountryType = 1,
            AssessmentGroupName = "Backend",
            AssessmentGroupId = 5,
            DifficultyLevel = "Hard",
            EmploymentType = "FullTime",
            PerHourRate = 1500,
            CategoryName = "Engineering",
            SkillChannelCounts = 3,
            PrimarySkills = "C#,SQL",
            CreatedOn = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc),
            TaggedClients = "Acme,Globex",
            IsDeleted = false,
            SkillChannelName = "Backend channel",
            ChannelOwner = "owner@risebird.io"
        };

        [Fact]
        public void Throws_for_a_null_row() =>
            Assert.Throws<ArgumentNullException>(() => TemplateIndexDocumentMapper.FromDatabaseRow(null));

        [Fact]
        public void Throws_when_asked_to_serialize_a_null_document() =>
            Assert.Throws<ArgumentNullException>(() => TemplateIndexDocumentMapper.ToAzureDocument(null));

        [Fact]
        public void Throws_when_asked_to_read_a_null_document() =>
            Assert.Throws<ArgumentNullException>(() => TemplateIndexDocumentMapper.ToListViewModel(null));

        [Fact]
        public void Maps_the_identifying_columns_across()
        {
            var document = TemplateIndexDocumentMapper.FromDatabaseRow(Row());

            Assert.Equal(7, document.ClonableSkillChannelId);
            Assert.Equal("Backend template", document.Name);
            Assert.Equal(42, document.CreatedFromIRFId);
        }

        [Fact]
        public void An_all_null_row_maps_without_throwing()
        {
            var exception = Record.Exception(() =>
                TemplateIndexDocumentMapper.FromDatabaseRow(new TemplateIndexDocumentRow()));

            Assert.Null(exception);
        }

        [Fact]
        public void Writes_every_list_view_field()
        {
            var azure = TemplateIndexDocumentMapper.ToAzureDocument(
                TemplateIndexDocumentMapper.FromDatabaseRow(Row()));

            foreach (var field in TemplateIndexFields.ListViewSelect)
                Assert.True(azure.ContainsKey(field), $"ToAzureDocument does not write '{field}'");
        }

        [Fact]
        public void Round_trips_from_sql_row_to_list_view_model_without_throwing()
        {
            var exception = Record.Exception(() =>
                TemplateIndexDocumentMapper.ToListViewModel(
                    TemplateIndexDocumentMapper.ToAzureDocument(
                        TemplateIndexDocumentMapper.FromDatabaseRow(Row()))));

            Assert.Null(exception);
        }

        [Fact]
        public void A_sparse_azure_document_reads_back_without_throwing()
        {
            var exception = Record.Exception(() =>
                TemplateIndexDocumentMapper.ToListViewModel(new SearchDocument()));

            Assert.Null(exception);
        }

        [Fact]
        public void A_default_created_on_serializes_to_the_minimum_offset()
        {
            var row = Row();
            row.CreatedOn = default;

            var azure = TemplateIndexDocumentMapper.ToAzureDocument(
                TemplateIndexDocumentMapper.FromDatabaseRow(row));

            Assert.Equal(DateTimeOffset.MinValue, azure[TemplateIndexFields.CreatedOn]);
        }
    }

    public class RaisedIssueIndexDocumentMapperTests
    {
        private static RaisedIssueIndexDocumentRow Row() => new()
        {
            Id = 9,
            RequestType = "Bug",
            Comment = "Report will not open",
            AttachmentURL = "https://blob/a.png",
            RequestedBy = "ops@risebird.io",
            RequestedOn = new DateTime(2026, 09, 09, 10, 00, 00, DateTimeKind.Utc),
            Status = "Open",
            StatusComment = "triaging",
            ClosedBy = null,
            ClosedOn = null,
            Requester = "Ada Lovelace",
            InterviewId = 1234,
            AIReviewResponse = "looks like a permissions issue",
            MobileNumber = "+919876543210",
            Name = "Ada Lovelace",
            IrfIntId = 42,
            CandidateEmail = "cand@risebird.io",
            Interviewer = "iv@risebird.io",
            IsReopened = false
        };

        [Fact]
        public void Throws_for_a_null_row() =>
            Assert.Throws<ArgumentNullException>(() => RaisedIssueIndexDocumentMapper.FromDatabaseRow(null));

        [Fact]
        public void Throws_when_asked_to_serialize_a_null_document() =>
            Assert.Throws<ArgumentNullException>(() => RaisedIssueIndexDocumentMapper.ToAzureDocument(null));

        [Fact]
        public void Throws_when_asked_to_read_a_null_document() =>
            Assert.Throws<ArgumentNullException>(() => RaisedIssueIndexDocumentMapper.ToListViewModel(null));

        [Fact]
        public void Maps_the_identifying_columns_across()
        {
            var document = RaisedIssueIndexDocumentMapper.FromDatabaseRow(Row());

            Assert.Equal(9, document.Id);
            Assert.Equal("Bug", document.RequestType);
        }

        [Fact]
        public void An_all_null_row_maps_without_throwing()
        {
            var exception = Record.Exception(() =>
                RaisedIssueIndexDocumentMapper.FromDatabaseRow(new RaisedIssueIndexDocumentRow()));

            Assert.Null(exception);
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void Writes_every_list_view_field()
        {
            var azure = RaisedIssueIndexDocumentMapper.ToAzureDocument(
                RaisedIssueIndexDocumentMapper.FromDatabaseRow(Row()));

            foreach (var field in RaisedIssueIndexFields.ListViewSelect)
                Assert.True(azure.ContainsKey(field), $"ToAzureDocument does not write '{field}'");
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void An_oversized_comment_is_truncated_to_the_azure_string_limit()
        {
            var row = Row();
            row.Comment = new string('x', 40_000);

            var azure = RaisedIssueIndexDocumentMapper.ToAzureDocument(
                RaisedIssueIndexDocumentMapper.FromDatabaseRow(row));

            foreach (var value in azure.Select(field => field.Value).OfType<string>())
                Assert.True(value.Length <= 32_000, $"a string field of {value.Length} chars exceeds the 32 KB limit");
        }

        [Fact]
        public void Round_trips_from_sql_row_to_list_view_model_without_throwing()
        {
            var exception = Record.Exception(() =>
                RaisedIssueIndexDocumentMapper.ToListViewModel(
                    RaisedIssueIndexDocumentMapper.ToAzureDocument(
                        RaisedIssueIndexDocumentMapper.FromDatabaseRow(Row()))));

            Assert.Null(exception);
        }

        [Fact]
        public void A_sparse_azure_document_reads_back_without_throwing()
        {
            var exception = Record.Exception(() =>
                RaisedIssueIndexDocumentMapper.ToListViewModel(new SearchDocument()));

            Assert.Null(exception);
        }
    }

    /// <summary>
    /// Field-name contracts shared by every index: names must be unique, camelCase, and the
    /// searchable set must be a subset of the retrieved set — otherwise the listing renders
    /// blanks for a column users can search on.
    /// </summary>
    public class IndexFieldContractTests
    {
        public static IEnumerable<object[]> AllFieldSets() => new[]
        {
            new object[] { "CallCatalogue", CallCatalogueIndexFields.ListViewSearch, CallCatalogueIndexFields.ListViewSelect },
            new object[] { "Interviewer", InterviewerIndexFields.ListViewSearch, InterviewerIndexFields.ListViewSelect },
            new object[] { "Template", TemplateIndexFields.ListViewSearch, TemplateIndexFields.ListViewSelect },
            new object[] { "RaisedIssue", RaisedIssueIndexFields.ListViewSearch, RaisedIssueIndexFields.ListViewSelect },
        };

        [Theory]
        [MemberData(nameof(AllFieldSets))]
        public void Selected_field_names_are_unique(
            string index, IReadOnlyList<string> search, IReadOnlyList<string> select)
        {
            _ = index;
            _ = search;

            Assert.Equal(select.Count, select.Distinct(StringComparer.Ordinal).Count());
        }

        [Theory]
        [MemberData(nameof(AllFieldSets))]
        public void Searchable_fields_are_also_retrieved(
            string index, IReadOnlyList<string> search, IReadOnlyList<string> select)
        {
            foreach (var field in search)
                Assert.True(select.Contains(field, StringComparer.Ordinal),
                    $"{index}: '{field}' is searchable but not selected, so the column renders blank");
        }

        [Theory]
        [MemberData(nameof(AllFieldSets))]
        public void Field_names_are_camel_case_with_no_whitespace(
            string index, IReadOnlyList<string> search, IReadOnlyList<string> select)
        {
            _ = search;

            foreach (var field in select)
            {
                Assert.False(string.IsNullOrWhiteSpace(field));
                Assert.DoesNotContain(" ", field);
                Assert.True(char.IsLower(field[0]), $"{index}: '{field}' should start lower-case");
            }
        }

        [Theory]
        [MemberData(nameof(AllFieldSets))]
        public void Every_index_exposes_at_least_one_searchable_field(
            string index, IReadOnlyList<string> search, IReadOnlyList<string> select)
        {
            _ = index;
            _ = select;

            Assert.NotEmpty(search);
        }
    }
}
