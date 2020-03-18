using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class RiseTubeController : Controller
    {

        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAzureStorageHelper _azureStorageHelper;
        private ISkillService _skillService;
        private IIRFService _iRFService;
        private IIRFRepository _iRFRepository;

        public RiseTubeController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService, IIRFRepository iRFRepository, IIRFService iRFService)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _iRFRepository = iRFRepository;
            _iRFService = iRFService;
        }


        /// <summary>
        /// Method to show all list of video if confirmation status is 1 or Selected and video link not equals to null on index page
        /// </summary>
        /// <param name="Page"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> Index(int? Page)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        var modelList = _meetingService.GetMeetingList();
                        modelList = modelList.Where(x => x.confirmationStatus == "1" && (x.InterviewStatus == 2 || x.InterviewStatus == 4) && x.VideoLink != null).OrderByDescending(x => x.interviewDate).ToList();


                        if (HttpContext.Session.GetString("RiseTubefilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("RiseTubefilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = _meetingService.GetMeetingList();
                                modelList = modelList.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate && t.confirmationStatus == "1" && (t.InterviewStatus == 2 || t.InterviewStatus == 4) && t.VideoLink != null).OrderByDescending(x => x.interviewDate).ToList();
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                                x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                                x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                                x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x;
                            }).ToList();
                            modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                            modelList = modelList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && filter.RFID.Contains(c.IRFId)
                            && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                            && (filter.ConfirmationStatus.Count() > 0 ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                            && (filter.MeetingStatus.Count() > 0 ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            && (filter.Interviewer.Count() > 0 ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();


                        }
                        return View(modelList.ToPagedList(Page ?? 1, 12));
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

        /// <summary>
        /// Method to apply filters on index page
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="Page"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Index(InterviewFilter filter, int? Page)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("RiseTubefilters", JsonConvert.SerializeObject(filter));
                        }
                        var modelList = _meetingService.GetMeetingList();
                        modelList = modelList.Where(x => x.confirmationStatus == "1" && (x.InterviewStatus == 2 || x.InterviewStatus == 4) && x.VideoLink != null).OrderByDescending(x => x.interviewDate).ToList();


                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            modelList = modelList.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                        }


                        modelList = modelList.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x;
                        }).ToList();

                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                        filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                        filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                        filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                        filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                        filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                        modelList = modelList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && filter.RFID.Contains(c.IRFId)
                        && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                        && (filter.ConfirmationStatus.Count() > 0 ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                        && (filter.MeetingStatus.Count() > 0 ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                        && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                        && (filter.Interviewer.Count() > 0 ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();

                        return View(modelList.ToPagedList(Page ?? 1, 12));
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

        /// <summary>
        /// Method to reset applied filters on index page
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ResetRisetubeFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("RiseTubefilters");
                    return RedirectToAction("Index");
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

        /// <summary>
        /// Function to Redirect mainframe page risetube on interview id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> VideoPage(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {

                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        var modelList1 = _meetingService.GetMeetingList();
                        MeetingVM model = _meetingService.GetMeetingbyid(id);
                        //MeetingVM model = modelList.Where(a => a.imid == id).FirstOrDefault();
                        modelList1 = modelList1.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0]) && c.confirmationStatus == "1" && (c.InterviewStatus == 2 || c.InterviewStatus == 4) && c.VideoLink != null).OrderByDescending(x => x.interviewDate).ToList();
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;


                        var modelList = _meetingService.GetMeetingList();
                        modelList = modelList.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0]) && c.confirmationStatus == "1" && (c.InterviewStatus == 2 || c.InterviewStatus == 4) && c.VideoLink != null).OrderByDescending(x => x.interviewDate).ToList();

                        if (HttpContext.Session.GetString("VideoPagefilters") != null)
                        {

                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("VideoPagefilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = _meetingService.GetMeetingList();
                                modelList = modelList.Where(t => t.JDTiltle.Contains(model.JDTiltle.Split("|")[0]) && t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate && t.confirmationStatus == "1" && (t.InterviewStatus == 2 || t.InterviewStatus == 4) && t.VideoLink != null).OrderByDescending(x => x.interviewDate).ToList();
                            }
                            modelList = modelList.Select(x =>
                            {
                                x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                                x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                                x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                                x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x;
                            }).ToList();
                            modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);

                            filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                            filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                            filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                            filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                            filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                            filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                            filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                            modelList = modelList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                            && filter.RFID.Contains(c.IRFId)
                            && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                            && (filter.ConfirmationStatus.Count() > 0 ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                            && (filter.MeetingStatus.Count() > 0 ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                            && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                            && (filter.Interviewer.Count() > 0 ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();
                        }

                        MeetingVM NewModel = new MeetingVM();

                        if (HttpContext.Session.GetString("VideoPagefilters") != null)
                        {
                            NewModel.SubVideo_List = modelList.ToList();
                        }
                        else
                        {
                            NewModel.SubVideo_List = modelList1.ToList();
                        }

                        NewModel.candidateName = model.candidateName;
                        NewModel.CandidateId = model.CandidateId;
                        NewModel.interviewDate = model.interviewDate;
                        NewModel.imid = model.imid;
                        NewModel.IRFId = model.IRFId;
                        NewModel.ClientName = model.ClientName;
                        NewModel.InterviewerComment = model.InterviewerComment;
                        NewModel.InterviewStatus = model.InterviewStatus;
                        NewModel.JDTiltle = model.JDTiltle;
                        NewModel.JDLink = model.JDLink;
                        NewModel.MeetingStatus = model.MeetingStatus;
                        NewModel.confirmationStatus = model.confirmationStatus;
                        NewModel.VideoLink = model.VideoLink;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.reportLink = model.reportLink;
                        NewModel.resumeLink = model.resumeLink;
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel;

                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }

                        bool isQC = false;
                        NewModel.IreportModel.IsQC = isQC;
                        GetStatusList(IRListModel, model.irfIntId);




                        return View(NewModel);
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

        /// <summary>
        /// Method to apply filters on mainframe VideoPage
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> VideoPage(InterviewFilter filter, int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR")
                    {
                        if (TempData["SuccessErrorMessageInterview"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageInterview"].ToString();
                            TempData.Remove("SuccessErrorMessageInterview");
                        }
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("VideoPagefilters", JsonConvert.SerializeObject(filter));
                        }



                        var modelList = _meetingService.GetMeetingList();
                        MeetingVM model = _meetingService.GetMeetingbyid(id);
                        //MeetingVM model = modelList.Where(a => a.imid == id).FirstOrDefault();
                        modelList = modelList.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0]) && c.confirmationStatus == "1" && (c.InterviewStatus == 2 || c.InterviewStatus == 4) && c.VideoLink != null).OrderByDescending(x => x.interviewDate).ToList();
                        var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;

                        if (filter.StartDate != null && filter.EndDate != null)
                        {
                            modelList = modelList.Where(t => t.JDTiltle.Contains(model.JDTiltle.Split("|")[0]) &&  t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                        }
                        modelList = modelList.Select(x =>
                        {
                            x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription();
                            x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty;
                            x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty;
                            x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x;
                        }).ToList();

                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = modelList.Select(x => x.JDTiltle.Split("|")[0]).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.RFID = filter.RFID == null ? filter.RFID = modelList.Select(x => x.IRFId).ToArray() : filter.RFID = filter.RFID;
                        filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = modelList.Select(x => x.ClientHrContact).ToArray() : filter.ClientHR = filter.ClientHR;
                        filter.ConfirmationStatus = filter.ConfirmationStatus == null ? filter.ConfirmationStatus = modelList.Select(x => x.confirmationStatus).ToArray() : filter.ConfirmationStatus = filter.ConfirmationStatus;
                        filter.MeetingStatus = filter.MeetingStatus == null ? filter.MeetingStatus = modelList.Select(x => x.MeetingStatusStatusDesc).ToArray() : filter.MeetingStatus = filter.MeetingStatus;
                        filter.InterviewStatus = filter.InterviewStatus == null ? filter.InterviewStatus = modelList.Select(x => x.intervieweStatusStatDesc).ToArray() : filter.InterviewStatus = filter.InterviewStatus;
                        filter.Interviewer = filter.Interviewer == null ? filter.Interviewer = modelList.Select(x => x.interviewer).ToArray() : filter.Interviewer = filter.Interviewer;

                        modelList = modelList.Where(c => filter.PrimarySkill.Contains(c.JDTiltle.Split("|")[0])
                        && filter.RFID.Contains(c.IRFId)
                        && (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHrContact) : c.ClientHrContact == null)
                        && (filter.ConfirmationStatus.Count() > 0 ? filter.ConfirmationStatus.Contains(c.confirmationStatus) : c.confirmationStatus == null)
                        && (filter.MeetingStatus.Count() > 0 ? filter.MeetingStatus.Contains(c.MeetingStatusStatusDesc) : c.MeetingStatusStatusDesc == string.Empty)
                        && (filter.InterviewStatus.Count() > 0 ? filter.InterviewStatus.Contains(c.intervieweStatusStatDesc) : c.intervieweStatusStatDesc == string.Empty)
                        && (filter.Interviewer.Count() > 0 ? filter.Interviewer.Contains(c.interviewer) : c.interviewer == null)).ToList();
                        MeetingVM NewModel = new MeetingVM();


                        NewModel.SubVideo_List = modelList.ToList();
                        NewModel.candidateName = model.candidateName;
                        NewModel.CandidateId = model.CandidateId;
                        NewModel.interviewDate = model.interviewDate;
                        NewModel.imid = model.imid;
                        NewModel.IRFId = model.IRFId;
                        NewModel.ClientName = model.ClientName;
                        NewModel.InterviewerComment = model.InterviewerComment;
                        NewModel.InterviewStatus = model.InterviewStatus;
                        NewModel.JDTiltle = model.JDTiltle;
                        NewModel.JDLink = model.JDLink;
                        NewModel.MeetingStatus = model.MeetingStatus;
                        NewModel.confirmationStatus = model.confirmationStatus;
                        NewModel.VideoLink = model.VideoLink;
                        NewModel.candidateEmail = model.candidateEmail;
                        NewModel.reportLink = model.reportLink;
                        NewModel.resumeLink = model.resumeLink;
                        NewModel.zoomLink = model.zoomLink;
                        NewModel.IreportModel = IRListModel;

                        if (NewModel.IreportModel.QuestionsCollection == null)
                        {
                            ViewBag.QuestionsDiscussedCount = 0;
                        }
                        else
                        {
                            ViewBag.QuestionsDiscussedCount = NewModel.IreportModel.QuestionsCollection?.Count();
                        }

                        bool isQC = false;
                        NewModel.IreportModel.IsQC = isQC;
                        GetStatusList(IRListModel, model.irfIntId);


                        return View(NewModel);
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

        /// <summary>
        /// Method to reset applied filters on mainframe VideoPage
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> ResetVideoPageFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("VideoPagefilters");
                    return Json("Success");
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


        /// <summary>
        /// Method to show all report data on mainframe VideoPage
        /// </summary>
        /// <param name="model"></param>
        /// <param name="irfId"></param>
        private void GetStatusList(InterviewReport model, int irfId = 0)
        {
            var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                    select new { ID = (int)s, Name = s.GetDescription() };
            model.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

            var meetingStatuses = from MeetingStatus s in Enum.GetValues(typeof(MeetingStatus))
                                  select new { ID = (int)s, Name = s.GetDescription() };

            model.MeetingStatusList = new SelectList(meetingStatuses, "ID", "Name");

            model.Level = new SelectList(_iRFRepository.GetLevels(), "id", "Name");
            model.LevelTech = new SelectList(_iRFRepository.GetLevelTech(), "id", "Name");
            var questionsLevel = from QuestionsLevel s in Enum.GetValues(typeof(QuestionsLevel))
                                 select new { ID = (int)s, Name = s.GetDescription() };
            model.QuestionsLevelList = new SelectList(questionsLevel, "ID", "Name");

            model.QuestionsList = new SelectList(_meetingService.GetTechSkillComments(irfId), "Comment", "Comment");
        }




    }
}