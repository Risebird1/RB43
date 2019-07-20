using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Entities.Data;
using System.Collections.Generic;
using System.Linq;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class InterviewController : Controller
    {
        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAzureStorageHelper _azureStorageHelper;
        private ISkillService _skillService;
        public InterviewController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                return View(_meetingService.GetMeetingList());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }

        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add(MeetingVM meeting)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (meeting.File == null)
                    {
                        ModelState.AddModelError(String.Empty, "Please upload a resume.");
                        var model = new MeetingVM();
                        model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                        return View("AddInterview", model);
                    }

                    byte[] fileData = new byte[meeting.File.Length];
                    string uploadedFileURL = _azureStorageHelper.InsertBlob(meeting.File.FileName, "resumes", fileData);
                    List<Skill> skills = _skillService.GetSkills();

                    if (uploadedFileURL == null)
                    {
                        ModelState.AddModelError(String.Empty, "Resume Upload is failed.");
                        var model = new MeetingVM();
                        model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                        return View("AddInterview", model);
                    }

                    else
                    {
                        meeting.resumeLink = uploadedFileURL;
                        List<String> selectedSkill = skills.Where(x => meeting.SelectedSkillIds.Contains(x.id)).Select(x => x.Name).ToList();
                        meeting.primaryskill = String.Join("| ", selectedSkill);

                        if (_meetingService.AddMeeting(meeting))
                        {
                            TempData["SuccessErrorMessage"] = "Meeting is added Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessage"] = "Meeting addition is Failed";
                            return RedirectToAction("Index");
                        }
                    }
                }
                else
                {
                    var model = new MeetingVM();                    
                    model.PrimarySkills = new SelectList(_skillService.GetSkills(), "id", "Name");

                    return View("Add",model);
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                var model = new MeetingVM();
                model.PrimarySkills= new SelectList(_skillService.GetSkills(), "id", "Name");
                return View(model);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return View();
            }
        }


        public async Task<IActionResult> OpenInterview()
        {
            try
            {
                return View(_meetingService.GetAllOpenInterviews());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IActionResult> MyInterview()
        {
            try
            {
                 var myInterview=  _meetingService.GetAllOpenInterviews();
                return View(myInterview);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IActionResult> QCInterview()
        {
            try
            {
                return View(_meetingService.GetAllOpenInterviews());
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> AssignYourself(int id, string InterviewDate, string CandidateEmail)
        {
            try
            {
                AssignYourselfVM assinAssignYourselfVm = new AssignYourselfVM();
                assinAssignYourselfVm.id = id;
                assinAssignYourselfVm.InterviewDate = InterviewDate;
                assinAssignYourselfVm.CandidateEmail = CandidateEmail;
                return View(assinAssignYourselfVm);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AssignYourself(AssignYourselfVM model)
        {
            try
            {
                var result = _meetingService.AssignYourself(model.id, model.Interviewer);
                ViewBag.Message = result;
                return RedirectToAction("OpenInterview");
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("OpenInterview");
            }
        }


    }
}