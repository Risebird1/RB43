namespace Risebird.CommonHelper.Tests.CommonHelperLogic
{
    /// <summary>
    /// The B2C client distinguishes a successful Graph payload from an OData error purely by
    /// whether <c>odata.error</c> deserialized. These suites pin that discrimination, because
    /// mistaking an error for a value silently returns an empty user list.
    /// </summary>
    public class ODataResponseParseTests
    {
        [Fact]
        public void An_empty_object_parses_with_no_error()
        {
            var response = ODataResponse.Parse("{}");

            Assert.NotNull(response);
            Assert.Null(response.Error);
        }

        [Fact]
        public void An_odata_error_payload_populates_the_error()
        {
            const string json = """
                {"odata.error":{"code":"Request_BadRequest",
                 "message":{"lang":"en","value":"Invalid object identifier."},
                 "requestId":"7f1c8f4e-1111-2222-3333-444455556666",
                 "date":"2026-09-09T12:00:00"}}
                """;

            var response = ODataResponse.Parse(json);

            Assert.NotNull(response.Error);
            Assert.Equal("Request_BadRequest", response.Error.Code);
            Assert.Equal("en", response.Error.Message.Lang);
            Assert.Equal("Invalid object identifier.", response.Error.Message.Value);
            Assert.Equal(Guid.Parse("7f1c8f4e-1111-2222-3333-444455556666"), response.Error.RequestId);
            Assert.Equal(new DateTime(2026, 09, 09, 12, 00, 00), response.Error.Date);
        }

        [Fact]
        public void An_error_payload_carries_its_detail_values()
        {
            const string json = """
                {"odata.error":{"code":"Request_BadRequest",
                 "values":[{"item":"PropertyName","value":"userPrincipalName"},
                           {"item":"PropertyErrorCode","value":"NullValue"}]}}
                """;

            var response = ODataResponse.Parse(json);

            var values = response.Error.Values.ToList();
            Assert.Equal(2, values.Count);
            Assert.Equal("PropertyName", values[0].Item);
            Assert.Equal("userPrincipalName", values[0].Value);
            Assert.Equal("NullValue", values[1].Value);
        }

        [Fact]
        public void A_success_payload_leaves_the_error_null()
        {
            var response = ODataResponse.Parse("""{"value":[{"objectId":"abc"}]}""");

            Assert.Null(response.Error);
        }

        [Fact]
        public void Parsing_null_yields_null()
        {
            Assert.Null(ODataResponse.Parse("null"));
        }

        [Theory]
        [InlineData("not json")]
        [InlineData("{")]
        public void Malformed_json_throws_so_the_caller_cannot_mistake_it_for_a_success(string json)
        {
            // Pinned deliberately: Parse does not swallow deserialization failures, so a
            // transport-level corruption surfaces instead of looking like an empty response.
            Assert.ThrowsAny<Exception>(() => ODataResponse.Parse(json));
        }

        [Fact]
        public void An_empty_body_yields_null_rather_than_throwing()
        {
            // A zero-length body is indistinguishable from "no error" downstream, so callers
            // must null-check the parse result.
            Assert.Null(ODataResponse.Parse(string.Empty));
        }
    }

    public class ODataResponseDtoParseTests
    {
        private sealed class GraphUser
        {
            public string ObjectId { get; set; }
            public string DisplayName { get; set; }
        }

        [Fact]
        public void An_error_payload_is_returned_as_the_error_with_no_value()
        {
            var response = ODataResponseDto<GraphUser>.Parse(
                """{"odata.error":{"code":"Authorization_RequestDenied"}}""");

            Assert.NotNull(response.Error);
            Assert.Equal("Authorization_RequestDenied", response.Error.Code);
            Assert.Null(response.Value);
        }

        [Fact]
        public void An_error_payload_short_circuits_before_the_value_is_deserialized()
        {
            // The second deserialization pass never runs on an error body, which is what keeps
            // an error payload from being mis-read as a value of type T.
            var response = ODataResponseDto<GraphUser>.Parse(
                """{"odata.error":{"code":"Request_BadRequest","message":{"lang":"en","value":"bad"}}}""");

            Assert.Null(response.Value);
            Assert.Equal("bad", response.Error.Message.Value);
        }

        [Fact]
        public void A_success_payload_is_deserialized_into_the_value()
        {
            var response = ODataResponseDto<GraphUser>.Parse(
                """{"objectId":"abc","displayName":"Ada"}""");

            Assert.Null(response.Error);
            Assert.NotNull(response.Value);
            Assert.Equal("abc", response.Value.ObjectId);
            Assert.Equal("Ada", response.Value.DisplayName);
        }

        [Fact]
        public void An_empty_object_yields_a_value_with_default_members()
        {
            var response = ODataResponseDto<GraphUser>.Parse("{}");

            Assert.Null(response.Error);
            Assert.NotNull(response.Value);
            Assert.Null(response.Value.ObjectId);
        }

        [Fact]
        public void Unknown_members_are_ignored_rather_than_failing_the_parse()
        {
            var response = ODataResponseDto<GraphUser>.Parse(
                """{"objectId":"abc","somethingGraphAdded":123}""");

            Assert.Null(response.Error);
            Assert.Equal("abc", response.Value.ObjectId);
        }

        [Fact]
        public async Task Requesting_the_next_page_without_a_next_link_returns_null()
        {
            var response = ODataResponseDto<GraphUser>.Parse("""{"objectId":"abc"}""");

            Assert.Null(await response.GetNextPage());
        }

        [Fact]
        public async Task Requesting_the_next_page_without_a_prepared_client_throws()
        {
            // PreparePaging is internal and only the B2C client calls it, so a DTO parsed in
            // isolation has no client to page with. Pinned so the failure mode is known.
            var response = new ODataResponseDto<GraphUser> { NextLink = "users?$skiptoken=X" };

            await Assert.ThrowsAnyAsync<Exception>(() => response.GetNextPage());
        }

        [Fact]
        public void A_next_link_survives_on_a_manually_built_dto()
        {
            var response = new ODataResponseDto<GraphUser> { NextLink = "users?$skiptoken=X" };

            Assert.Equal("users?$skiptoken=X", response.NextLink);
        }
    }

    public class ODataErrorDtoShapeTests
    {
        [Fact]
        public void Default_error_has_no_code_message_or_values()
        {
            var error = new ODataErrorDto();

            Assert.Null(error.Code);
            Assert.Null(error.Message);
            Assert.Null(error.Values);
            Assert.Equal(Guid.Empty, error.RequestId);
            Assert.Equal(default(DateTime), error.Date);
        }

        [Fact]
        public void Error_message_holds_a_language_and_a_value()
        {
            var message = new ODataErrorMessageDto { Lang = "en", Value = "boom" };

            Assert.Equal("en", message.Lang);
            Assert.Equal("boom", message.Value);
        }

        [Fact]
        public void Error_value_holds_an_item_and_a_value()
        {
            var value = new ODataErrorValueDto { Item = "PropertyName", Value = "mail" };

            Assert.Equal("PropertyName", value.Item);
            Assert.Equal("mail", value.Value);
        }
    }
}
