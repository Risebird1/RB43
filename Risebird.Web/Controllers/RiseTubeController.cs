using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using X.PagedList.Mvc.Core;

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

        [Authorize]
        public async Task<IActionResult> Index( int? Page)
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
                        //modelList = modelList.Where(x => x.interviewDate.Date == DateTime.Now.Date).ToList();

                       
                        modelList = modelList.Where(x => x.confirmationStatus == "1" && x.MeetingStatus == 1 && x.VideoLink != null).ToList();

                        modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((InterviewSlots)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                        modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);
                        if (HttpContext.Session.GetString("Interviewfilters") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("Interviewfilters"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                modelList = _meetingService.GetMeetingList();
                                modelList = modelList.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
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

                            //if (modelList.Count == 0 && filter.StartDate != null && filter.EndDate != null)
                            //{
                            //    modelList = _meetingService.GetMeetingList();
                            //    modelList = modelList.Select(x => { x.confirmationStatus = ((ReceiveMeetingStatus)Convert.ToInt32(x.confirmationStatus)).GetDescription(); x.intervieweStatusStatDesc = x.InterviewStatus.HasValue ? ((InterviewStatus)Convert.ToInt32(x.InterviewStatus)).GetDescription() : string.Empty; x.MeetingStatusStatusDesc = x.MeetingStatus.HasValue ? ((MeetingStatus)Convert.ToInt32(x.MeetingStatus)).GetDescription() : string.Empty; x.interviewSlot = ((ReceiveMeetingStatus)Convert.ToInt32(x.interviewSlot)).GetDescription(); return x; }).ToList();
                            //    modelList = modelList.Where(t => t.interviewDate.Date >= filter.StartDate && t.interviewDate.Date <= filter.EndDate).ToList();
                            //    modelList.ForEach(x => x.NextLevelApprovalStat = x.IsApproved.HasValue ? ((NextLevelApprovalStatus)x.IsApproved).GetDescription() : string.Empty);

                            //}
                        }
                        return View(modelList.ToPagedList(Page ?? 1,12));
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

        public IActionResult VideoPage(MeetingVM model)
        {
            return View(model);
        }
    }
}