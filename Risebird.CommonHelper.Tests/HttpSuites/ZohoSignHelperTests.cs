using Microsoft.Extensions.Options;
using Moq;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.Entities.Data;
using System.Security.Cryptography;
using System.Text;

namespace Risebird.CommonHelper.Tests.HttpSuites
{
    public class ZohoSignHelperTests
    {
        private const string Payload = "{\"requests\":{\"request_id\":\"123\"}}";

        private static ZohoSignHelper CreateSut(string webhookSecret) =>
            new(new HttpClient(), Options.Create(new ZohoSignOptions { WebhookSecret = webhookSecret }), Mock.Of<IInterviewSchedulerService>());

        private static string Sign(string secret, string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        [Fact]
        public void Webhook_signature_matching_the_secret_is_accepted()
        {
            Assert.True(CreateSut("s3cret").IsWebhookSignatureValid(Payload, Sign("s3cret", Payload)));
        }

        [Fact]
        public void Webhook_signature_from_another_secret_or_tampered_body_is_rejected()
        {
            var sut = CreateSut("s3cret");
            Assert.False(sut.IsWebhookSignatureValid(Payload, Sign("other", Payload)));
            Assert.False(sut.IsWebhookSignatureValid(Payload.Replace("123", "999"), Sign("s3cret", Payload)));
            Assert.False(sut.IsWebhookSignatureValid(Payload, ""));
        }

        [Fact]
        public void Webhook_signature_check_is_off_when_no_secret_is_configured()
        {
            Assert.True(CreateSut("").IsWebhookSignatureValid(Payload, null));
        }

        [Fact]
        public async Task Send_uploads_the_pdf_then_submits_layout_fields_as_page_percentages()
        {
            var handler = new StubHandler(
                "{\"code\":0,\"requests\":{\"request_id\":\"R1\",\"document_ids\":[{\"document_id\":\"D1\"}],\"actions\":[{\"action_id\":\"A1\",\"recipient_email\":\"asha@test.com\"},{\"action_id\":\"A2\",\"recipient_email\":\"sign@risebird.io\"}]}}",
                "{\"code\":0,\"requests\":{\"request_status\":\"inprogress\"}}");
            var scheduler = new Mock<IInterviewSchedulerService>();
            scheduler.Setup(s => s.FetchZohoAccessToken(It.IsAny<string>()))
                .ReturnsAsync(new Entities.ViewModel.ZohoAccessToken { AccessToken = "tok", TokenExpiry = DateTime.Now.AddHours(1) });
            var sut = new ZohoSignHelper(new HttpClient(handler),
                Options.Create(new ZohoSignOptions { CompanySignerName = "Pradeep Kumar", CompanySignerEmail = "sign@risebird.io" }), scheduler.Object);

            var document = new Entities.ViewModel.AgreementDocument
            {
                FileName = "agreement.pdf",
                Pdf = new byte[] { 1, 2, 3 },
                PageSizes = { (600f, 800f), (600f, 800f) },
                Layout = new Entities.ViewModel.AgreementDocumentLayout
                {
                    Fields =
                    {
                        new() { Signer = "Interviewer", Type = "Textfield", Label = "Address", Page = 1, X = 60, Y = 80, Width = 300, Height = 16 },
                        new() { Signer = "Interviewer", Type = "Signature", Label = "Interviewer Signature", Page = 2, X = 60, Y = 400, Width = 150, Height = 24 },
                        new() { Signer = "Company", Type = "Signature", Label = "Company Signature", Page = 2, X = 60, Y = 300, Width = 150, Height = 24 }
                    }
                }
            };

            var result = await sut.SendInterviewerAgreementAsync(
                new Entities.ViewModel.ZohoSignAgreementRequest { RecipientName = "Asha Rao", RecipientEmail = "asha@test.com" }, document);

            Assert.Equal("R1", result.RequestId);
            Assert.Equal("A1", result.ActionId);
            Assert.EndsWith("/requests", handler.Requests[0].Url);
            Assert.Contains("asha@test.com", handler.Requests[0].Body);
            Assert.EndsWith("/requests/R1/submit", handler.Requests[1].Url);

            var data = System.Web.HttpUtility.ParseQueryString(handler.Requests[1].Body)["data"];
            using var submit = System.Text.Json.JsonDocument.Parse(data);
            var actions = submit.RootElement.GetProperty("requests").GetProperty("actions");
            Assert.Equal(2, actions.GetArrayLength());
            var interviewer = actions[0];
            Assert.Equal("A1", interviewer.GetProperty("action_id").GetString());
            var address = interviewer.GetProperty("fields")[0];
            Assert.Equal("D1", address.GetProperty("document_id").GetString());
            Assert.Equal(0, address.GetProperty("page_no").GetInt32());
            Assert.Equal("10", address.GetProperty("x_value").GetString());   // 60 / 600
            Assert.Equal("10", address.GetProperty("y_value").GetString());   // 80 / 800
            Assert.Equal("50", address.GetProperty("width").GetString());     // 300 / 600
            Assert.Equal(1, interviewer.GetProperty("fields")[1].GetProperty("page_no").GetInt32());
            Assert.Equal("A2", actions[1].GetProperty("action_id").GetString());
            Assert.Single(actions[1].GetProperty("fields").EnumerateArray());
        }

        private class StubHandler : HttpMessageHandler
        {
            private readonly Queue<string> _responses;
            public List<(string Url, string Body)> Requests { get; } = new();
            public StubHandler(params string[] responses) => _responses = new Queue<string>(responses);

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
                Requests.Add((request.RequestUri!.ToString(), body));
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(_responses.Dequeue()) };
            }
        }
    }
}
