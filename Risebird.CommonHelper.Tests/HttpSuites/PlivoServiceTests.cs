using System.Net;
using System.Text;
using Moq;
using Newtonsoft.Json.Linq;
using Risebird.CommonHelper.Interface;
using Risebird.CommonHelper.Tests.Infrastructure;
using Risebird.Entities.Data.PlivoModels;

namespace Risebird.CommonHelper.Tests.HttpSuites
{
    /// <summary>
    /// <c>PlivoService</c> is the one CommonHelper client that already takes an
    /// <c>IHttpClientFactory</c>, which makes it fully testable offline — so it is both the most
    /// valuable suite here and the template for the rest of the helpers once they get the same
    /// seam. It places real outbound calls that cost money and reach candidates, so the URL, the
    /// auth header and the payload all matter.
    /// </summary>
    public sealed class PlivoServiceFixture
    {
        public StubHttpMessageHandler Handler { get; } = new();
        public Mock<ICandidateCallTracker> CallTracker { get; } = new(MockBehavior.Loose);

        public string AuthId { get; set; } = "MAXXXXXXXXXXXXXXXXXX";
        public string AuthToken { get; set; } = "the-auth-token";
        public string FromNumber { get; set; } = "+911234567890";

        public PlivoService CreateSut()
        {
            var configuration = TestConfiguration.From(
                ("Plivo:AuthId", AuthId),
                ("Plivo:AuthToken", AuthToken),
                ("Plivo:FromNumber", FromNumber));

            CallTracker.Setup(x => x.IncrementVapiCallAttemptsAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            return new PlivoService(configuration, new StubHttpClientFactory(Handler), CallTracker.Object);
        }

        public static PlivoOutboundCallRequest Request(
            string to = "+919876543210",
            string from = null,
            string answerUrl = "https://portal.risebird.test/plivo/answer") => new()
            {
                To = to,
                From = from,
                AnswerUrl = answerUrl
            };

        /// <summary>The Basic credential Plivo expects, for comparing against what was sent.</summary>
        public string ExpectedBasicCredential =>
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{AuthId}:{AuthToken}"));
    }

    public sealed class PlivoServiceInitiateCallTests
    {
        [Fact]
        public async Task The_call_is_posted_to_the_account_scoped_Call_endpoint()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            var request = fixture.Handler.TheRequest;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                $"https://api.plivo.com/v1/Account/{fixture.AuthId}/Call/",
                request.Uri.ToString());
        }

