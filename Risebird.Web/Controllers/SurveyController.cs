using System;
using System.Collections.Generic;
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
                    //FetchCanddiateInterviewer
                    var model1 = _meetingService.FetchCanddiateInterviewer(model.interviewid);
                    var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    model.PartitionKey = date;
                    model.RowKey = Guid.NewGuid().ToString();
                    model.CreatedOn = DateTime.Now;
                    model.Interviewerid = model1.id;
                    model.PrimarySkill = model1.PrimarySkill;
                    await _azureTableStorageService.InsertEntity("RisebirdSurvey", model);
                    //Send mail to interviewer with candidate email address
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
        public IActionResult CandidateFeedBack()
        {
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
            var _resultlist = (from data in result
                               select new SurveyDetails { Interviewerid = data.Interviewerid }).ToList();
            _resultlist = _resultlist.Distinct().ToList();
            var resultset = _meetingService.FetchSurveyInterviewer(_resultlist);
            var model = (from pair1 in result
                         join pair2 in resultset on pair1.Interviewerid equals pair2.Interviewerid
                         select new SurveyVM
                         {
                             interviewid = pair1.interviewid,
                             Question1 = pair1.Question1,
                             Question2 = pair1.Question2,
                             Question3 = pair1.Question3,
                             Question4 = pair1.Question4,
                             Question5 = pair1.Question5,
                             Question6 = pair1.Question6,
                             Name = pair1.Name,
                             EmailAddress = pair1.EmailAddress,
                             interviewerEmail = pair2.Intervieweremail,
                             CommentforInterviewer = pair1.CommentforInterviewer,
                             PrimarySkill = pair1.PrimarySkill
                         }).ToList().Distinct();
            return View(model);
        }
    }
}
