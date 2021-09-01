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
        public SurveyController(IConfiguration configuration, IMeetingService meetingService, INotificationHelper notificationHelper, IUserRepository userRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _meetingService = meetingService;
            _notificationHelper = notificationHelper;
            _userRepository = userRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
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
                    var resultsetdetails = _notificationHelper.FeedbackSubmitNotification(_mail, model1.CandidateName);

                }

            }
            catch (Exception ex1)
            {
                Console.WriteLine(ex1.InnerException);
            }
            ViewBag.Status = "Complete";
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
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                int Clientid = 0;
                ViewBag.ProfileName = profileName;
                var modelData = new List<SurveyVM>();
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                var _resultlist = (from data in result
                                   select new SurveyDetails { Interviewerid = data.Interviewerid }).ToList();
                _resultlist = _resultlist.Distinct().ToList();
                var resultset = _meetingService.FetchSurveyInterviewer(_resultlist);
                if (profileName == "Client")
                {
                    var client = _userRepository.GetClientUserDetails(emailid);
                    Clientid = client.Result.ClientId;
                    var interviewerids = _meetingService.FetchAllInterviewidByClient(Clientid, emailid);
                    foreach (var item in result)
                    {
                        if (interviewerids.Any(x => x == item.interviewid))
                        {
                            if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                            {
                                modelData.Add(new SurveyVM
                                {
                                    interviewid = item.interviewid,
                                    Question1 = item.Question1,
                                    Question2 = item.Question2,
                                    Question3 = item.Question3,
                                    Question4 = item.Question4,
                                    Question5 = item.Question5,
                                    Question6 = item.Question6,
                                    Name = item.Name,
                                    EmailAddress = item.EmailAddress,
                                    interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                                    CommentforInterviewer = item.CommentforInterviewer,
                                    PrimarySkill = item.PrimarySkill,
                                    IsClient = true
                                });
                            }
                        }
                    }
                }
                else if (profileName == "SeniorHR")
                {
                    var interviewerids = _meetingService.FetchAllInterviewidBySeniorHR(emailid);
                    foreach (var item in result)
                    {
                        if (interviewerids.Any(x => x == item.interviewid))
                        {
                            if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                            {
                                modelData.Add(new SurveyVM
                                {
                                    interviewid = item.interviewid,
                                    Question1 = item.Question1,
                                    Question2 = item.Question2,
                                    Question3 = item.Question3,
                                    Question4 = item.Question4,
                                    Question5 = item.Question5,
                                    Question6 = item.Question6,
                                    Name = item.Name,
                                    EmailAddress = item.EmailAddress,
                                    interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                                    CommentforInterviewer = item.CommentforInterviewer,
                                    PrimarySkill = item.PrimarySkill,
                                    IsClient = true
                                });
                            }
                        }
                    }
                }
                else
                {
                    var _resultinterview = (from data in result
                                            select new SurveyDetails { interviewid = data.interviewid }).ToList();
                    _resultinterview = _resultinterview.Distinct().ToList();
                    var ClientNamelist = _meetingService.FetchSurveyClientName(_resultinterview);
                    foreach (var item in result)
                    {
                        if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                        {
                            modelData.Add(new SurveyVM
                            {
                                interviewid = item.interviewid,
                                Question1 = item.Question1,
                                Question2 = item.Question2,
                                Question3 = item.Question3,
                                Question4 = item.Question4,
                                Question5 = item.Question5,
                                Question6 = item.Question6,
                                Name = item.Name,
                                EmailAddress = item.EmailAddress,
                                interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                                CommentforInterviewer = item.CommentforInterviewer,
                                PrimarySkill = item.PrimarySkill,
                                Type = ClientNamelist.Where(x => x.interviewid == item.interviewid).FirstOrDefault()?.ClientName,
                            });
                        }
                    }
                }
                if (HttpContext.Session.GetString("CandidateTimelinefilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<CandidateTimelineFilter>(HttpContext.Session.GetString("CandidateTimelinefilters"));
                    filter.ProfileSource = filter.ProfileSource == null ? filter.ProfileSource = modelData.Select(x => x.PrimarySkill).ToArray() : filter.ProfileSource = filter.ProfileSource;
                    filter.HREmail = filter.HREmail == null ? filter.HREmail = modelData.Select(x => x.Type).ToArray() : filter.HREmail = filter.HREmail;
                    modelData = modelData.Where(x => filter.ProfileSource.Contains(x.PrimarySkill)).ToList();
                    modelData = modelData.Where(x => filter.HREmail.Contains(x.Type)).ToList();
                }
                ViewBag.PageName = "Interview";
                ViewBag.IRFId = "0";
                return View(modelData);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }


        }
        [HttpPost]
        public IActionResult CandidateFeedBack(CandidateTimelineFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                ViewBag.ProfileName = profileName;
                int Clientid = 0;
                var modelData = new List<SurveyVM>();
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                var _resultlist = (from data in result
                                   select new SurveyDetails { Interviewerid = data.Interviewerid }).ToList();
                _resultlist = _resultlist.Distinct().ToList();
                var resultset = _meetingService.FetchSurveyInterviewer(_resultlist);
                if (profileName == "Client")
                {
                    var client = _userRepository.GetClientUserDetails(emailid);
                    Clientid = client.Result.ClientId;
                    var interviewerids = _meetingService.FetchAllInterviewidByClient(Clientid, emailid);
                    foreach (var item in result)
                    {
                        if (interviewerids.Any(x => x == item.interviewid))
                        {
                            if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                            {
                                modelData.Add(new SurveyVM
                                {
                                    interviewid = item.interviewid,
                                    Question1 = item.Question1,
                                    Question2 = item.Question2,
                                    Question3 = item.Question3,
                                    Question4 = item.Question4,
                                    Question5 = item.Question5,
                                    Question6 = item.Question6,
                                    Name = item.Name,
                                    EmailAddress = item.EmailAddress,
                                    interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                                    CommentforInterviewer = item.CommentforInterviewer,
                                    PrimarySkill = item.PrimarySkill
                                });
                            }
                        }
                    }
                }
                else if (profileName == "SeniorHR")
                {
                    var interviewerids = _meetingService.FetchAllInterviewidBySeniorHR(emailid);
                    foreach (var item in result)
                    {
                        if (interviewerids.Any(x => x == item.interviewid))
                        {
                            if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                            {
                                modelData.Add(new SurveyVM
                                {
                                    interviewid = item.interviewid,
                                    Question1 = item.Question1,
                                    Question2 = item.Question2,
                                    Question3 = item.Question3,
                                    Question4 = item.Question4,
                                    Question5 = item.Question5,
                                    Question6 = item.Question6,
                                    Name = item.Name,
                                    EmailAddress = item.EmailAddress,
                                    interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                                    CommentforInterviewer = item.CommentforInterviewer,
                                    PrimarySkill = item.PrimarySkill,
                                    IsClient = true
                                });
                            }
                        }
                    }
                }
                else
                {
                    var _resultinterview = (from data in result
                                            select new SurveyDetails { interviewid = data.interviewid }).ToList();
                    _resultinterview = _resultinterview.Distinct().ToList();
                    var ClientNamelist = _meetingService.FetchSurveyClientName(_resultinterview);
                    foreach (var item in result)
                    {
                        if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                        {
                            modelData.Add(new SurveyVM
                            {
                                interviewid = item.interviewid,
                                Question1 = item.Question1,
                                Question2 = item.Question2,
                                Question3 = item.Question3,
                                Question4 = item.Question4,
                                Question5 = item.Question5,
                                Question6 = item.Question6,
                                Name = item.Name,
                                EmailAddress = item.EmailAddress,
                                interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                                CommentforInterviewer = item.CommentforInterviewer,
                                PrimarySkill = item.PrimarySkill,
                                Type = ClientNamelist.Where(x => x.interviewid == item.interviewid).FirstOrDefault()?.ClientName,
                            });
                        }
                    }
                }
                if (filter != null)
                {
                    HttpContext.Session.SetString("CandidateFeedBackfilters", JsonConvert.SerializeObject(filter));
                }
                filter.ProfileSource = filter.ProfileSource == null ? filter.ProfileSource = modelData.Select(x => x.PrimarySkill).ToArray() : filter.ProfileSource = filter.ProfileSource;
                filter.HREmail = filter.HREmail == null ? filter.HREmail = modelData.Select(x => x.Type).ToArray() : filter.HREmail = filter.HREmail;
                modelData = modelData.Where(x => filter.ProfileSource.Contains(x.PrimarySkill)).ToList();
                modelData = modelData.Where(x => filter.HREmail.Contains(x.Type)).ToList();
                ViewBag.PageName = "Interview";
                ViewBag.IRFId = "0";
                return View(modelData);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }
        [Authorize]
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
        public IActionResult CandidateFeedBackByIRFId(string IRFId)
        {
            var modelData = new List<SurveyVM>();
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
            var _resultlist = (from data in result
                               select new SurveyDetails { Interviewerid = data.Interviewerid }).ToList();
            _resultlist = _resultlist.Distinct().ToList();
            var resultset = _meetingService.FetchSurveyInterviewer(_resultlist);
            var interviewerids = _meetingService.FetchAllInterviewidfromIrf(IRFId);
            ViewBag.ProfileName = "";
            foreach (var item in result)
            {
                if (interviewerids.Any(x => x == item.interviewid))
                {
                    if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                    {
                        modelData.Add(new SurveyVM
                        {
                            interviewid = item.interviewid,
                            Question1 = item.Question1,
                            Question2 = item.Question2,
                            Question3 = item.Question3,
                            Question4 = item.Question4,
                            Question5 = item.Question5,
                            Question6 = item.Question6,
                            Name = item.Name,
                            EmailAddress = item.EmailAddress,
                            interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                            CommentforInterviewer = item.CommentforInterviewer,
                            PrimarySkill = item.PrimarySkill
                        });
                    }
                }
            }
            ViewBag.PageName = "IRF";
            ViewBag.IRFId = IRFId;
            return View("CandidateFeedBack", modelData);
        }
        public FileContentResult CandidateFeedbackExporttoExcel()
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
                    int Clientid = 0;
                    if (profileName == "Client")
                    {
                        var client = _userRepository.GetClientUserDetails(emailid);
                        Clientid = client.Result.ClientId;
                    }
                    var interviewerids = _meetingService.FetchAllInterviewidByClient(Clientid, emailid);
                    var modelData = new List<SurveyVM>();
                    AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                    var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                    var _resultlist = (from data in result
                                       select new SurveyDetails { Interviewerid = data.Interviewerid }).ToList();
                    _resultlist = _resultlist.Distinct().ToList();
                    var resultset = _meetingService.FetchSurveyInterviewer(_resultlist);
                    if (profileName == "Client")
                    {
                        foreach (var item in result)
                        {
                            if (interviewerids.Any(x => x == item.interviewid))
                            {
                                if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                                {
                                    modelData.Add(new SurveyVM
                                    {
                                        interviewid = item.interviewid,
                                        Question1 = item.Question1,
                                        Question2 = item.Question2,
                                        Question3 = item.Question3,
                                        Question4 = item.Question4,
                                        Question5 = item.Question5,
                                        Question6 = item.Question6,
                                        Name = item.Name,
                                        EmailAddress = item.EmailAddress,
                                        interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                                        CommentforInterviewer = item.CommentforInterviewer,
                                        PrimarySkill = item.PrimarySkill
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var item in result)
                        {
                            if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                            {
                                modelData.Add(new SurveyVM
                                {
                                    interviewid = item.interviewid,
                                    Question1 = item.Question1,
                                    Question2 = item.Question2,
                                    Question3 = item.Question3,
                                    Question4 = item.Question4,
                                    Question5 = item.Question5,
                                    Question6 = item.Question6,
                                    Name = item.Name,
                                    EmailAddress = item.EmailAddress,
                                    interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                                    CommentforInterviewer = item.CommentforInterviewer,
                                    PrimarySkill = item.PrimarySkill
                                });
                            }
                        }
                    }
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
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }
        public FileContentResult CandidateFeedBackByIRFIdExporttoExcel(string IRFId)
        {
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("Candidate Feedback");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_CandidateFeedback.xlsx";
            try
            {
                var modelData = new List<SurveyVM>();
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                var _resultlist = (from data in result
                                   select new SurveyDetails { Interviewerid = data.Interviewerid }).ToList();
                _resultlist = _resultlist.Distinct().ToList();
                var resultset = _meetingService.FetchSurveyInterviewer(_resultlist);
                var interviewerids = _meetingService.FetchAllInterviewidfromIrf(IRFId);
                foreach (var item in result)
                {
                    if (interviewerids.Any(x => x == item.interviewid))
                    {
                        if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                        {
                            modelData.Add(new SurveyVM
                            {
                                interviewid = item.interviewid,
                                Question1 = item.Question1,
                                Question2 = item.Question2,
                                Question3 = item.Question3,
                                Question4 = item.Question4,
                                Question5 = item.Question5,
                                Question6 = item.Question6,
                                Name = item.Name,
                                EmailAddress = item.EmailAddress,
                                interviewerEmail = resultset.Where(x => x.Interviewerid == item.Interviewerid).FirstOrDefault()?.Intervieweremail,
                                CommentforInterviewer = item.CommentforInterviewer,
                                PrimarySkill = item.PrimarySkill
                            });
                        }
                    }
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
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }
        public JsonResult FetchCandidateFeedBackDetails()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                int Clientid = 0;
                if (profileName == "Client")
                {
                    var client = _userRepository.GetClientUserDetails(emailid);
                    Clientid = client.Result.ClientId;
                }
                var modelData = new List<SurveyVM>();
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                var result = _azureTableStorageService.GetAllEntities<SurveyVM>("RisebirdSurvey");
                if (profileName == "Client")
                {
                    var interviewerids = _meetingService.FetchAllInterviewidByClient(Clientid, emailid);
                    foreach (var item in result)
                    {
                        if (interviewerids.Any(x => x == item.interviewid))
                        {
                            if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                            {
                                modelData.Add(new SurveyVM
                                {
                                    interviewid = item.interviewid,
                                    Question1 = item.Question1,
                                    Question2 = item.Question2,
                                    Question3 = item.Question3,
                                    Question4 = item.Question4,
                                    Question5 = item.Question5,
                                    Question6 = item.Question6,
                                    Name = item.Name,
                                    EmailAddress = item.EmailAddress,
                                    CommentforInterviewer = item.CommentforInterviewer,
                                    PrimarySkill = item.PrimarySkill,
                                });
                            }
                        }
                    }
                }
                else
                {
                    foreach (var item in result)
                    {
                        if (modelData.Where(x => x.interviewid == item.interviewid).Count() == 0)
                        {
                            modelData.Add(new SurveyVM
                            {
                                interviewid = item.interviewid,
                                Question1 = item.Question1,
                                Question2 = item.Question2,
                                Question3 = item.Question3,
                                Question4 = item.Question4,
                                Question5 = item.Question5,
                                Question6 = item.Question6,
                                Name = item.Name,
                                EmailAddress = item.EmailAddress,
                                CommentforInterviewer = item.CommentforInterviewer,
                                PrimarySkill = item.PrimarySkill,
                            });
                        }
                    }
                }
                var avgQ1 = modelData.Average(x => String.IsNullOrEmpty(x.Question1) ? 0 : Convert.ToInt32(x.Question1)).ToString("#.##");
                var avgQ2 = modelData.Average(x => String.IsNullOrEmpty(x.Question2) ? 0 : Convert.ToInt32(x.Question2)).ToString("0.00");
                var avgQ6 = modelData.Average(x => String.IsNullOrEmpty(x.Question6) ? 0 : Convert.ToInt32(x.Question6)).ToString("#.##");
                return Json(new { avgQ1 = avgQ1, avgQ2 = avgQ2, avgQ6 = avgQ6 });
            }
            else
            {
                return Json("Fail");
            }

        }
    }
}
