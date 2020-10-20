using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.CommonHelper;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
    [AllowAnonymous]
    public class SurveyController : Controller
    {
        private readonly IConfiguration _configuration;
        public SurveyController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Index(int id)
        {
            SurveyVM model = new SurveyVM();
            model.interviewid = id;
            model.Type = "Candidate";
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Index(SurveyVM model)
        {
            if (ModelState.IsValid)
            {
                return View("Index", model);
            }
            try
            {
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                model.PartitionKey = date;
                model.RowKey = Guid.NewGuid().ToString();
                model.CreatedOn = DateTime.Now;
                await _azureTableStorageService.InsertEntity("RisebirdSurvey", model);
            }
            catch (Exception ex1)
            {
                Console.WriteLine(ex1.InnerException);
            }
            return View();
        }
        public IActionResult InterviewerSurvey(int id)
        {
            SurveyVM model = new SurveyVM();
            model.interviewid = id;
            model.Type = "Interviewer";
            return View("Index", model);
        }
    }
}
