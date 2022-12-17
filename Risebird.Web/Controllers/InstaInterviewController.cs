using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using OfficeOpenXml;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.Web.Controllers.AzureMediaservice;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirusTotalNet;
using VirusTotalNet.ResponseCodes;

namespace Risebird.Web.Controllers
{
    public class InstaInterviewController : Controller
    {
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly INotificationHelper _notificationHelper;
        private readonly IConfiguration _configuration;
        private readonly IInstaInterviewService _instaInterviewService;
        private readonly IInterviewerService _interviewerService;
        private readonly IMeetingService _meetingService;
        private readonly IIRFRepository _irfRepository;
        private readonly IUserService _userService;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly ISkillService _skillService;

        public InstaInterviewController(IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, INotificationHelper notificationHelper, IConfiguration configuration, IInstaInterviewService instaInterviewService, IInterviewerService interviewerService, IMeetingService meetingService, IIRFRepository irfRepository, IUserService userService, IFileMimeTypeHelper fileMimeTypeHelper, ISkillService skillService)
        {
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _notificationHelper = notificationHelper;
            _configuration = configuration;
            _instaInterviewService = instaInterviewService;
            _interviewerService = interviewerService;
            _meetingService = meetingService;
            _irfRepository = irfRepository;
            _userService = userService;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _skillService = skillService;
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InstaIRF()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new List<InstaMeetingVM>();
                        var irfList = _instaInterviewService.GetAllInstaRequests(emailid, profileName);
                        model = (List<InstaMeetingVM>)irfList;
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (HttpContext.Session.GetString("InstaRequestfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<IRFFilter>(HttpContext.Session.GetString("InstaRequestfilters"));
                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = model.Select(x => x.RequiredSkill).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            model = model.Where(c => filter.PrimarySkill.Contains(c.RequiredSkill.Split("|")[0])).ToList();
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CreateRequest()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new InstaMeetingVM();
                        ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                        //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                        //Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                        //model.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                        var skills = _irfRepository.GetSkills();
                        ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                        var Skillset = skills;
                        if (Skillset != null)
                        {
                            Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
                        }
                        ViewBag.Skill = new SelectList(Skillset, "id", "Name");
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRequest(InstaMeetingVM meeting)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                        if (profileName == "Client")
                        {
                            meeting.RequestedBy = emailid;
                        }
                        if (ModelState.IsValid)
                        {
                            if (meeting.JDFile != null)
                            {
                                var fileName = meeting.JDFile.FileName.Replace(" ", "_");
                                FileInfo fi = new FileInfo(fileName);

                                int virusScanResult = 0;

                                if (profileName == "Client")
                                {
                                    if ((await _instaInterviewService.GetClientPasswordResetStatus(emailid)) == (int)BooleanResultsEnum.True)
                                    {
                                        using (var ms = new MemoryStream())
                                        {
                                            meeting.JDFile.CopyTo(ms);

                                            VirusTotal virusTotal = new VirusTotal(_configuration["VirusTotal:ApiKey"]);

                                            //Use HTTPS instead of HTTP
                                            virusTotal.UseTLS = true;

                                            //byte[] eicar = Encoding.ASCII.GetBytes(@"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");
                                            //VirusTotalNet.Results.FileReport fileReport = await virusTotal.GetFileReportAsync(eicar);//ms

                                            VirusTotalNet.Results.FileReport fileReport = await virusTotal.GetFileReportAsync(ms);
                                            bool hasFileBeenScannedBefore = fileReport.ResponseCode == FileReportResponseCode.Present;

                                            if (hasFileBeenScannedBefore)
                                            {
                                                virusScanResult = fileReport.Positives;
                                            }
                                            else
                                            {
                                                //var fileResults = await virusTotal.ScanFileAsync(eicar, fileName);//ms
                                                var fileResults = await virusTotal.ScanFileAsync(ms, fileName);
                                                virusScanResult = fileReport.Positives;
                                            }

                                            if (virusScanResult > 0)
                                            {
                                                ModelState.AddModelError(string.Empty, "File upload failed. Virus Detected!");
                                                //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                                //if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                                                //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                                                //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                                ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                                                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                                                return View(meeting);
                                            }

                                        }

                                    }
                                }


                                if (fi.Extension == ".docx" || fi.Extension == ".doc" || fi.Extension == ".pdf")
                                {
                                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(meeting.JDFile);
                                    if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                               || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                    {
                                        await _azureStorageHelper.InsertBlob(meeting.JDFile.FileName.Replace(" ", ""), "resumes", meeting.JDFile);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                        var blob = containerRef.GetBlockBlobReference(meeting.JDFile.FileName.Replace(" ", ""));
                                        meeting.JDLink = blob.Uri.ToString();
                                    }
                                    else
                                    {
                                        ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                        //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                        //if (profileName == "SeniorHR")
                                        //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                                        //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                        ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                                        ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                                        return View(meeting);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                    //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                    //if (profileName == "SeniorHR" )
                                    //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                                    //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                    ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                                    ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                                    return View(meeting);
                                }
                            }
                            if (meeting.TechSkill.Count() < 2)
                            {
                                ModelState.AddModelError(string.Empty, "Please Add atleast 2 skills!");
                                //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                //if (profileName == "SeniorHR")
                                //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                                //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                                return View(meeting);
                            }
                            if (_instaInterviewService.CreateRequest(meeting))
                            {
                                TempData["SuccessErrorMessageInterview"] = "Request has been created Successfully!";
                                return RedirectToAction("InstaIRF");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Failed to create request!";
                                return RedirectToAction("InstaIRF");
                            }

                        }
                        else
                        {
                            //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                            //if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                            //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                            //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                            ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                            ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                            return View("CreateRequest", meeting);
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Failed to Create request!";
                return RedirectToAction("Index");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditRequest(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new InstaMeetingVM();
                        model = _instaInterviewService.FetchRequestById(id);
                        ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");

                        //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                        //Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                        //model.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                        var skills = _irfRepository.GetSkills();
                        ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                        var Skillset = skills;
                        if (Skillset != null)
                        {
                            Skillset = Skillset.OrderBy(x => x.Name.Trim()).ToList();
                        }
                        ViewBag.Skill = new SelectList(Skillset, "id", "Name");
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRequest(InstaMeetingVM meeting)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            meeting.RequestedBy = emailid;
                        }
                        if (ModelState.IsValid)
                        {
                            if (meeting.JDFile != null)
                            {
                                var fileName = meeting.JDFile.FileName.Replace(" ", "_");
                                FileInfo fi = new FileInfo(fileName);

                                int virusScanResult = 0;
                                if (profileName == "Client")
                                {
                                    if ((await _instaInterviewService.GetClientPasswordResetStatus(emailid)) == (int)BooleanResultsEnum.True)
                                    {
                                        using (var ms = new MemoryStream())
                                        {
                                            meeting.JDFile.CopyTo(ms);

                                            //VirusTotal virusTotal = new VirusTotal("30368672cc1d7fa0497a0d2c640544ade03ad455600d24c220bfd41c83b5e6d5");
                                            VirusTotal virusTotal = new VirusTotal(_configuration["VirusTotal:ApiKey"]);

                                            //Use HTTPS instead of HTTP
                                            virusTotal.UseTLS = true;

                                            //byte[] eicar = Encoding.ASCII.GetBytes(@"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");
                                            //VirusTotalNet.Results.FileReport fileReport = await virusTotal.GetFileReportAsync(eicar);//ms

                                            VirusTotalNet.Results.FileReport fileReport = await virusTotal.GetFileReportAsync(ms);
                                            bool hasFileBeenScannedBefore = fileReport.ResponseCode == FileReportResponseCode.Present;

                                            if (hasFileBeenScannedBefore)
                                            {
                                                virusScanResult = fileReport.Positives;
                                            }
                                            else
                                            {
                                                //var fileResults = await virusTotal.ScanFileAsync(eicar, fileName);//ms
                                                var fileResults = await virusTotal.ScanFileAsync(ms, fileName);
                                                virusScanResult = fileReport.Positives;
                                            }

                                            if (virusScanResult > 0)
                                            {
                                                ModelState.AddModelError(string.Empty, "File upload failed. Virus Detected!");
                                                //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                                //if (profileName == "SeniorHR")
                                                //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                                                //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                                ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                                                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                                                return View(meeting);
                                            }

                                        }

                                    }
                                }

                                if (fi.Extension == ".docx" || fi.Extension == ".doc" || fi.Extension == ".pdf")
                                {
                                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(meeting.JDFile);
                                    if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                              || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                    {
                                        await _azureStorageHelper.InsertBlob(meeting.JDFile.FileName.Replace(" ", ""), "resumes", meeting.JDFile);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                        var blob = containerRef.GetBlockBlobReference(meeting.JDFile.FileName.Replace(" ", ""));
                                        meeting.JDLink = blob.Uri.ToString();
                                    }
                                    else
                                    {
                                        ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                        //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                        //if (profileName == "SeniorHR")
                                        //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                                        //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                        ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                                        ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                                        return View("EditRequest", meeting);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                    //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                    //if (profileName == "SeniorHR")
                                    //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                                    //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                    ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                                    ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                                    return View("EditRequest", meeting);
                                }
                            }
                            if (meeting.TechSkill.Count() < 2)
                            {
                                ModelState.AddModelError(string.Empty, "Please Add atleast 2 skills!");
                                //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                //if (profileName == "SeniorHR")
                                //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                                //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                                return View(meeting);
                            }
                            if (_instaInterviewService.CreateRequest(meeting))
                            {
                                TempData["SuccessErrorMessageInterview"] = "Request has been updated Successfully!";
                                return RedirectToAction("InstaIRF");
                            }
                            else
                            {
                                TempData["SuccessErrorMessageInterview"] = "Failed to update the request!";
                                return RedirectToAction("InstaIRF");
                            }

                        }
                        else
                        {
                            //var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                            //if (profileName == "SeniorHR" || profileName == "SupportAdmin")
                            //    Assessmentgroups = Assessmentgroups.Where(x => x.Isinstaavailable == true).ToList();
                            //meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                            ViewBag.AssessmentCategory = new SelectList(_skillService.GetAssessmentGroupCategory(), "id", "Name");
                            ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                            return View("EditRequest", meeting);
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Failed to update the request!";
                return RedirectToAction("InstaIRF");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Index(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        ViewBag.profile = profileName;
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
                        }
                        List<InstaMeetingVM> modelList = new List<InstaMeetingVM>();
                        if (HttpContext.Session.GetString("InstaInterviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("InstaInterviewfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = _instaInterviewService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), id, emailId);
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                                x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription();
                                return x;
                            }).ToList();

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.RequiredSkill.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            //filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.ConfirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.Interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                            modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.RequiredSkill.Split("|")[0])
                            //&& ((filter.ConfirmationStatus != null && filter.ConfirmationStatus.Count() > 0) ? filter.ConfirmationStatus.Contains(c.ConfirmationStatus) : c.ConfirmationStatus == null)
                            && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.Interviewer) : c.Interviewer == null)).ToList();
                        }
                        else
                        {
                            modelList = _instaInterviewService.GetAllMeetingList(DateTime.Now.Date, DateTime.Now.Date.AddDays(1), id, emailId);
                            modelList = modelList.Select(x => { x.ConfirmationStatus = ((InstaReceiveMeetingStatus)Convert.ToInt32(x.ConfirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                        }
                        ViewBag.Requestid = id;
                        return View(modelList);
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
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(int id, InterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("InstaInterviewfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("Index", new { id = id });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Add(int Requestedid)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var model = new InstaMeetingVM();
                        model.RequestId = Requestedid;
                        var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                        model.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(InstaMeetingVM meeting)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    ViewBag.profile = profileName;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        if (profileName == "Client")
                        {
                            meeting.RequestedBy = emailid;
                        }
                        if (ModelState.IsValid)
                        {
                            string uploadedFileURL = string.Empty;
                            if (meeting.ResumeFile == null)
                            {
                                ModelState.AddModelError(string.Empty, "Please upload a Resume or add a Resume link.");
                                var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                return View("Add", meeting);
                            }
                            if (meeting.ResumeFile != null)
                            {
                                var fileName = meeting.ResumeFile.FileName.Replace(" ", "_");
                                FileInfo fi = new FileInfo(fileName);
                                if (fi.Extension == ".docx" || fi.Extension == ".doc" || fi.Extension == ".pdf")
                                {
                                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(meeting.ResumeFile);
                                    if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                              || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                    {
                                        await _azureStorageHelper.InsertBlob(fileName, "resumes", meeting.ResumeFile);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                        var blob = containerRef.GetBlockBlobReference(fileName);
                                        uploadedFileURL = blob.Uri.ToString();
                                    }
                                    else
                                    {
                                        ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                        var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                        meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                        return View("Add", meeting);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                    var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                    meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                    return View("Add", meeting);
                                }
                            }
                            if (uploadedFileURL == null || uploadedFileURL == string.Empty)
                            {
                                ModelState.AddModelError(string.Empty, "Resume Upload has failed.");
                                var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                return View("Add", meeting);
                            }
                            else
                            {
                                meeting.ResumeLink = uploadedFileURL;
                                if (_instaInterviewService.AddInstaMeeting(meeting))
                                {
                                    TempData["SuccessErrorMessageInterview"] = "Meeting has been added Successfully!";
                                    return RedirectToAction("Index", new { id = meeting.RequestId });
                                }
                                else
                                {
                                    TempData["SuccessErrorMessageInterview"] = "Failed to add Meeting!";
                                    return RedirectToAction("Index", new { id = meeting.RequestId });
                                }
                            }
                        }
                        else
                        {
                            var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                            meeting.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                            return View("Add", meeting);
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageInterview"] = "Meeting addition has Failed!";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        InstaMeetingVM meetingToUpdate = await _instaInterviewService.GetInstaMeeting(id);
                        TempData["ResumeLink"] = meetingToUpdate.ResumeLink;
                        var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                        meetingToUpdate.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                        return View(meetingToUpdate);
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
            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InstaMeetingVM meetingUpdated)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailId = HttpContext.Session.GetString("email")?.Trim();
                    emailId = emailId.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (ModelState.IsValid)
                        {
                            if ((meetingUpdated.ResumeFile != null))
                            {
                                var fileName = meetingUpdated.ResumeFile.FileName.Replace(" ", "_");
                                FileInfo fi = new FileInfo(fileName);
                                if (fi.Extension == ".docx" || fi.Extension == ".doc" || fi.Extension == ".pdf")
                                {
                                    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(meetingUpdated.ResumeFile);
                                    if (fileMimeType == "application/msword" || fileMimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                              || fileMimeType == "application/pdf" || fileMimeType == "application/x-java-archive" || fileMimeType == "application/vnd.ms-powerpoint")
                                    {
                                        await _azureStorageHelper.InsertBlob(fileName, "resumes", meetingUpdated.ResumeFile);
                                        var containerRef = _azureStorageHelper.GetCloudBlobContainer("resumes");
                                        var blob = containerRef.GetBlockBlobReference(fileName);
                                        string uploadedFileURL = blob.Uri.ToString();
                                        if (uploadedFileURL == null)
                                        {
                                            var model = new AddUpdateMeetingVM();
                                            ModelState.AddModelError(string.Empty, "Resume Upload has failed.");
                                            var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                            meetingUpdated.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                            return View(model);
                                        }
                                        meetingUpdated.ResumeLink = uploadedFileURL;
                                    }
                                    else
                                    {
                                        ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                        var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                        meetingUpdated.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                        return View("Edit", meetingUpdated);
                                    }
                                }
                                else
                                {
                                    ModelState.AddModelError(string.Empty, "Please select valid file for import(.pdf, .doc, .docx)!");
                                    var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                                    meetingUpdated.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                                    return View("Edit", meetingUpdated);
                                }
                            }
                            else if (meetingUpdated.ResumeLink == null || meetingUpdated.ResumeLink == "")
                            {
                                meetingUpdated.ResumeLink = TempData["ResumeLink"]?.ToString();
                            }
                            if (_instaInterviewService.UpdateInstaMeeting(meetingUpdated))
                            {
                                TempData["SuccessErrorMessageUpdatedInterview"] = "Meeting has been updated Successfully!";
                                return RedirectToAction("Edit", new { id = meetingUpdated.id });
                            }
                            else
                            {
                                TempData["SuccessErrorMessageUpdatedInterview"] = "Failed to update Meeting!";
                                return RedirectToAction("Edit", new { id = meetingUpdated.id });
                            }
                        }
                        else
                        {
                            var Assessmentgroups = _interviewerService.FetchAssessmentGroups();
                            meetingUpdated.AssessmentGroupList = new SelectList(Assessmentgroups, "id", "AssessmentGroup");
                            return View("Edit", meetingUpdated);
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageUpdatedInterview"] = "Meeting update has Failed";
                return RedirectToAction("Edit", new { id = meetingUpdated.id });
            }
        }
        //Fetch Skills On AssessmentGroupId

        [HttpGet]
        public JsonResult FetchAssessmentGroupSkills(int id)
        {
            var Skills = _instaInterviewService.FetchSkillsByAssessmentGroup(id);
            if (Skills == null)
            {
                Skills = "";
            }
            return Json(Skills);
        }
        [HttpGet]
        public async Task<IActionResult> OpenInterview()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var meetings = _instaInterviewService.GetAllInstaOpenInterviews(emailid);
                        meetings = meetings.Select(x => { x.ConfirmationStatus = ((InstaReceiveMeetingStatus)Convert.ToInt32(x.ConfirmationStatus)).GetDescription(); return x; }).ToList();
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
                        }
                        return View(meetings);
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
                return View();
            }
        }
        [HttpGet]
        public async Task<IActionResult> AssignYourself(int id, string InterviewDate, string CandidateName, string Role)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var checkforAssign = _instaInterviewService.CheckForInstaAssign(id);
                        if (string.IsNullOrEmpty(checkforAssign))
                        {
                            var result = _instaInterviewService.InstaAssignYourself(id, emailid.ToLower(), Convert.ToDateTime(InterviewDate));
                            if (result.Contains("Successfully"))
                            {
                                InstaZoomMeetingVM _zoommetingobject = new InstaZoomMeetingVM();
                                _zoommetingobject.topic = CandidateName + "| " + Role + "| Video Interview";
                                TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                                DateTime indianTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow.AddMinutes(15), INDIAN_ZONE);
                                _zoommetingobject.start_time = indianTime.ToString("yyyy-MM-ddThh:mm:ss");
                                var accountEmail = await _instaInterviewService.CheckInstaMeetingAccount(indianTime, indianTime.AddMinutes(60));
                                if (string.IsNullOrEmpty(accountEmail))
                                    accountEmail = "me";
                                var zoomlink = CreateZoomMeetings(_zoommetingobject, accountEmail, indianTime);
                                var clientname = _instaInterviewService.UpdateInstaZoomLink(id, zoomlink.join_url, indianTime, indianTime.AddMinutes(60), accountEmail);
                                //Send Mail to Client And Interviewer
                                Mail _mail = new Mail
                                {
                                    TO = emailid,
                                    Subject = "Risebird: Insta interview has been scheduled for " + CandidateName
                                };
                                var emailsend = await _notificationHelper.InstaInterviewwithzoomlink(_mail, CandidateName, zoomlink.join_url, zoomlink.InterviewDate);
                                Mail _mail1 = new Mail
                                {
                                    TO = clientname,
                                    Subject = "Risebird: Insta interview has been confirmed for " + CandidateName
                                };
                                var emailsend1 = await _notificationHelper.InstaClientInterviewwithzoomlink(_mail1, CandidateName, zoomlink.join_url, zoomlink.InterviewDate);

                            }
                            TempData["SuccessErrorMessage"] = result;
                            ViewBag.Message = result;
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "This interview has been already assigned by another Interviewer";
                            ViewBag.Message = "This interview has been already assigned by another Interviewer";
                        }
                        return RedirectToAction("OpenInterview");
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
                return RedirectToAction("OpenInterview");
            }
        }

        [HttpGet]
        public async Task<IActionResult> MyInterview()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Interviewer")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myInterview = _instaInterviewService.GetAllInstaAssignedInterviews(emailid);
                        var codedname = await _meetingService.FetchInterviewerCodedName(emailid);
                        ViewBag.CodedName = codedname;
                        myInterview = myInterview.Select(x => { x.ConfirmationStatus = ((InstaReceiveMeetingStatus)Convert.ToInt32(x.ConfirmationStatus)).GetDescription(); return x; }).ToList();
                        foreach (var item in myInterview)
                        {
                            if (item.InterviewStatus > 0)
                            {
                                item.intervieweStatusStatDesc = ((InterviewStatus)(item.InterviewStatus)).GetDescription();
                            }
                            if (item.MeetingStatus > 0)
                            {
                                item.MeetingStatusStatusDesc = ((MeetingStatus)(item.MeetingStatus)).GetDescription();
                            }
                        }
                        if (HttpContext.Session.GetString("MyInstaInterviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<MyInterviewFilter>(HttpContext.Session.GetString("MyInstaInterviewfilters"));

                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                myInterview = myInterview.Where(t => t.RequestedDate.Date >= filter.StartDate && t.RequestedDate.Date <= filter.EndDate).ToList();
                            }
                            else if (filter.StartDate != null && filter.EndDate == null)
                            {
                                myInterview = myInterview.Where(t => t.RequestedDate.Date >= filter.StartDate /*&& t.interviewDate.Date <= filter.EndDate*/).ToList();
                            }
                            else if (filter.StartDate == null && filter.EndDate != null)
                            {
                                myInterview = myInterview.Where(t => /*t.interviewDate.Date >= filter.StartDate &&*/ t.RequestedDate.Date <= filter.EndDate).ToList();
                            }
                            //filter.InterviewDuration = filter.InterviewDuration == null ? filter.InterviewDuration = myInterview.Select(x => x.interviewDuration).ToArray() : filter.InterviewDuration = filter.InterviewDuration;
                            filter.InterviewDate = filter.InterviewDate == null ? filter.InterviewDate = myInterview.Select(x => Convert.ToString(x.RequestedDate)).ToArray() : filter.InterviewDate = filter.InterviewDate;
                            filter.CandidateName = filter.CandidateName == null ? filter.CandidateName = myInterview.Select(x => x.CandidateName).ToArray() : filter.CandidateName = filter.CandidateName;
                            //filter.InterviewDuration.Contains(c.interviewDuration)
                            myInterview = myInterview.Where(c => (filter.InterviewDate.Count() > 0 ? filter.InterviewDate.Contains(Convert.ToString(c.RequestedDate)) : c.RequestedDate == null)
                           && (filter.CandidateName.Count() > 0 ? filter.CandidateName.Contains(c.CandidateName) : c.CandidateName == null)).ToList();
                        }
                        if (TempData["SuccessErrorMessage"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                            TempData.Remove("SuccessErrorMessage");
                        }
                        return View(myInterview);
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
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> MyInterview(MyInterviewFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("MyInstaInterviewfilters", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("MyInterview");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public IActionResult ResetMyInterviewFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("MyInstaInterviewfilters");
                    return RedirectToAction("MyInterview");
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }

        [HttpGet]
        public IActionResult AddReport(int interviewId, int Requestedid, bool isClient = false, string actiontype = null)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    bool isAdminHR = false;
                    bool isInterviewer = false;
                    ViewBag.actiontype = actiontype;
                    string profileName = HttpContext.Session.GetString("profileName");
                    ViewBag.profile = profileName.Replace("\"", "");
                    if (profileName != null)
                    {
                        profileName = profileName.Replace("\"", "");
                        if (profileName == "HR" || profileName == "Client")
                        {
                            isAdminHR = true;
                        }
                        else if (profileName == "Admin" || profileName == "Interviewer" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            isInterviewer = true;
                        }
                    }
                    var model = _instaInterviewService.GetInstaInteviewReport(interviewId).Result;
                    if (model == null)
                    {
                        if (isAdminHR || profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                        {
                            return RedirectToAction("Index", new { id = Requestedid });
                        }
                        else if (isInterviewer)
                        {
                            return RedirectToAction("MyInterview");
                        }
                    }
                    var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                            select new { ID = (int)s, Name = s.GetDescription() };
                    model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");
                    var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");
                    ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                    ViewBag.isAdminHR = isAdminHR;
                    //model.IsInterviewer = isInterviewer;
                    //model.PreviousMeetingStatus = model.MeetingStatus;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReport(InstaMeetingVM model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    ViewBag.PageStatus = "Mainurl";
                    ViewBag.profile = profileName.Replace("\"", "");
                    bool isAdminHR = false;
                    bool isInterviewer = false;
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Interviewer" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                    {
                        if (profileName != null)
                        {
                            profileName = profileName.Replace("\"", "");
                            if (profileName == "HR" || profileName == "Client")
                            {
                                isAdminHR = true;
                            }
                            else if (profileName == "Admin" || profileName == "Interviewer" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                            {
                                isInterviewer = true;
                            }
                        }
                        var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                                select new { ID = (int)s, Name = s.GetDescription() };
                        model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");
                        var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                              select new { ID = (int)s, Name = s.GetDescription() };
                        model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");
                        ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                        ViewBag.isAdminHR = isAdminHR;
                        //if (model.VideoFile == null && model.PageVideoLink == null && model.VideoLink == null)
                        //{
                        //    ModelState.AddModelError(string.Empty, "Video file/link not found.");
                        //    return View("AddReport", model);
                        //}
                        if (model.VideoFile != null)
                        {
                            AzureCall _call = new AzureCall(_configuration);
                            var resulttype = _call.CallApiUpload(model.VideoFile);
                            resulttype = resulttype.Replace("\"", "");
                            var uploadedvideoFileURL = resulttype;
                            if (uploadedvideoFileURL == null)
                            {
                                ModelState.AddModelError(string.Empty, "Video Upload is failed.");
                                return View("AddReport", model);
                            }
                            else
                            {
                                model.VideoLink = uploadedvideoFileURL;
                            }
                        }
                        else if (model.PageVideoLink != null)
                        {
                            model.VideoLink = model.PageVideoLink;
                        }
                        if (_instaInterviewService.AddInstaInterviewReport(model, emailid).Result)
                        {
                            if (model.MeetingStatus == 4 && model.PreviousMeetingStatus != model.MeetingStatus)
                            {
                                string sendreporturl = "https://" + Request.Host.ToString() + "/InstaInterview/InstaAddReport?interviewId=" + model.id + "&Requestedid=" + model.RequestId;
                                Mail _mail1 = new Mail
                                {
                                    TO = model.RequestedBy,
                                    Subject = "Insta Report URL of " + model.CandidateName + " - for Insta Interview"
                                };
                                var emailsend1 = await _notificationHelper.InstaReportClientNotification(_mail1, sendreporturl, model.CandidateName);
                            }
                            TempData["SuccessErrorMessage"] = "Report has been saved Successfully!";
                            //return RedirectToAction("AddReport", new { interviewId = model.id, Requestedid = model.RequestId });
                            if (profileName == "Admin" || profileName == "SeniorHR" || profileName == "SupportAdmin")
                            {
                                return RedirectToAction("Index", new { id = model.RequestId });
                            }
                            else
                            {
                                return RedirectToAction("MyInterview");
                            }
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Report was not saved.";
                            if (profileName == "Admin")
                            {
                                return RedirectToAction("Index", new { id = model.RequestId });
                            }
                            else
                            {
                                return RedirectToAction("MyInterview");
                            }
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
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Report was not saved.";
                return RedirectToAction("MyInterview");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PublicReport(int interviewId, int Requestedid, bool isClient = false, string actiontype = null)
        {
            try
            {
                bool isAdminHR = false;
                ViewBag.actiontype = actiontype;
                string profileName = HttpContext.Session.GetString("profileName");
                ViewBag.PageStatus = "Public";
                ViewBag.PageReport = "";
                var model = await _instaInterviewService.GetInstaInteviewReport(interviewId);
                var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                        select new { ID = (int)s, Name = s.GetDescription() };
                model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");
                var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                      select new { ID = (int)s, Name = s.GetDescription() };
                model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");
                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                ViewBag.isAdminHR = isAdminHR;
                var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                return View("AddReport", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }

        [HttpGet]
        public async Task<IActionResult> InstaAddReport(int interviewId, int Requestedid, bool isClient = false, string actiontype = null)
        {
            try
            {
                bool isAdminHR = false;
                ViewBag.actiontype = actiontype;
                string profileName = HttpContext.Session.GetString("profileName");

                ViewBag.PageStatus = "Public";
                ViewBag.PageReport = "Insta";
                var model = await _instaInterviewService.GetInstaInteviewReport(interviewId);
                var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                        select new { ID = (int)s, Name = s.GetDescription() };
                model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");
                var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                      select new { ID = (int)s, Name = s.GetDescription() };
                model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");
                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                ViewBag.isAdminHR = isAdminHR;
                var menuData = await _userService.GetMenuBasedOnUserEmailld("ExternalUser");
                var menus = menuData.GroupBy(p => p.functionName).Select(g => g.First()).ToList();
                if (menus != null)
                {
                    menus = menus.Where(x => x.functionName != "Support" && x.functionName != "Common Pool" && x.functionName != "Risetube").ToList();
                    HttpContext.Session.SetString("userMenus", JsonConvert.SerializeObject(menus));
                }
                return View("AddReport", model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }
        }

        [HttpGet]
        public InstaZoomMeetingResponse CreateZoomMeetings(InstaZoomMeetingVM _zoommetingobject, string accountEmail, DateTime indianTime)
        {
            _zoommetingobject.type = 2;
            _zoommetingobject.duration = 60;
            _zoommetingobject.timezone = "Asia/Calcutta";
            _zoommetingobject.settings.host_video = true;
            _zoommetingobject.settings.participant_video = true;
            _zoommetingobject.settings.join_before_host = true;
            _zoommetingobject.settings.jbh_time = 5;
            _zoommetingobject.settings.auto_recording = "cloud";
            string token = ZoomToken();

            var client = new RestClient(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"users/" + accountEmail + "/meetings"));

            var request = new RestRequest(string.Concat(_configuration["ZoomAppDetails:endPoint"], $"users/" + accountEmail + "/meetings"), Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddHeader("authorization", "Bearer " + token);
            var body = JsonConvert.SerializeObject(_zoommetingobject);
            request.AddParameter("application/json", body, ParameterType.RequestBody);
            RestResponse response = client.Execute(request);
            var content = response.Content;
            var convertedResponse = JsonConvert.DeserializeObject<InstaZoomMeetingResponse>(content);
            convertedResponse.InterviewDate = indianTime;
            return convertedResponse;
        }

        [HttpGet]
        public string ZoomToken()
        {
            DateTime Expiry = DateTime.UtcNow.AddDays(1);
            string ApiKey = _configuration["ZoomAppDetails:apiKey"];
            string ApiSecret = _configuration["ZoomAppDetails:secretKey"];
            int ts = (int)(Expiry - new DateTime(1970, 1, 1)).TotalSeconds;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ApiSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(credentials);
            var payload = new JwtPayload
            {
                { "iss", ApiKey},
                { "exp", ts },
            };
            var secToken = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();
            var tokenString = handler.WriteToken(secToken);
            return tokenString;
        }

        [HttpGet]
        public FileContentResult InstaInterviewsExporttoExcel(int id)
        {
            string userEmailId = string.Empty;
            ExcelPackage Ep = new ExcelPackage();
            ExcelWorksheet Sheet = Ep.Workbook.Worksheets.Add("InstaInterviews");
            string fileName = "Risebird" + DateTime.Now.ToString("dd/MM/yyyy") + "_InstaInterviews" + ".xlsx";
            try
            {
                var emailid = HttpContext.Session.GetString("email")?.Trim();
                emailid = emailid.Replace("\"", "");
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                List<InstaMeetingVM> modelList = new List<InstaMeetingVM>();
                if (HttpContext.Session.GetString("InstaInterviewfilters") != null)
                {
                    var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("InstaInterviewfilters"));
                    if (filter.StartDate != null && filter.EndDate != null)
                    {
                        modelList = _instaInterviewService.GetAllMeetingList(filter.StartDate.Value, filter.EndDate.Value.AddDays(1), id, emailid);
                    }
                    modelList = modelList.Select(x =>
                    {
                        x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription();
                        x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription();
                        return x;
                    }).ToList();

                    filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.RequiredSkill.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                    filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                    filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                    filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.Interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                    modelList = modelList.Where(c => filter.PrimarySkill != null && filter.PrimarySkill.Contains(c.RequiredSkill.Split("|")[0])
                    && ((filter.MeetingStatus != null && filter.MeetingStatus.Count() > 0) ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                    && ((filter.InterviewStatus != null && filter.InterviewStatus.Count() > 0) ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                    && ((filter.Interviewer != null && filter.Interviewer.Count() > 0) ? filter.Interviewer.Contains(c.Interviewer) : c.Interviewer == null)).ToList();
                }
                else
                {
                    modelList = _instaInterviewService.GetAllMeetingList(DateTime.Now.Date, DateTime.Now.Date.AddDays(1), id, emailid);
                    modelList = modelList.Select(x => { x.ConfirmationStatus = ((InstaReceiveMeetingStatus)Convert.ToInt32(x.ConfirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription(); x.MeetingStatusStatusDesc = ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription(); return x; }).ToList();
                }

                Sheet.Cells["A1:AI1"].Style.Font.Bold = true;
                Sheet.Cells["A1:AI1"].Style.Font.Size = 12;
                Sheet.Cells["A1:AI1"].Style.WrapText = true;
                //Sheet.Column(1).Width = 30;
                //Sheet.Column(2).Width = 30;
                int startColumn = 1; int endColumn = 20;
                for (int a = startColumn; a <= endColumn; a++)
                {
                    Sheet.Column(a).Width = 40;
                }
                Sheet.Cells[1, 1].Value = "Requested Date";
                Sheet.Cells[1, 2].Value = "Candidate Name";
                Sheet.Cells[1, 3].Value = "Required Skill";
                Sheet.Cells[1, 4].Value = "Assessment Group";
                if (profileName == "Client")
                {
                    Sheet.Cells[1, 5].Value = "Confirmation Status";
                    Sheet.Cells[1, 6].Value = "Meeting Status";
                    Sheet.Cells[1, 7].Value = "Interview Status";
                    Sheet.Cells[1, 8].Value = "Resume";
                    Sheet.Cells[1, 9].Value = "JD Link";
                    Sheet.Cells[1, 10].Value = "Report URL";
                }
                else
                {
                    Sheet.Cells[1, 5].Value = "Interviewer";
                    Sheet.Cells[1, 6].Value = "Zoom Link";
                    Sheet.Cells[1, 7].Value = "Zoom Id";
                    Sheet.Cells[1, 8].Value = "Confirmation Status";
                    Sheet.Cells[1, 9].Value = "Meeting Status";
                    Sheet.Cells[1, 10].Value = "Interview Status";
                    Sheet.Cells[1, 11].Value = "Resume";
                    Sheet.Cells[1, 12].Value = "JD Link";
                    Sheet.Cells[1, 13].Value = "Report URL";
                }
                int i = 2;
                if (modelList.Count() > 0)
                {
                    foreach (var item in modelList)
                    {
                        Sheet.Cells[i, 1].Value = item.RequestedDate.ToString("MM/dd/yyyy HH:mm");
                        Sheet.Cells[i, 2].Value = item.CandidateName;
                        Sheet.Cells[i, 3].Value = item.RequiredSkill;
                        Sheet.Cells[i, 4].Value = item.AssessmentGroup;
                        if (profileName == "Client")
                        {
                            Sheet.Cells[i, 5].Value = item.ConfirmationStatus;
                            if (item.MeetingStatusStatusDesc != "0")
                                Sheet.Cells[i, 6].Value = item.MeetingStatusStatusDesc;
                            else
                                Sheet.Cells[i, 6].Value = "";
                            if (item.intervieweStatusStatDesc != "0")
                                Sheet.Cells[i, 7].Value = item.intervieweStatusStatDesc;
                            else
                                Sheet.Cells[i, 7].Value = "";
                            if (!string.IsNullOrEmpty(item.ResumeLink))
                            {
                                try
                                {
                                    Sheet.Cells[i, 8].Hyperlink = new Uri(item.ResumeLink, UriKind.Absolute);
                                    Sheet.Cells[i, 8].Value = "Resume";
                                    Sheet.Cells[i, 8].Style.Font.UnderLine = true;
                                    Sheet.Cells[i, 8].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                }
                                catch (Exception)
                                {
                                    Sheet.Cells[i, 8].Value = "No Resume";
                                }
                            }
                            else
                            {
                                Sheet.Cells[i, 8].Value = "No Resume";
                            }
                            if (!string.IsNullOrEmpty(item.JDLink))
                            {
                                try
                                {
                                    Sheet.Cells[i, 9].Hyperlink = new Uri(item.JDLink, UriKind.Absolute);
                                    Sheet.Cells[i, 9].Value = "JD Link";
                                    Sheet.Cells[i, 9].Style.Font.UnderLine = true;
                                    Sheet.Cells[i, 9].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                }
                                catch (Exception)
                                {
                                    Sheet.Cells[i, 9].Value = "No JD Link";
                                }
                            }
                            else
                            {
                                Sheet.Cells[i, 9].Value = "No JD Link";
                            }
                            if (item.id != 0)
                            {
                                try
                                {
                                    string sendreporturl = "https://" + Request.Host.ToString() + "/InstaInterview/InstaAddReport?interviewId=" + item.id + "&Requestedid=" + id;
                                    Sheet.Cells[i, 10].Hyperlink = new Uri(sendreporturl, UriKind.Absolute);
                                    Sheet.Cells[i, 10].Value = "Report Url";
                                    Sheet.Cells[i, 10].Style.Font.UnderLine = true;
                                    Sheet.Cells[i, 10].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                }
                                catch (Exception)
                                {
                                    Sheet.Cells[i, 10].Value = "No url";
                                }
                            }
                            else
                            {
                                Sheet.Cells[i, 10].Value = "No url";
                            }
                        }
                        else
                        {
                            Sheet.Cells[i, 5].Value = item.Interviewer;
                            if (!string.IsNullOrEmpty(item.ZoomLink))
                            {
                                try
                                {
                                    Sheet.Cells[i, 6].Hyperlink = new Uri(item.ZoomLink, UriKind.Absolute);
                                    Sheet.Cells[i, 6].Value = "Zoom Link";
                                    Sheet.Cells[i, 6].Style.Font.UnderLine = true;
                                    Sheet.Cells[i, 6].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                }
                                catch (Exception)
                                {
                                    Sheet.Cells[i, 6].Value = "No Link";
                                }
                            }
                            else
                            {
                                Sheet.Cells[i, 6].Value = "No Link";
                            }
                            Sheet.Cells[i, 7].Value = item.AccountEmail;
                            Sheet.Cells[i, 8].Value = item.ConfirmationStatus;
                            if (item.MeetingStatusStatusDesc != "0")
                                Sheet.Cells[i, 9].Value = item.MeetingStatusStatusDesc;
                            else
                                Sheet.Cells[i, 9].Value = "";
                            if (item.intervieweStatusStatDesc != "0")
                                Sheet.Cells[i, 10].Value = item.intervieweStatusStatDesc;
                            else
                                Sheet.Cells[i, 10].Value = "";
                            if (!string.IsNullOrEmpty(item.ResumeLink))
                            {
                                try
                                {
                                    Sheet.Cells[i, 11].Hyperlink = new Uri(item.ResumeLink, UriKind.Absolute);
                                    Sheet.Cells[i, 11].Value = "Resume";
                                    Sheet.Cells[i, 11].Style.Font.UnderLine = true;
                                    Sheet.Cells[i, 11].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                }
                                catch (Exception)
                                {
                                    Sheet.Cells[i, 11].Value = "No Resume";
                                }
                            }
                            else
                            {
                                Sheet.Cells[i, 11].Value = "No Resume";
                            }
                            if (!string.IsNullOrEmpty(item.JDLink))
                            {
                                try
                                {
                                    Sheet.Cells[i, 12].Hyperlink = new Uri(item.JDLink, UriKind.Absolute);
                                    Sheet.Cells[i, 12].Value = "JD Link";
                                    Sheet.Cells[i, 12].Style.Font.UnderLine = true;
                                    Sheet.Cells[i, 12].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                }
                                catch (Exception)
                                {
                                    Sheet.Cells[i, 12].Value = "No JD Link";
                                }
                            }
                            else
                            {
                                Sheet.Cells[i, 12].Value = "No JD Link";
                            }
                            if (item.id != 0)
                            {
                                try
                                {
                                    string sendreporturl = "https://" + Request.Host.ToString() + "/InstaInterview/InstaAddReport?interviewId=" + item.id + "&Requestedid=" + id;
                                    Sheet.Cells[i, 13].Hyperlink = new Uri(sendreporturl, UriKind.Absolute);
                                    Sheet.Cells[i, 13].Value = "Report Url";
                                    Sheet.Cells[i, 13].Style.Font.UnderLine = true;
                                    Sheet.Cells[i, 13].Style.Font.Color.SetColor(System.Drawing.Color.Blue);
                                }
                                catch (Exception)
                                {
                                    Sheet.Cells[i, 13].Value = "No url";
                                }
                            }
                            else
                            {
                                Sheet.Cells[i, 13].Value = "No url";
                            }
                        }
                        i++;
                    }
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                    Rng.Value = "No data available in table !!!";
                    return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }

            }
            catch (Exception)
            {
                ExcelRange Rng = Sheet.Cells[2, 2, 2, 2];
                Rng.Value = "Data export failed !!!";
                return File(Ep.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

        }
    }
}
