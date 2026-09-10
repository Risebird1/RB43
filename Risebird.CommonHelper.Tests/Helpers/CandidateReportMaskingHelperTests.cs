using Microsoft.AspNetCore.Http;
using Moq;
using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;

namespace Risebird.CommonHelper.Tests.Helpers
{
    /// <summary>
    /// A minimal in-memory <see cref="ISession"/>. The real one needs a full HTTP pipeline, and
    /// this helper only ever reads and writes one string key.
    /// </summary>
    internal sealed class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _store.Remove(key);
        public void Set(string key, byte[] value) => _store[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
    }

    /// <summary>
    /// Candidate masking is a privacy control: on the HM/DM approval drilldown a reviewer sees a
    /// coded id instead of the candidate's name, email and resume links. Every test here is
    /// either "masking happened when it should" or "masking could not be bypassed", and the
    /// bypass cases are the ones that matter — the whole design exists because removing a URL
    /// parameter must not be enough to unmask.
    /// </summary>
    public sealed class CandidateReportMaskingParamTests
    {
        [Theory]
        [InlineData("1")]
        [InlineData("true")]
        [InlineData("True")]
        [InlineData("TRUE")]
        [InlineData("yes")]
        [InlineData("Yes")]
        [InlineData("YES")]
        [InlineData("  1  ")]
        [InlineData("  true  ")]
        [InlineData("\tyes\t")]
        public void Recognised_affirmative_values_request_masking(string value)
        {
            Assert.True(CandidateReportMaskingHelper.IsMaskCandidateParam(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("0")]
        [InlineData("false")]
        [InlineData("no")]
        [InlineData("2")]
        [InlineData("11")]
        [InlineData("y")]
        [InlineData("t")]
        [InlineData("on")]
        [InlineData("yes please")]
        [InlineData("truthy")]
        public void Everything_else_does_not(string value)
        {
            // "y", "t" and "on" are deliberately not accepted: the parameter is produced by the
            // portal itself, so widening the vocabulary only widens the attack surface.
            Assert.False(CandidateReportMaskingHelper.IsMaskCandidateParam(value));
        }

        [Fact]
        public void The_session_key_is_the_documented_constant()
        {
            // Other layers read this key by name, so renaming it silently would disable masking.
            Assert.Equal("DmApprovalMaskCandidateContext", CandidateReportMaskingHelper.SessionKey);
        }

        [Fact]
        public void Activating_the_context_writes_the_flag_into_the_session()
        {
            var session = new FakeSession();

            CandidateReportMaskingHelper.ActivateMaskContext(session);

            Assert.Equal("1", session.GetString(CandidateReportMaskingHelper.SessionKey));
        }

        [Fact]
        public void Activating_the_context_twice_is_idempotent()
        {
            var session = new FakeSession();

            CandidateReportMaskingHelper.ActivateMaskContext(session);
            CandidateReportMaskingHelper.ActivateMaskContext(session);

            Assert.Equal("1", session.GetString(CandidateReportMaskingHelper.SessionKey));
            Assert.Single(session.Keys);
        }

        [Fact]
        public void Activating_with_no_session_is_a_no_op_rather_than_a_throw()
        {
            CandidateReportMaskingHelper.ActivateMaskContext(null);
        }
    }

    public sealed class CandidateReportMaskingDecisionTests
    {
        private static HttpRequest RequestWithReferer(string referer)
        {
            var context = new DefaultHttpContext();
            if (referer != null)
                context.Request.Headers["Referer"] = referer;
            return context.Request;
        }

        private static ISession ActivatedSession()
        {
            var session = new FakeSession();
            CandidateReportMaskingHelper.ActivateMaskContext(session);
            return session;
        }

        [Fact]
        public void An_explicit_param_masks_without_needing_a_session_or_a_referrer()
        {
            Assert.True(CandidateReportMaskingHelper.ShouldMaskCandidate(null, null, "1"));
        }

        [Fact]
        public void An_explicit_param_masks_even_from_an_unrelated_referrer()
        {
            var request = RequestWithReferer("https://portal.risebird.test/Home/Index");

            Assert.True(CandidateReportMaskingHelper.ShouldMaskCandidate(request, new FakeSession(), "yes"));
        }

        [Fact]
        public void The_drilldown_session_plus_a_stats_referrer_masks()
        {
            var request = RequestWithReferer(
                "https://portal.risebird.test/Interviewer/InterviewerStatsDetails?id=42");

            Assert.True(CandidateReportMaskingHelper.ShouldMaskCandidate(request, ActivatedSession(), null));
        }

        [Theory]
        [InlineData("https://portal.risebird.test/Interviewer/interviewerstatsdetails")]
        [InlineData("https://portal.risebird.test/Interviewer/INTERVIEWERSTATSDETAILS")]
        [InlineData("https://portal.risebird.test/x/InterviewerStatsDetails/nested")]
        [InlineData("InterviewerStatsDetails")]
        public void The_referrer_match_is_case_insensitive_and_positional(string referer)
        {
            Assert.True(CandidateReportMaskingHelper.ShouldMaskCandidate(
                RequestWithReferer(referer), ActivatedSession(), null));
        }

        [Fact]
        public void Removing_the_URL_parameter_does_not_bypass_masking()
        {
            // This is the point of the whole design: the param is gone, but the session plus the
            // referrer still mask.
            var request = RequestWithReferer(
                "https://portal.risebird.test/Interviewer/InterviewerStatsDetails");

            Assert.True(CandidateReportMaskingHelper.ShouldMaskCandidate(request, ActivatedSession(), ""));
        }

        [Fact]
        public void A_session_without_the_drilldown_flag_does_not_mask()
        {
            var request = RequestWithReferer(
                "https://portal.risebird.test/Interviewer/InterviewerStatsDetails");

            Assert.False(CandidateReportMaskingHelper.ShouldMaskCandidate(request, new FakeSession(), null));
        }

        [Fact]
        public void A_null_session_does_not_mask()
        {
            var request = RequestWithReferer(
                "https://portal.risebird.test/Interviewer/InterviewerStatsDetails");

            Assert.False(CandidateReportMaskingHelper.ShouldMaskCandidate(request, null, null));
        }

        [Fact]
        public void A_session_flag_with_any_other_value_does_not_mask()
        {
            var session = new FakeSession();
            session.SetString(CandidateReportMaskingHelper.SessionKey, "true");
            var request = RequestWithReferer(
                "https://portal.risebird.test/Interviewer/InterviewerStatsDetails");

            Assert.False(CandidateReportMaskingHelper.ShouldMaskCandidate(request, session, null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void A_missing_referrer_does_not_mask_even_with_the_session_flag(string referer)
        {
            // Direct navigation to the report is not a drilldown, so it is not masked.
            Assert.False(CandidateReportMaskingHelper.ShouldMaskCandidate(
                RequestWithReferer(referer), ActivatedSession(), null));
        }

        [Fact]
        public void A_null_request_does_not_mask_even_with_the_session_flag()
        {
            Assert.False(CandidateReportMaskingHelper.ShouldMaskCandidate(null, ActivatedSession(), null));
        }

        [Theory]
        [InlineData("https://portal.risebird.test/Interviewer/StatsDetails")]
        [InlineData("https://portal.risebird.test/Home/Index")]
        [InlineData("https://evil.test/InterviewerStats")]
        public void An_unrelated_referrer_does_not_mask(string referer)
        {
            Assert.False(CandidateReportMaskingHelper.ShouldMaskCandidate(
                RequestWithReferer(referer), ActivatedSession(), null));
        }

        [Fact]
        public void Several_referrer_headers_are_joined_and_still_matched()
        {
            var context = new DefaultHttpContext();
            context.Request.Headers["Referer"] =
                new[] { "https://portal.risebird.test/Home", "https://portal.risebird.test/InterviewerStatsDetails" };

            Assert.True(CandidateReportMaskingHelper.ShouldMaskCandidate(
                context.Request, ActivatedSession(), null));
        }

        [Fact]
        public void Nothing_at_all_does_not_mask()
        {
            Assert.False(CandidateReportMaskingHelper.ShouldMaskCandidate(null, null, null));
        }
    }

    public sealed class CandidateReportMaskingApplyTests
    {
        private static Mock<ICandidateService> CandidateService(IDictionary<string, string> pseudonyms)
        {
            var mock = new Mock<ICandidateService>(MockBehavior.Loose);
            mock.Setup(x => x.EnsureCandidatePseudonymsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .ReturnsAsync(pseudonyms);
            return mock;
        }

        private static InterviewReport Report(string email = "candidate@acme.test") => new()
        {
            CandidateName = "Asha Rao",
            CandidateEmail = email,
            resumeLink = "https://storage.test/resume.pdf",
            encodedresumeLink = "https://storage.test/resume.pdf?sig=abc",
            CodedResumeLink = "https://storage.test/coded.pdf"
        };

        [Fact]
        public async Task Masking_replaces_the_name_with_the_coded_id()
        {
            var report = Report();
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            Assert.Equal("CaAc1234", report.CandidateName);
        }

        [Fact]
        public async Task Masking_clears_every_identifying_link_and_the_email()
        {
            var report = Report();
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            Assert.Null(report.CandidateEmail);
            Assert.Null(report.resumeLink);
            Assert.Null(report.encodedresumeLink);
            Assert.Null(report.CodedResumeLink);
        }

        [Fact]
        public async Task The_email_is_trimmed_before_the_pseudonym_lookup()
        {
            var report = Report("  candidate@acme.test  ");
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            service.Verify(x => x.EnsureCandidatePseudonymsAsync(
                It.Is<IEnumerable<string>>(emails => emails.Single() == "candidate@acme.test"),
                It.IsAny<string>()), Times.Once);
            Assert.Equal("CaAc1234", report.CandidateName);
        }

        [Fact]
        public async Task A_pseudonym_keyed_with_different_casing_is_still_found()
        {
            // The dictionary the repository returns is not guaranteed to be case-insensitive, so
            // the helper falls back to an ordinal-ignore-case scan. Without it the report would
            // show the generic "Candidate" for a perfectly good pseudonym.
            var report = Report("Candidate@Acme.test");
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            Assert.Equal("CaAc1234", report.CandidateName);
        }

        [Fact]
        public async Task An_unmatched_email_falls_back_to_the_generic_label()
        {
            var report = Report("someone-else@acme.test");
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            Assert.Equal("Candidate", report.CandidateName);
        }

        [Fact]
        public async Task A_null_pseudonym_dictionary_falls_back_to_the_generic_label_and_still_clears_the_links()
        {
            var report = Report();
            var service = CandidateService(null);

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            Assert.Equal("Candidate", report.CandidateName);
            Assert.Null(report.CandidateEmail);
            Assert.Null(report.resumeLink);
        }

        [Fact]
        public async Task An_empty_pseudonym_dictionary_falls_back_to_the_generic_label()
        {
            var report = Report();
            var service = CandidateService(new Dictionary<string, string>());

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            Assert.Equal("Candidate", report.CandidateName);
        }

        [Fact]
        public async Task A_blank_pseudonym_falls_back_to_the_generic_label_rather_than_showing_nothing()
        {
            var report = Report();
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "   " });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            Assert.Equal("Candidate", report.CandidateName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task A_report_with_no_email_is_masked_generically_without_a_lookup(string email)
        {
            var report = Report(email);
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            Assert.Equal("Candidate", report.CandidateName);
            Assert.Null(report.CandidateEmail);
            service.Verify(x => x.EnsureCandidatePseudonymsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task The_acting_user_is_passed_through_as_the_pseudonym_creator()
        {
            var report = Report();
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object, "dm@risebird.io");

            service.Verify(x => x.EnsureCandidatePseudonymsAsync(
                It.IsAny<IEnumerable<string>>(), "dm@risebird.io"), Times.Once);
        }

        [Fact]
        public async Task No_acting_user_passes_null_through()
        {
            var report = Report();
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            service.Verify(x => x.EnsureCandidatePseudonymsAsync(
                It.IsAny<IEnumerable<string>>(), null), Times.Once);
        }

        [Fact]
        public async Task Exactly_one_email_is_sent_for_a_lookup()
        {
            var report = Report();
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            service.Verify(x => x.EnsureCandidatePseudonymsAsync(
                It.Is<IEnumerable<string>>(emails => emails.Count() == 1), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task A_null_report_is_a_no_op_rather_than_a_throw()
        {
            var service = CandidateService(new Dictionary<string, string>());

            await CandidateReportMaskingHelper.ApplyMaskingAsync(null, service.Object);

            service.Verify(x => x.EnsureCandidatePseudonymsAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Masking_an_already_masked_report_is_stable()
        {
            var report = Report();
            var service = CandidateService(new Dictionary<string, string> { ["candidate@acme.test"] = "CaAc1234" });

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);
            var afterFirst = report.CandidateName;
            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object);

            Assert.Equal("CaAc1234", afterFirst);
            Assert.Equal("Candidate", report.CandidateName);
            Assert.Null(report.CandidateEmail);
        }

        [Fact]
        public async Task A_lookup_failure_surfaces_rather_than_leaving_the_report_half_masked()
        {
            // Important ordering property: the links are cleared only after the lookup returns,
            // so a throwing lookup leaves the report untouched rather than partly scrubbed and
            // still carrying the real name.
            var report = Report();
            var service = new Mock<ICandidateService>(MockBehavior.Loose);
            service.Setup(x => x.EnsureCandidatePseudonymsAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("pseudonym store unavailable"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CandidateReportMaskingHelper.ApplyMaskingAsync(report, service.Object));

            Assert.Equal("Asha Rao", report.CandidateName);
            Assert.Equal("candidate@acme.test", report.CandidateEmail);
        }

        [Fact]
        public async Task A_null_candidate_service_throws_when_a_lookup_is_needed()
        {
            var report = Report();

            await Assert.ThrowsAsync<NullReferenceException>(
                () => CandidateReportMaskingHelper.ApplyMaskingAsync(report, null));
        }

        [Fact]
        public async Task A_null_candidate_service_is_harmless_when_there_is_no_email_to_look_up()
        {
            var report = Report(null);

            await CandidateReportMaskingHelper.ApplyMaskingAsync(report, null);

            Assert.Equal("Candidate", report.CandidateName);
        }
    }
}
