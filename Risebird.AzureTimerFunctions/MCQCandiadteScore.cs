using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;
using System.Collections.Specialized;
using System.Net;
using System.Web;

namespace Risebird.AzureTimerFunctions
{
    public class MCQCandiadteScore
    {
        private readonly IConfiguration _configuration;
        private readonly IMCQService _mCQService;
        private readonly ILogger log;

        public MCQCandiadteScore(ILoggerFactory loggerFactory, IConfiguration configuration, IMCQService mCQService)
        {
            _configuration = configuration;
            _mCQService = mCQService;
            log = loggerFactory.CreateLogger<MCQCandiadteScore>();
        }
        [Function("MCQCandiadteScore")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestData req)
        {
            log.LogInformation("C# HTTP trigger MCQCandiadteScore function processed a request.");
            NameValueCollection queryParameters = HttpUtility.ParseQueryString(req.Url.Query);

            // Get the "emailid" query parameter value
            string emailid = queryParameters["Emailid"];

            //string emailid = req.Query["Emailid"];
            //string mcqid = req.Query["Mcqid"];
            string mcqid = string.Empty;
            string mcqtitle = queryParameters["McqTitle"];
            //string mcqtitle = req.Query["McqTitle"];

            if (!string.IsNullOrEmpty(mcqtitle))
            {
                mcqtitle = mcqtitle.Trim().ToUpper();
            }
            var candidateset = new CandidateDetailsForTestDuplicate();
            
            var _candidateset = (List<CandidateDetailsForTestDuplicate>)await _mCQService.GetMCQCandidateDetailsByMCQTitle(mcqtitle);
            if (!string.IsNullOrEmpty(emailid))
            {
                emailid = emailid.ToUpper();
                candidateset = _candidateset.Where(x => x.CandidateEmail.ToUpper() == emailid).FirstOrDefault();
            }
            var Questions = (List<MCQVM>)await _mCQService.GetMCQCandidateQuestions(candidateset.Id);
            candidateset.QuestionDetails = Questions;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(candidateset);
            return response;

            //return new OkObjectResult(candidateset);
        }
    }
}
