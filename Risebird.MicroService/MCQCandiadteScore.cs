using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Risebird.Business.Interface;
using Risebird.Entities.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.MicroService
{
    public class MCQCandiadteScore
    {
        private readonly IConfiguration _configuration;
        private readonly IMCQService _mCQService;

        public MCQCandiadteScore(IConfiguration configuration, IMCQService mCQService)
        {
            _configuration = configuration;
            _mCQService = mCQService;
        }
        [FunctionName("MCQCandiadteScore")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger MCQCandiadteScore function processed a request.");
            string emailid = req.Query["Emailid"];
            //string mcqid = req.Query["Mcqid"];
            string mcqid = string.Empty;
            string mcqtitle = req.Query["McqTitle"];
            if (!string.IsNullOrEmpty(mcqtitle))
            {
                mcqtitle = mcqtitle.Trim().ToUpper();
            }
            var candidateset = new CandidateDetailsForTestDuplicate();
            //AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            //var _mcqsets = await _azureTableStorageService.GetAllEntitiesAsync<MCQVMset>("RisebirdMCQSet");
            //_mcqsets = _mcqsets.Where(x => x.Title.ToUpper() == mcqtitle);
            //if (_mcqsets.Count() > 0)
            //{
            //    mcqid = _mcqsets.FirstOrDefault().PartitionKey;
            //}
            var _candidateset = (List<CandidateDetailsForTestDuplicate>)await _mCQService.GetMCQCandidateDetailsByMCQTitle(mcqtitle);
            if (!string.IsNullOrEmpty(emailid))
            {
                emailid = emailid.ToUpper();
                candidateset = _candidateset.Where(x => x.CandidateEmail.ToUpper() == emailid).FirstOrDefault();
            }
            var Questions = (List<MCQVM>)await _mCQService.GetMCQCandidateQuestions(candidateset.Id);
            candidateset.QuestionDetails = Questions;
            //var candidateDetails = await _azureTableStorageService.GetAllEntitiesAsync<CandidateDetailsForTest>("RisebirdMCQCandidate");
            //var candidate = await _azureTableStorageService.GetAllEntitiesAsync<MCQVMShare>("RisebirdMCQShareList");
            //var Questions = await _azureTableStorageService.GetAllEntitiesAsync<MCQVM>("RisebirdMCQCandidateQuestions");
            //if (candidateDetails != null && candidate != null)
            //{
            //    //candidate = candidate.Where(x => x.Id == mcqid).ToList();
            //    candidate = candidate.Where(x => x.MCQId == mcqid).ToList();
            //    candidate = candidate.Where(x => x.EmailAddress == emailid).ToList();
            //    //candidateDetails = candidateDetails.Where(x => x.Id == mcqid).ToList();
            //    candidateDetails = candidateDetails.Where(x => x.MCQId == mcqid).ToList();
            //    candidateDetails = candidateDetails.Where(x => x.CandidateEmail == emailid).ToList();
            //    string candidateid = candidateDetails.FirstOrDefault().PartitionKey;
            //    //Questions = Questions.Where(x => x.CandidateID == candidateid).ToList();
            //    candidateset = (from o in candidate
            //                    join a in candidateDetails
            //                    on o.EmailAddress.ToUpper() equals a.CandidateEmail.ToUpper()
            //                    into temp
            //                    from a in temp.DefaultIfEmpty()
            //                    select new CandidateDetailsForTestDuplicate
            //                    {
            //                        CandidateEmail = o.EmailAddress,
            //                        ClientHR = o.ClientHR,
            //                        ApplyingCompany = o.ApplyingCompany,
            //                        TotalExprience = a == null ? "" : a.TotalExprience,
            //                        Skills = a == null ? "" : a.Skills,
            //                        Id = a == null ? "" : a.PartitionKey,
            //                        CandidateScore = a == null ? 0 : a.CandidateScore,
            //                        TotalQuestions = a == null ? 0 : a.TotalQuestions,
            //                        PartitionKey = o.PartitionKey,
            //                        Sharetime = o.Timestamp.DateTime,
            //                        SameID = o.MCQId,
            //                        VideoLink = a == null ? "" : a.VideoLink,
            //                        Feedbackreating = a == null ? 0 : a.Feedbackreating,
            //                        FeedbackComment = a == null ? "" : a.FeedbackComment,
            //                        QuestionDetails = Questions.Where(x => x.CandidateID == a.PartitionKey).ToList()
            //                    }).ToList();
            //}
            return new OkObjectResult(candidateset);
        }
    }
}
