using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OfficeOpenXml;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Risebird.Entities.Data.RchilliResumeMapFields;

namespace Risebird.Web.Controllers
{
    [AllowAnonymous]
    public class SurveyController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IMeetingService _meetingService;
        private readonly INotificationHelper _notificationHelper;
        private readonly IUserRepository _userRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAadhaarService _aadhaarService;
        private readonly ISurveyService _surveyService;
        public SurveyController(IConfiguration configuration, IMeetingService meetingService, INotificationHelper notificationHelper, IUserRepository userRepository, IApplicationErrorLogger applicationErrorLogger, IAadhaarService aadhaarService, ISurveyService surveyService)
        {
            _configuration = configuration;
            _meetingService = meetingService;
            _notificationHelper = notificationHelper;
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _aadhaarService = aadhaarService;
            _surveyService = surveyService;
        }

        [HttpGet]
        public IActionResult Index(int id)
        {
            SurveyVM model = new SurveyVM();
            model.interviewid = id;
            model.Type = "Candidate";
            ViewBag.Status = "";
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
                var results = await _surveyService.AddOrEditCandidateRating(model);
                if (!results.IsUpdated)
                {
                    Mail _mail = new Mail
                    {
                        TO = results.Interviewer,
                        Subject = "Candidate Interview Feedback"
                    };
                    var resultsetdetails = await _notificationHelper.FeedbackSubmitNotification(_mail, results.CandidateName);
                }
                //AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                //var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                //if (result.Where(x => x.interviewid == model.interviewid).Count() > 0)
                //{
                //    var resultset = result.Where(x => x.interviewid == model.interviewid).FirstOrDefault();
                //    resultset.Question1 = model.Question1;
                //    resultset.Question2 = model.Question2;
                //    resultset.Question3 = model.Question3;
                //    resultset.Question4 = model.Question4;
                //    resultset.Question5 = model.Question5;
                //    resultset.Question6 = model.Question6;
                //    resultset.Name = model.Name;
                //    resultset.Phone = model.Phone;
                //    await _azureTableStorageService.InsertEntity("RisebirdSurvey", resultset);
                //}
                //else
                //{
                //    //FetchCanddiateInterviewer
                //    var model1 = _meetingService.FetchCanddiateInterviewer(model.interviewid);
                //    var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                //    model.PartitionKey = date;
                //    model.RowKey = Guid.NewGuid().ToString();
                //    model.CreatedOn = DateTime.Now;
                //    model.Interviewerid = model1.id;
                //    model.PrimarySkill = model1.PrimarySkill;
                //    await _azureTableStorageService.InsertEntity("RisebirdSurvey", model);
                //    //Send mail to interviewer with candidate email address
                //    Mail _mail = new Mail
                //    {
                //        TO = model1.Interviewer,
                //        Subject = "Candidate Interview Feedback"
                //    };
                //    var resultsetdetails = await _notificationHelper.FeedbackSubmitNotification(_mail, model1.CandidateName);
                //}
            }
            catch (Exception ex1)
            {
                Console.WriteLine(ex1.InnerException);
            }
            ViewBag.Status = "Complete";
            return View();
        }

        [HttpGet]
        public IActionResult InterviewerSurvey(int id)
        {
            SurveyVM model = new SurveyVM();
            model.interviewid = id;
            model.Type = "Interviewer";
            return View("Index", model);
        }

        [HttpGet]
        public async Task<IActionResult> CandidateFeedBack(string type)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                //var modelData = new List<SurveyVM>();
                int Clientid = 0;
                ViewBag.profileName = profileName;
                ViewBag.type = "Client";
                ViewBag.PageName = "Interview";
                if (profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin" || profileName == "Admin")
                {
                    if (type == "Candidate")
                    {
                        ViewBag.type = "Candidate";
                        var modelData = await _surveyService.GetCandidateRating(emailid, profileName);

                        if (HttpContext.Session.GetString("CandidateFeedBackfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("CandidateFeedBackfilters"));
                            modelData = modelData.Where(x => x.Interviewdate >= filter.StartDate && x.Interviewdate <= filter.EndDate).ToList();
                            filter.ProfileSource = filter.ProfileSource == null ? filter.ProfileSource = modelData.Select(x => x.PrimarySkill).ToArray() : filter.ProfileSource = filter.ProfileSource;
                            filter.HREmail = filter.HREmail == null ? filter.HREmail = modelData.Select(x => x.Type).ToArray() : filter.HREmail = filter.HREmail;
                            modelData = modelData.Where(x => filter.ProfileSource.Contains(x.PrimarySkill)).ToList();
                            modelData = modelData.Where(x => filter.HREmail.Contains(x.Type)).ToList();
                        }
                        else
                        {
                            var startdate = DateTime.Now.AddDays(-15);
                            var enddate = DateTime.Now;
                            modelData = modelData.Where(x => x.Interviewdate >= startdate && x.Interviewdate <= enddate).ToList();
                        }
                        ViewBag.IRFId = "0";
                        return View(modelData);
                    }
                    else
                    {
                        var results = await _meetingService.FetchFeedbackDetails();
                        if (results != null)
                        {
                            results = results.Where(x => x.StarRating < 5).ToList();
                        }
                        if (HttpContext.Session.GetString("CandidateFeedBackfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("CandidateFeedBackfilters"));
                            results = results.Where(x => x.interviewdate >= filter.StartDate && x.interviewdate <= filter.EndDate).ToList();
                        }
                        else
                        {
                            var startdate = DateTime.Now.AddDays(-15);
                            var enddate = DateTime.Now;
                            results = results.Where(x => x.interviewdate >= startdate && x.interviewdate <= enddate).ToList();
                        }
                        return View("FeedBack", results);
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [HttpPost]
        public IActionResult CandidateFeedBack(CandidateTimelineFilter filter, string type)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("CandidateFeedBackfilters", JsonConvert.SerializeObject(filter));
                }
                return RedirectToAction("CandidateFeedBack", new { type = type });
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetCandidateFeedBackFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("CandidateFeedBackfilters");
                    return RedirectToAction("CandidateFeedBack");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("UnassignedInterviews");
            }

        }

        [HttpGet]
        public async Task<IActionResult> CandidateFeedBackByIRFId(string IRFId, string type)
        {
            ViewBag.type = "Client";
            ViewBag.PageName = "IRF";
            ViewBag.IRFId = IRFId;
            if (type == "Candidate")
            {
                ViewBag.type = "Candidate";
                //var modelData = new List<SurveyVM>();
                var modelData = await _surveyService.GetCandidateRatingByIRFId(IRFId);
                //AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                //var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                //var _resultlist = (from data in result
                //                   select new SurveyDetails { Interviewerid = data.Interviewerid }).ToList();
                //_resultlist = _resultlist.Distinct().ToList();
                //var resultset = await _meetingService.FetchSurveyInterviewer(_resultlist);
                //var interviewerids = await _meetingService.FetchAllInterviewidfromIrf(IRFId);
                //ViewBag.profileName = "";
                //foreach (var item in result)
                //{
                //    if (interviewerids.Any(x => x == item.interviewid))
                //    {
                //        if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                //        {
                //            modelData.Add(new SurveyVM
                //            {
                //                interviewid = item.interviewid,
                //                Question1 = item.Question1,
                //                Question2 = item.Question2,
                //                Question3 = item.Question3,
                //                Question4 = item.Question4,
                //                Question5 = item.Question5,
                //                Question6 = item.Question6,
                //                Name = item.Name,
                //                EmailAddress = item.EmailAddress,
                //                interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                //                CommentforInterviewer = item.CommentforInterviewer,
                //                PrimarySkill = item.PrimarySkill
                //            });
                //        }
                //    }
                //}

                return View("CandidateFeedBack", modelData);
            }
            else
            {
                var results = await _meetingService.FetchFeedbackDetailsByIRFId(IRFId);
                return View("FeedBack", results);
            }
        }

        [HttpGet]
        public async Task<FileContentResult> CandidateFeedbackExporttoExcel()
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Feedback");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_CandidateFeedback.xlsx";
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var modelData = await _surveyService.GetCandidateRating(emailid, profileName);
                    //if (profileName == "Client")
                    //{
                    //    var client = _userRepository.GetClientUserDetails(emailid);
                    //    Clientid = client.Result.ClientId;
                    //}
                    //var interviewerids = _meetingService.FetchAllInterviewidByClient(Clientid, emailid);
                    //var modelData = new List<SurveyVM>();
                    //AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                    //var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                    //var _resultlist = (from data in result
                    //                   select new SurveyDetails { Interviewerid = data.Interviewerid }).ToList();
                    //_resultlist = _resultlist.Distinct().ToList();
                    //var resultset = await _meetingService.FetchSurveyInterviewer(_resultlist);
                    //if (profileName == "Client")
                    //{
                    //    foreach (var item in result)
                    //    {
                    //        if (interviewerids.Any(x => x == item.interviewid))
                    //        {
                    //            if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                    //            {
                    //                modelData.Add(new SurveyVM
                    //                {
                    //                    interviewid = item.interviewid,
                    //                    Question1 = item.Question1,
                    //                    Question2 = item.Question2,
                    //                    Question3 = item.Question3,
                    //                    Question4 = item.Question4,
                    //                    Question5 = item.Question5,
                    //                    Question6 = item.Question6,
                    //                    Name = item.Name,
                    //                    EmailAddress = item.EmailAddress,
                    //                    interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                    //                    CommentforInterviewer = item.CommentforInterviewer,
                    //                    PrimarySkill = item.PrimarySkill
                    //                });
                    //            }
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    foreach (var item in result)
                    //    {
                    //        if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                    //        {
                    //            modelData.Add(new SurveyVM
                    //            {
                    //                interviewid = item.interviewid,
                    //                Question1 = item.Question1,
                    //                Question2 = item.Question2,
                    //                Question3 = item.Question3,
                    //                Question4 = item.Question4,
                    //                Question5 = item.Question5,
                    //                Question6 = item.Question6,
                    //                Name = item.Name,
                    //                EmailAddress = item.EmailAddress,
                    //                interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                    //                CommentforInterviewer = item.CommentforInterviewer,
                    //                PrimarySkill = item.PrimarySkill
                    //            });
                    //        }
                    //    }
                    //}
                    if (HttpContext.Session.GetString("CandidateTimelinefilters") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("CandidateTimelinefilters"));
                        filter.ProfileSource = filter.ProfileSource == null ? filter.ProfileSource = modelData.Select(x => x.PrimarySkill).ToArray() : filter.ProfileSource = filter.ProfileSource;
                        modelData = modelData.Where(x => filter.ProfileSource.Contains(x.PrimarySkill)).ToList();
                    }
                    Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                    Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                    Sheet.Cells["A1:Z1"].Style.WrapText = true;
                    //Sheet.Column(1).Width = 30;
                    //Sheet.Column(2).Width = 30;
                    int startColumn = 1; int endColumn = 25;
                    for (int a = startColumn; a <= endColumn; a++)
                    {
                        Sheet.Column(a).Width = 30;
                    }
                    Sheet.Cells[1, 1].Value = "Name";
                    Sheet.Cells[1, 2].Value = "Candidate Email";
                    Sheet.Cells[1, 3].Value = "Interviewer";
                    Sheet.Cells[1, 4].Value = "Primary Skill";
                    Sheet.Cells[1, 5].Value = "Overall, how would you rate the video interview experience?";
                    Sheet.Cells[1, 6].Value = "How would you rate the evaluation process of interviewer?";
                    Sheet.Cells[1, 7].Value = "Given a chance would you like to reappear for the video interview?";
                    Sheet.Cells[1, 8].Value = "What did you like about the video interviews event?";
                    Sheet.Cells[1, 9].Value = " did you dislike about the video interviews event?";
                    Sheet.Cells[1, 10].Value = "How friendly was the Risebird staff from scheduling to completion of interviews ?";
                    Sheet.Cells[1, 11].Value = "Please share comment for Interviewer";
                    int i = 2;
                    if (modelData.Count() > 0)
                    {
                        foreach (var item in modelData)
                        {
                            Sheet.Cells[i, 1].Value = item.Name;
                            Sheet.Cells[i, 2].Value = item.EmailAddress;
                            Sheet.Cells[i, 3].Value = item.interviewerEmail;
                            Sheet.Cells[i, 4].Value = item.PrimarySkill;
                            Sheet.Cells[i, 5].Value = item.Question1;
                            Sheet.Cells[i, 6].Value = item.Question2;
                            if (item.Question3 == "1")
                            {
                                Sheet.Cells[i, 7].Value = "I am satisfied with the interviewer and evaluation process";
                            }
                            else
                            {
                                Sheet.Cells[i, 7].Value = "I didn't get a fair chance and I can do better";
                            }
                            Sheet.Cells[i, 8].Value = item.Question4;
                            Sheet.Cells[i, 9].Value = item.Question5;
                            Sheet.Cells[i, 10].Value = item.Question6;
                            Sheet.Cells[i, 11].Value = item.CommentforInterviewer;
                            i++;
                        }
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                    else
                    {
                        ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                        Rng.Value = "Data export failed !!!";
                        return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                    }
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }

        [HttpGet]
        public async Task<FileContentResult> CandidateFeedBackByIRFIdExporttoExcel(string IRFId)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Feedback");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_CandidateFeedback.xlsx";
            try
            {
                //var modelData = new List<SurveyVM>();
                var modelData = await _surveyService.GetCandidateRatingByIRFId(IRFId);
                //AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                //var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                //var _resultlist = (from data in result
                //                   select new SurveyDetails { Interviewerid = data.Interviewerid }).ToList();
                //_resultlist = _resultlist.Distinct().ToList();
                //var resultset = await _meetingService.FetchSurveyInterviewer(_resultlist);
                //var interviewerids = await _meetingService.FetchAllInterviewidfromIrf(IRFId);
                //foreach (var item in result)
                //{
                //    if (interviewerids.Any(x => x == item.interviewid))
                //    {
                //        if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                //        {
                //            modelData.Add(new SurveyVM
                //            {
                //                interviewid = item.interviewid,
                //                Question1 = item.Question1,
                //                Question2 = item.Question2,
                //                Question3 = item.Question3,
                //                Question4 = item.Question4,
                //                Question5 = item.Question5,
                //                Question6 = item.Question6,
                //                Name = item.Name,
                //                EmailAddress = item.EmailAddress,
                //                interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                //                CommentforInterviewer = item.CommentforInterviewer,
                //                PrimarySkill = item.PrimarySkill
                //            });
                //        }
                //    }
                //}
                Sheet.Cells["A1:Z1"].Style.Font.Bold = true;
                Sheet.Cells["A1:Z1"].Style.Font.Size = 12;
                Sheet.Cells["A1:Z1"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 25;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 30;
                }
                Sheet.Cells[1, 1].Value = "Name";
                Sheet.Cells[1, 2].Value = "Candidate Email";
                Sheet.Cells[1, 3].Value = "Interviewer";
                Sheet.Cells[1, 4].Value = "Primary Skill";
                Sheet.Cells[1, 5].Value = "Overall, how would you rate the video interview experience?";
                Sheet.Cells[1, 6].Value = "How would you rate the evaluation process of interviewer?";
                Sheet.Cells[1, 7].Value = "Given a chance would you like to reappear for the video interview?";
                Sheet.Cells[1, 8].Value = "What did you like about the video interviews event?";
                Sheet.Cells[1, 9].Value = " did you dislike about the video interviews event?";
                Sheet.Cells[1, 10].Value = "How friendly was the Risebird staff from scheduling to completion of interviews ?";
                Sheet.Cells[1, 11].Value = "Please share comment for Interviewer";
                int i = 2;
                if (modelData.Count() > 0)
                {
                    foreach (var item in modelData)
                    {
                        Sheet.Cells[i, 1].Value = item.Name;
                        Sheet.Cells[i, 2].Value = item.EmailAddress;
                        Sheet.Cells[i, 3].Value = item.interviewerEmail;
                        Sheet.Cells[i, 4].Value = item.PrimarySkill;
                        Sheet.Cells[i, 5].Value = item.Question1;
                        Sheet.Cells[i, 6].Value = item.Question2;
                        if (item.Question3 == "1")
                        {
                            Sheet.Cells[i, 7].Value = "I am satisfied with the interviewer and evaluation process";
                        }
                        else
                        {
                            Sheet.Cells[i, 7].Value = "I didn't get a fair chance and I can do better";
                        }
                        Sheet.Cells[i, 8].Value = item.Question4;
                        Sheet.Cells[i, 9].Value = item.Question5;
                        Sheet.Cells[i, 10].Value = item.Question6;
                        Sheet.Cells[i, 11].Value = item.CommentforInterviewer;
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "Data export failed !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }

        [HttpGet]
        public async Task<JsonResult> FetchCandidateFeedBackDetails()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                //int Clientid = 0;
                //if (profileName == "Client")
                //{
                //    var client = await _userRepository.GetClientUserDetails(emailid);
                //    Clientid = client.ClientId;
                //}
                var modelData = await _surveyService.GetCandidateRating(emailid, profileName);
                //var modelData = new List<SurveyVM>();
                //AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                //var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                //if (profileName == "Client")
                //{
                //    var interviewerids = _meetingService.FetchAllInterviewidByClient(Clientid, emailid);
                //    foreach (var item in result)
                //    {
                //        if (interviewerids.Any(x => x == item.interviewid))
                //        {
                //            if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                //            {
                //                modelData.Add(new SurveyVM
                //                {
                //                    interviewid = item.interviewid,
                //                    Question1 = item.Question1,
                //                    Question2 = item.Question2,
                //                    Question3 = item.Question3,
                //                    Question4 = item.Question4,
                //                    Question5 = item.Question5,
                //                    Question6 = item.Question6,
                //                    Name = item.Name,
                //                    EmailAddress = item.EmailAddress,
                //                    CommentforInterviewer = item.CommentforInterviewer,
                //                    PrimarySkill = item.PrimarySkill,
                //                });
                //            }
                //        }
                //    }
                //}
                //else if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                //{
                //    var interviewerids = _meetingService.FetchAllInterviewidBySeniorHR(emailid);
                //    foreach (var item in result)
                //    {
                //        if (interviewerids.Any(x => x == item.interviewid))
                //        {
                //            if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                //            {
                //                modelData.Add(new SurveyVM
                //                {
                //                    interviewid = item.interviewid,
                //                    Question1 = item.Question1,
                //                    Question2 = item.Question2,
                //                    Question3 = item.Question3,
                //                    Question4 = item.Question4,
                //                    Question5 = item.Question5,
                //                    Question6 = item.Question6,
                //                    Name = item.Name,
                //                    EmailAddress = item.EmailAddress,
                //                    CommentforInterviewer = item.CommentforInterviewer,
                //                    PrimarySkill = item.PrimarySkill,
                //                });
                //            }
                //        }
                //    }
                //}
                //else
                //{
                //    foreach (var item in result)
                //    {
                //        if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                //        {
                //            modelData.Add(new SurveyVM
                //            {
                //                interviewid = item.interviewid,
                //                Question1 = item.Question1,
                //                Question2 = item.Question2,
                //                Question3 = item.Question3,
                //                Question4 = item.Question4,
                //                Question5 = item.Question5,
                //                Question6 = item.Question6,
                //                Name = item.Name,
                //                EmailAddress = item.EmailAddress,
                //                CommentforInterviewer = item.CommentforInterviewer,
                //                PrimarySkill = item.PrimarySkill,
                //            });
                //        }
                //    }
                //}
                if (modelData != null && modelData.Count() > 0)
                {
                    var avgQ1 = modelData.Average(x => string.IsNullOrEmpty(x.Question1) ? 0 : Convert.ToInt32(x.Question1)).ToString("#.##");
                    var avgQ2 = modelData.Average(x => string.IsNullOrEmpty(x.Question2) ? 0 : Convert.ToInt32(x.Question2)).ToString("#.##");
                    var avgQ6 = modelData.Average(x => string.IsNullOrEmpty(x.Question6) ? 0 : Convert.ToInt32(x.Question6)).ToString("#.##");
                    return Json(new { avgQ1 = avgQ1, avgQ2 = avgQ2, avgQ6 = avgQ6 });
                }
                else
                {
                    return Json("Fail");
                }
            }
            else
            {
                return Json("Fail");
            }
        }

        [HttpGet]
        public async Task<IActionResult> InterviewsReview(string uniqueid)
        {
            var _list = await _aadhaarService.FetchCandidateEmailidByUniqueId(uniqueid);
            ViewBag.Emailid = _list.EmailId;
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> SaveInterviewsReview(int rating, string comments, string Emailid)
        {
            if (string.IsNullOrWhiteSpace(comments))
            {
                comments = "";
            }
            var resultset = await _meetingService.SaveInterviewsReview(rating, comments, Emailid);
            return Json("success");
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ClientInterviewsReview()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                if (profileName == "Admin" || profileName == "SupportAdmin")
                {
                    var model = await _meetingService.FetchInterviewsReview();
                    ViewBag.Clients = model.Select(x => x.ClientName).Distinct().ToList();
                    if (HttpContext.Session.GetString("ClientInterviewReviewList") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("ClientInterviewReviewList"));
                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            model = model.Where(x => x.CreatedOn >= filter.StartDate && x.CreatedOn < filter.EndDate).ToList();
                        }
                        filter.Client = filter.Client == null ? filter.Client = model.Select(x => x.ClientName).ToArray() : filter.Client = filter.Client;
                        model = model.Where(c => (filter.Client.Count() > 0 ? filter.Client.Contains(c.ClientName) : c.ClientName == null)).ToList();
                    }
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [Authorize]
        [HttpPost]
        public IActionResult ClientInterviewsReview(InterviewFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                if (filter != null)
                {
                    HttpContext.Session.SetString("ClientInterviewReviewList", JsonConvert.SerializeObject(filter));
                }

                return RedirectToAction("ClientInterviewsReview");
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpGet]
        public ActionResult ResetClientInterviewsReview()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                HttpContext.Session.Remove("ClientInterviewReviewList");
                return RedirectToAction("ClientInterviewsReview");
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpGet]
        public async Task<IActionResult> ClientFeedbacks()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client" || profileName == "SeniorHR" || profileName == "HR" || profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        ViewBag.PageName = "ClientFeedbacks";
                        ViewBag.type = "Audio Feedback";
                        ViewBag.profileName = "";
                        var results = await _surveyService.FetchClientAudioFeedbackDetails();
                        return View("ClientFeedbacks", results);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }
        [HttpGet]
        public async Task<IActionResult> BroadCastMessages()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Client" || profileName == "SeniorHR" || profileName == "HR" || profileName == "Admin" || profileName == "SupportAdmin")
                    {
                        ViewBag.PageName = "ClientFeedbacks";
                        ViewBag.type = "Broadcast Messages";
                        ViewBag.profileName = "";
                        var results = await _surveyService.FetchBroadcastMessages();
                        return View("ClientFeedbacks", results);
                    }
                    else
                    {
                        return RedirectToAction("SignOut", "Session");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }
        [HttpGet]
        public async Task<IActionResult> ClientIRFReview(string IRFId, string uniqueid)
        {
            var _list = await _aadhaarService.FetchCandidateEmailidByUniqueId(uniqueid);
            ViewBag.Emailid = _list.EmailId;
            ViewBag.IRFId = IRFId;
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> SaveClientIRFReview(int rating, string comments, string Emailid, string IRFId)
        {
            var resultset = await _meetingService.SaveSauveyIRFReview(rating, comments, Emailid, IRFId);
            return Json("success");
        }
        [HttpGet]
        public async Task<IActionResult> PortalErrorList()
        {
            try
            {
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                var result = _azureTableStorageService.GetLogEntities<LogData>("Risebirdlogs");
                return View(result);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Error, ex);
                var result = new List<LogData>();
                return View(result);
            }
        }
        [HttpGet]
        public async Task<IActionResult> CandidateFeedbackForm(int id)
        {
            var model = new PaymentSLAVM();
            model.Interviewid = id;
            await SetInitialValue(model);
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> CandidateFeedbackForm(PaymentSLAVM model)
        {
            try
            {
                await _surveyService.CandidateFeedbackFormDetailsService(model);
                await SetInitialValue(model);
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        public async Task SetInitialValue(PaymentSLAVM model)
        {
            if (string.IsNullOrEmpty(model.Inaccuratedecisionmaking))
            {
                model.Inaccuratedecisionmaking = string.Empty;
            }
            if (string.IsNullOrEmpty(model.TimeManagement))
            {
                model.TimeManagement = string.Empty;
            }
            if (string.IsNullOrEmpty(model.QuestionsRelevance))
            {
                model.QuestionsRelevance = string.Empty;
            }
            if (string.IsNullOrEmpty(model.ScoreRubricIgnorance))
            {
                model.ScoreRubricIgnorance = string.Empty;
            }
            if (string.IsNullOrEmpty(model.PoorFeedbackComments))
            {
                model.PoorFeedbackComments = string.Empty;
            }
            if (string.IsNullOrEmpty(model.CandidateInteractionQuality))
            {
                model.CandidateInteractionQuality = string.Empty;
            }
            if (string.IsNullOrEmpty(model.LimitedHandoncoding))
            {
                model.LimitedHandoncoding = string.Empty;
            }
            if (string.IsNullOrEmpty(model.WrongStatusUpdates))
            {
                model.WrongStatusUpdates = string.Empty;
            }
            if (string.IsNullOrEmpty(model.WeakInfrastructure))
            {
                model.WeakInfrastructure = string.Empty;
            }
            if (string.IsNullOrEmpty(model.ProctoringIgnorance))
            {
                model.ProctoringIgnorance = string.Empty;
            }
            if (string.IsNullOrEmpty(model.UnethicalPractices))
            {
                model.UnethicalPractices = string.Empty;
            }
            if (string.IsNullOrEmpty(model.PoorCommunicationSkills))
            {
                model.PoorCommunicationSkills = string.Empty;
            }
        }
        
    }

}
