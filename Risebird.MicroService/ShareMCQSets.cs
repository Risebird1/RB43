using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Risebird.Entities.ViewModel;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.Entities.Enum;

namespace Risebird.MicroService
{
    public class ShareMCQSets
    {
        private readonly IConfiguration _configuration;
        private readonly IMCQService _mCQService;

        public ShareMCQSets(IConfiguration configuration, IMCQService mCQService)
        {
            _configuration = configuration;
            _mCQService = mCQService;
        }
        [FunctionName("ShareMCQSets")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            string name = req.Query["name"];
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            MCQApiVM data = JsonConvert.DeserializeObject<MCQApiVM>(requestBody);
            if (!string.IsNullOrEmpty(data.CandidateEmailid) && !string.IsNullOrEmpty(data.PrimarySkill))
            {
                var mcqset = _mCQService.FetchMCQSetsForApi(data);
                if (mcqset != null)
                {
                    var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    MCQVMShare _list = new MCQVMShare();
                    _list.status = 0;
                    _list.MCQSet = mcqset.Title;
                    _list.ClientHR = string.IsNullOrEmpty(data.ClientHrEmailid) ? "" : data.ClientHrEmailid; ;
                    _list.ApplyingCompany = string.IsNullOrEmpty(data.Client) ? "" : data.Client;
                    _list.WhoShared = "MCQ Bot";
                    _list.PartitionKey = date1;
                    _list.MCQId = mcqset.PartitionKey;
                    _list.EmailAddress = data.CandidateEmailid.Trim();
                    _list.roomId = "";
                    _list.Expiretime = 24;
                    _mCQService.AddMCQSetShareList(_list);
                    string currenturl = "https://portal.risebird.io/MCQ/SkillTest?Id=" + mcqset.PartitionKey;
                    return new OkObjectResult(currenturl);
                }
            }
            return new BadRequestResult();
        }
    }
}