        [Fact]
        public async Task The_request_carries_Basic_auth_built_from_the_configured_credentials()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            Assert.Equal(
                "Basic " + fixture.ExpectedBasicCredential,
                fixture.Handler.TheRequest.Header("Authorization"));
        }

        [Fact]
        public async Task The_auth_header_is_set_once_on_the_client_not_per_request()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request());
            await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            Assert.Equal(2, fixture.Handler.Requests.Count);
            Assert.All(fixture.Handler.Requests,
                request => Assert.Equal("Basic " + fixture.ExpectedBasicCredential, request.Header("Authorization")));
        }

        [Fact]
        public async Task The_payload_uses_the_snake_case_names_the_Plivo_API_expects()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request(
                to: "+919876543210", answerUrl: "https://portal.risebird.test/plivo/answer"));

            var payload = JObject.Parse(fixture.Handler.TheRequest.Body);
            Assert.Equal("+919876543210", payload["to"].Value<string>());
            Assert.Equal("https://portal.risebird.test/plivo/answer", payload["answer_url"].Value<string>());
            Assert.Equal("GET", payload["answer_method"].Value<string>());
            Assert.Equal("POST", payload["hangup_method"].Value<string>());
            Assert.Equal(300, payload["time_limit"].Value<int>());
        }

        [Fact]
        public async Task The_body_is_sent_as_UTF8_JSON()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            var contentType = fixture.Handler.TheRequest.ContentType;
            Assert.Contains("application/json", contentType);
            Assert.Contains("utf-8", contentType.ToLowerInvariant());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task A_missing_From_falls_back_to_the_configured_caller_id(string from)
        {
            var fixture = new PlivoServiceFixture { FromNumber = "+911111111111" };
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request(from: from));

            var payload = JObject.Parse(fixture.Handler.TheRequest.Body);
            Assert.Equal("+911111111111", payload["from"].Value<string>());
        }

        [Fact]
        public async Task An_explicit_From_wins_over_the_configured_caller_id()
        {
            var fixture = new PlivoServiceFixture { FromNumber = "+911111111111" };
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request(from: "+912222222222"));

            var payload = JObject.Parse(fixture.Handler.TheRequest.Body);
            Assert.Equal("+912222222222", payload["from"].Value<string>());
        }

        [Fact]
        public async Task A_whitespace_From_is_kept_because_only_null_and_empty_fall_back()
        {
            // Pinned deliberately: the guard is IsNullOrEmpty, not IsNullOrWhiteSpace, so a
            // stray space reaches Plivo as the caller id and the call fails at the provider.
            var fixture = new PlivoServiceFixture { FromNumber = "+911111111111" };
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request(from: " "));

            var payload = JObject.Parse(fixture.Handler.TheRequest.Body);
            Assert.Equal(" ", payload["from"].Value<string>());
        }

        [Fact]
        public async Task The_fallback_mutates_the_request_object_the_caller_passed_in()
        {
            // Worth knowing: a caller that reuses one request object for several calls will see
            // From filled in after the first call.
            var fixture = new PlivoServiceFixture { FromNumber = "+911111111111" };
            var sut = fixture.CreateSut();
            var request = PlivoServiceFixture.Request(from: null);

            await sut.InitiateCallAsync(request);

            Assert.Equal("+911111111111", request.From);
        }

        [Fact]
        public async Task Null_valued_optional_fields_are_omitted_rather_than_sent_as_null()
        {
            // Plivo rejects an explicit null for these, so NullValueHandling.Ignore is load
            // bearing, not cosmetic.
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            var body = fixture.Handler.TheRequest.Body;
            var payload = JObject.Parse(body);
            Assert.False(payload.ContainsKey("machine_detection"));
            Assert.False(payload.ContainsKey("machine_detection_url"));
            Assert.False(payload.ContainsKey("hangup_on_ring"));
            Assert.False(payload.ContainsKey("sip_headers"));
            Assert.DoesNotContain("null", body);
        }

        [Fact]
        public async Task Optional_fields_that_are_set_do_reach_the_payload()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();
            var request = PlivoServiceFixture.Request();
            request.MachineDetection = "true";
            request.MachineDetectionUrl = "https://portal.risebird.test/plivo/machine";
            request.HangupOnRing = 25;
            request.SipHeaders = "X-Interview=42";
            request.HangupUrl = "https://portal.risebird.test/plivo/hangup";
            request.RingUrl = "https://portal.risebird.test/plivo/ring";
            request.TimeLimit = 600;

            await sut.InitiateCallAsync(request);

            var payload = JObject.Parse(fixture.Handler.TheRequest.Body);
            Assert.Equal("true", payload["machine_detection"].Value<string>());
            Assert.Equal("https://portal.risebird.test/plivo/machine", payload["machine_detection_url"].Value<string>());
            Assert.Equal(25, payload["hangup_on_ring"].Value<int>());
            Assert.Equal("X-Interview=42", payload["sip_headers"].Value<string>());
            Assert.Equal("https://portal.risebird.test/plivo/hangup", payload["hangup_url"].Value<string>());
            Assert.Equal("https://portal.risebird.test/plivo/ring", payload["ring_url"].Value<string>());
            Assert.Equal(600, payload["time_limit"].Value<int>());
        }

        [Fact]
        public async Task A_successful_response_is_deserialised_into_the_request_uuid()
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(HttpStatusCode.Accepted,
                "{\"api_id\":\"abc-123\",\"message\":\"call fired\",\"request_uuid\":\"req-777\"}");
            var sut = fixture.CreateSut();

            var response = await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            Assert.Equal("abc-123", response.ApiId);
            Assert.Equal("call fired", response.Message);
            Assert.Equal("req-777", response.RequestUUID);
        }

        [Fact]
        public async Task Unknown_fields_in_the_response_are_ignored_rather_than_fatal()
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(HttpStatusCode.Accepted,
                "{\"request_uuid\":\"req-777\",\"a_new_field_plivo_added\":123}");
            var sut = fixture.CreateSut();

            var response = await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            Assert.Equal("req-777", response.RequestUUID);
        }

        [Fact]
        public async Task An_empty_response_body_deserialises_to_null_rather_than_throwing()
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(HttpStatusCode.Accepted, "");
            var sut = fixture.CreateSut();

            var response = await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            Assert.Null(response);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, 400)]
        [InlineData(HttpStatusCode.Unauthorized, 401)]
        [InlineData(HttpStatusCode.Forbidden, 403)]
        [InlineData(HttpStatusCode.NotFound, 404)]
        [InlineData(HttpStatusCode.TooManyRequests, 429)]
        [InlineData(HttpStatusCode.InternalServerError, 500)]
        [InlineData(HttpStatusCode.ServiceUnavailable, 503)]
        public async Task A_failure_status_throws_carrying_the_status_and_the_body(
            HttpStatusCode status, int expectedCode)
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(status, "{\"error\":\"to number invalid\"}");
            var sut = fixture.CreateSut();

            var exception = await Assert.ThrowsAsync<Exception>(
                () => sut.InitiateCallAsync(PlivoServiceFixture.Request()));

            Assert.Contains(expectedCode.ToString(), exception.Message);
            Assert.Contains("to number invalid", exception.Message);
            Assert.Contains("Plivo call initiation failed", exception.Message);
        }

        [Fact]
        public async Task A_failure_does_not_count_a_call_attempt_against_the_candidate()
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(HttpStatusCode.BadRequest, "{}");
            var sut = fixture.CreateSut();

            await Assert.ThrowsAsync<Exception>(
                () => sut.InitiateCallAsync(PlivoServiceFixture.Request(), candidateId: 55));

            fixture.CallTracker.Verify(x => x.IncrementVapiCallAttemptsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task A_successful_call_counts_one_attempt_against_the_named_candidate()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request(), candidateId: 55);

            fixture.CallTracker.Verify(x => x.IncrementVapiCallAttemptsAsync(55), Times.Once);
        }

        [Fact]
        public async Task No_candidate_id_means_no_attempt_is_counted()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            fixture.CallTracker.Verify(x => x.IncrementVapiCallAttemptsAsync(It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task A_non_positive_candidate_id_is_still_counted_because_only_null_is_checked(int candidateId)
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request(), candidateId);

            fixture.CallTracker.Verify(x => x.IncrementVapiCallAttemptsAsync(candidateId), Times.Once);
        }

        [Fact]
        public async Task A_transport_failure_surfaces_and_counts_nothing()
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWithTransportFailure();
            var sut = fixture.CreateSut();

            await Assert.ThrowsAsync<HttpRequestException>(
                () => sut.InitiateCallAsync(PlivoServiceFixture.Request(), candidateId: 55));

            fixture.CallTracker.Verify(x => x.IncrementVapiCallAttemptsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task A_null_request_throws_before_anything_is_sent()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await Assert.ThrowsAsync<NullReferenceException>(() => sut.InitiateCallAsync(null));

            Assert.Empty(fixture.Handler.Requests);
        }

        [Fact]
        public async Task A_missing_auth_id_still_builds_a_URL_which_is_how_a_misconfiguration_shows_up()
        {
            // Pinned so a blank Plivo:AuthId produces a recognisably wrong URL rather than a
            // silent success against some other account.
            var fixture = new PlivoServiceFixture { AuthId = null };
            var sut = fixture.CreateSut();

            await sut.InitiateCallAsync(PlivoServiceFixture.Request());

            Assert.Equal("https://api.plivo.com/v1/Account//Call/", fixture.Handler.TheRequest.Uri.ToString());
        }
    }

    public sealed class PlivoServiceCallDetailsTests
    {
        [Fact]
        public async Task Call_details_are_fetched_from_the_call_scoped_endpoint()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.GetCallDetailsAsync("call-uuid-1");

            var request = fixture.Handler.TheRequest;
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                $"https://api.plivo.com/v1/Account/{fixture.AuthId}/Call/call-uuid-1/",
                request.Uri.ToString());
        }

        [Fact]
        public async Task Call_details_send_no_body()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.GetCallDetailsAsync("call-uuid-1");

            Assert.Null(fixture.Handler.TheRequest.Body);
        }

        [Fact]
        public async Task The_CDR_is_deserialised_including_the_billing_fields()
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(HttpStatusCode.OK,
                "{\"call_uuid\":\"call-uuid-1\",\"call_status\":\"completed\",\"call_duration\":95," +
                "\"bill_duration\":120,\"total_amount\":\"0.0075\",\"hangup_cause_name\":\"NORMAL_CLEARING\"," +
                "\"from_number\":\"+911234567890\",\"to_number\":\"+919876543210\"}");
            var sut = fixture.CreateSut();

            var details = await sut.GetCallDetailsAsync("call-uuid-1");

            Assert.Equal("call-uuid-1", details.CallUUID);
            Assert.Equal("completed", details.CallStatus);
            Assert.Equal(95, details.CallDuration);
            Assert.Equal(120, details.BillDuration);
            Assert.Equal("0.0075", details.TotalAmount);
            Assert.Equal("NORMAL_CLEARING", details.HangupCauseName);
            Assert.Equal("+919876543210", details.ToNumber);
        }

        [Fact]
        public async Task Missing_duration_fields_come_back_as_null_rather_than_zero()
        {
            // A null duration means "Plivo did not tell us"; a zero means "the call lasted no
            // time". Collapsing the two would silently corrupt call reporting.
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(HttpStatusCode.OK, "{\"call_uuid\":\"call-uuid-1\"}");
            var sut = fixture.CreateSut();

            var details = await sut.GetCallDetailsAsync("call-uuid-1");

            Assert.Null(details.CallDuration);
            Assert.Null(details.BillDuration);
        }

        [Fact]
        public async Task A_failure_status_throws_carrying_the_status_and_the_body()
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(HttpStatusCode.NotFound, "{\"error\":\"not found\"}");
            var sut = fixture.CreateSut();

            var exception = await Assert.ThrowsAsync<Exception>(() => sut.GetCallDetailsAsync("nope"));

            Assert.Contains("Plivo get call details failed", exception.Message);
            Assert.Contains("404", exception.Message);
            Assert.Contains("not found", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task A_blank_call_uuid_produces_a_double_slash_URL_rather_than_being_rejected(string callUuid)
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.GetCallDetailsAsync(callUuid);

            Assert.Equal(
                $"https://api.plivo.com/v1/Account/{fixture.AuthId}/Call//",
                fixture.Handler.TheRequest.Uri.ToString());
        }

        [Fact]
        public async Task A_call_uuid_needing_escaping_is_sent_as_given()
        {
            // Recorded rather than endorsed: the uuid is interpolated straight into the URL, so
            // a value with a slash or a query character would change the endpoint being called.
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.GetCallDetailsAsync("abc def");

            Assert.Contains("abc%20def", fixture.Handler.TheRequest.Uri.AbsoluteUri);
        }
    }

    public sealed class PlivoServiceHangupTests
    {
        [Fact]
        public async Task A_hangup_deletes_the_call_scoped_endpoint()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.HangupCallAsync("call-uuid-1");

            var request = fixture.Handler.TheRequest;
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal(
                $"https://api.plivo.com/v1/Account/{fixture.AuthId}/Call/call-uuid-1/",
                request.Uri.ToString());
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.NoContent)]
        [InlineData(HttpStatusCode.Accepted)]
        public async Task A_success_status_reports_true(HttpStatusCode status)
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(status, "{}");
            var sut = fixture.CreateSut();

            Assert.True(await sut.HangupCallAsync("call-uuid-1"));
        }

        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.Unauthorized)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public async Task A_failure_status_reports_false_rather_than_throwing(HttpStatusCode status)
        {
            // Hanging up a call that has already ended is normal, so this one swallows failures
            // by design — unlike the other two methods on the service.
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWith(status, "{}");
            var sut = fixture.CreateSut();

            Assert.False(await sut.HangupCallAsync("call-uuid-1"));
        }

        [Fact]
        public async Task A_transport_failure_still_surfaces_because_only_the_status_is_swallowed()
        {
            var fixture = new PlivoServiceFixture();
            fixture.Handler.RespondWithTransportFailure();
            var sut = fixture.CreateSut();

            await Assert.ThrowsAsync<HttpRequestException>(() => sut.HangupCallAsync("call-uuid-1"));
        }

        [Fact]
        public async Task A_hangup_carries_the_Basic_auth_header_too()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            await sut.HangupCallAsync("call-uuid-1");

            Assert.Equal(
                "Basic " + fixture.ExpectedBasicCredential,
                fixture.Handler.TheRequest.Header("Authorization"));
        }
    }

    /// <summary>
    /// <c>BuildAnswerXml</c> is the PlivoXML the provider fetches when the candidate picks up. If
    /// the attributes are wrong the call connects and then sits silent, which is the worst
    /// failure mode available — the candidate hears nothing and hangs up.
    /// </summary>
    public sealed class PlivoServiceAnswerXmlTests
    {
        private static string Xml(string ws = "wss://portal.risebird.test/stream",
            string status = "https://portal.risebird.test/plivo/stream-status") =>
            new PlivoServiceFixture().CreateSut().BuildAnswerXml(ws, status);

        [Fact]
        public void The_XML_declares_a_UTF8_document()
        {
            Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", Xml());
        }

        [Fact]
        public void The_XML_is_a_Response_wrapping_a_Stream()
        {
            var xml = Xml();

            Assert.Contains("<Response>", xml);
            Assert.Contains("</Response>", xml);
            Assert.Contains("<Stream", xml);
            Assert.Contains("</Stream>", xml);
        }

        [Fact]
        public void The_stream_is_bidirectional_and_keeps_the_call_alive()
        {
            var xml = Xml();

            Assert.Contains("bidirectional=\"true\"", xml);
            Assert.Contains("keepCallAlive=\"true\"", xml);
        }

        [Fact]
        public void The_stream_negotiates_8kHz_mulaw_which_is_what_the_realtime_model_expects()
        {
            Assert.Contains("contentType=\"audio/x-mulaw;rate=8000\"", Xml());
        }

        [Fact]
        public void The_status_callback_is_posted_to_the_given_URL()
        {
            var xml = Xml(status: "https://portal.risebird.test/plivo/status");

            Assert.Contains("statusCallbackUrl=\"https://portal.risebird.test/plivo/status\"", xml);
            Assert.Contains("statusCallbackMethod=\"POST\"", xml);
        }

        [Fact]
        public void The_web_socket_URL_is_the_element_text_not_an_attribute()
        {
            var xml = Xml(ws: "wss://portal.risebird.test/stream/42");

            Assert.Contains("wss://portal.risebird.test/stream/42", xml);
            Assert.DoesNotContain("url=\"wss://", xml);
        }

        [Fact]
        public void The_document_parses_as_XML()
        {
            var document = System.Xml.Linq.XDocument.Parse(Xml());

            Assert.Equal("Response", document.Root.Name.LocalName);
            var stream = Assert.Single(document.Root.Elements("Stream"));
            Assert.Equal("true", stream.Attribute("bidirectional").Value);
            Assert.Contains("wss://portal.risebird.test/stream", stream.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Blank_URLs_still_produce_a_parseable_document(string value)
        {
            var xml = Xml(ws: value, status: value);

            var document = System.Xml.Linq.XDocument.Parse(xml);
            Assert.Equal("Response", document.Root.Name.LocalName);
        }

        [Fact]
        public void A_URL_containing_an_ampersand_breaks_the_document_because_nothing_escapes_it()
        {
            // A real defect, pinned rather than hidden: the XML is built by interpolation, so a
            // status callback carrying a query string with & produces malformed PlivoXML and the
            // call answers to silence. Escaping the two URLs is the fix.
            var xml = Xml(status: "https://portal.risebird.test/status?a=1&b=2");

            Assert.Throws<System.Xml.XmlException>(() => System.Xml.Linq.XDocument.Parse(xml));
        }

        [Fact(Skip = SkippedOnRequest.Reason)]
        public void A_URL_containing_a_quote_injects_an_extra_attribute()
        {
            // The same unescaped-interpolation defect as above, pinned rather than hidden. A quote
            // closes statusCallbackUrl early, so the rest of the value lands as its own attribute:
            // the document still parses, which is what makes this the more dangerous of the two.
            // Escaping the two URLs is the fix.
            var xml = Xml(status: "https://portal.risebird.test/\" onload=\"x");

            var stream = System.Xml.Linq.XDocument.Parse(xml).Root.Element("Stream");
            Assert.Equal("https://portal.risebird.test/", stream.Attribute("statusCallbackUrl").Value);
            Assert.Equal("x", stream.Attribute("onload").Value);
        }

        [Fact]
        public void Building_the_XML_sends_no_HTTP_request()
        {
            var fixture = new PlivoServiceFixture();
            var sut = fixture.CreateSut();

            sut.BuildAnswerXml("wss://x", "https://y");

            Assert.Empty(fixture.Handler.Requests);
        }
    }
}
