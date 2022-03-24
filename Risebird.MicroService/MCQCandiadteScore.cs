using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Risebird.CommonHelper;
using Risebird.Entities.ViewModel;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Collections.Generic;

namespace Risebird.MicroService
{
    public class MCQCandiadteScore
    {
        private readonly IConfiguration _configuration;

        public MCQCandiadteScore(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [FunctionName("MCQCandiadteScore")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger MCQCandiadteScore function processed a request.");
            string emailid = req.Query["Emailid"];
            string mcqid = req.Query["Mcqid"];
            var candidateset = new List<CandidateDetailsForTestDuplicate>();
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var candidateDetails = await _azureTableStorageService.GetAllEntitiesAsync<CandidateDetailsForTest>("RisebirdMCQCandidate");
            var candidate = await _azureTableStorageService.GetAllEntitiesAsync<MCQVMShare>("RisebirdMCQShareList");
            var Questions = await _azureTableStorageService.GetAllEntitiesAsync<MCQVM>("RisebirdMCQCandidateQuestions");
            if (candidateDetails != null && candidate != null)
            {
                candidate = candidate.Where(x => x.Id == mcqid).ToList();
                candidate = candidate.Where(x => x.EmailAddress == emailid).ToList();
                candidateDetails = candidateDetails.Where(x => x.Id == mcqid).ToList();
                candidateDetails = candidateDetails.Where(x => x.CandidateEmail == emailid).ToList();
                string candidateid = candidateDetails.FirstOrDefault().PartitionKey;
                //Questions = Questions.Where(x => x.CandidateID == candidateid).ToList();
                candidateset = (from o in candidate
                                join a in candidateDetails
                                on o.EmailAddress.ToUpper() equals a.CandidateEmail.ToUpper()
                                into temp
                                from a in temp.DefaultIfEmpty()
                                select new CandidateDetailsForTestDuplicate
                                {
                                    CandidateEmail = o.EmailAddress,
                                    ClientHR = o.ClientHR,
                                    ApplyingCompany = o.ApplyingCompany,
                                    TotalExprience = a == null ? "" : a.TotalExprience,
                                    Skills = a == null ? "" : a.Skills,
                                    Id = a == null ? "" : a.PartitionKey,
                                    CandidateScore = a == null ? 0 : a.CandidateScore,
                                    TotalQuestions = a == null ? 0 : a.TotalQuestions,
                                    PartitionKey = o.PartitionKey,
                                    Sharetime = o.Timestamp.DateTime,
                                    SameID = o.Id,
                                    VideoLink = a == null ? "" : a.VideoLink,
                                    Feedbackreating = a == null ? 0 : a.Feedbackreating,
                                    FeedbackComment = a == null ? "" : a.FeedbackComment,
                                    QuestionDetails = Questions.Where(x => x.CandidateID == a.PartitionKey).ToList()
                                }).ToList();
            }
            return new OkObjectResult(candidateset);
        }
    }
}
