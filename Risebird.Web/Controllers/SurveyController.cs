using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;

namespace Risebird.Web.Controllers
{
    [AllowAnonymous]
    public class SurveyController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IMeetingService _meetingService;
        private readonly INotificationHelper _notificationHelper;
        public SurveyController(IConfiguration configuration, IMeetingService meetingService, INotificationHelper notificationHelper)
        {
            _configuration = configuration;
            _meetingService = meetingService;
            _notificationHelper = notificationHelper;
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
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }
            try
            {
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                if (result.Where(x => x.interviewid == model.interviewid).Count() > 0)
                {
                    var resultset = result.Where(x => x.interviewid == model.interviewid).FirstOrDefault();
                    resultset.Question1 = model.Question1;
                    resultset.Question2 = model.Question2;
                    resultset.Question3 = model.Question3;
                    resultset.Question4 = model.Question4;
                    resultset.Question5 = model.Question5;
                    resultset.Question6 = model.Question6;
                    resultset.Name = model.Name;
                    resultset.Phone = model.Phone;
                    await _azureTableStorageService.InsertEntity("RisebirdSurvey", resultset);
                }
                else
                {
                    var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    model.PartitionKey = date;
                    model.RowKey = Guid.NewGuid().ToString();
                    model.CreatedOn = DateTime.Now;
                    await _azureTableStorageService.InsertEntity("RisebirdSurvey", model);
                    //Send mail to interviewer with candidate email address
                    //FetchCanddiateInterviewer
                    var model1 = _meetingService.FetchCanddiateInterviewer(model.interviewid);
                    Mail _mail = new Mail
                    {
                        TO = model1.Interviewer,
                        Subject = "Candidate Interview Feedback"
                    };
                    var resultsetdetails = _notificationHelper.FeedbackSubmitNotification(_mail, model1.CandidateEmail);

                }

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
