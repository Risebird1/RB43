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
using System.Collections.Generic;
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
                        modelList = modelList.Where(x => x.confirmationStatus == "1" && x.VideoLink != null).OrderByDescending(x => x.interviewDate).ToList();
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

        public IActionResult VideoPage(MeetingVM model)
        {


            var modelList = _meetingService.GetMeetingList();
            modelList = modelList.Where(c => c.JDTiltle.Contains(model.JDTiltle.Split("|")[0])).ToList();
            var IRListModel = _meetingService.GetInteviewReport(model.imid, model.irfIntId).Result;
            MeetingVM NewModel = new MeetingVM();
            NewModel.SubVideo_List = modelList.Take(50).ToList();
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

            if (model.IreportModel.QuestionsCollection == null)
            {
                ViewBag.QuestionsDiscussedCount = 0;
            }
            else
            {
                ViewBag.QuestionsDiscussedCount = model.IreportModel.QuestionsCollection?.Count();
            }

            bool isQC = false;
            model.IreportModel.IsQC = isQC;
            GetStatusList(IRListModel, model.irfIntId);

            return View(NewModel);
        }


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

        //public IActionResult GetSubVideoList(string Id)
        //{
        //    try
        //    {
        //        var modelList = _meetingService.GetMeetingList();
        //        modelList = modelList.Where(c => c.JDTiltle.Contains(Id)).ToList();
        //        MeetingVM model = new MeetingVM();
        //        model.SubVideo_List = modelList.Take(5).ToList();
        //        return PartialView("_SubVideoList", model);
        //    }
        //   catch (Exception ex)
        //    {
        //         return null;
        //    }
        //}
    }
}