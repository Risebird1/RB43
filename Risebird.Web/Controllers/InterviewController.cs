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
using System.Reflection;
using Risebird.Repository.Interface;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class InterviewController : Controller
    {
        private IMeetingService _meetingService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAzureStorageHelper _azureStorageHelper;
        private ISkillService _skillService;
        private IIRFRepository _iRFRepository;
        public InterviewController(IMeetingService meetingService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, ISkillService skillService, IIRFRepository iRFRepository)
        {
            _meetingService = meetingService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _skillService = skillService;
            _iRFRepository = iRFRepository;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.Message = TempData["SuccessErrorMessageInterview"];
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
                            TempData["SuccessErrorMessageInterview"] = "Meeting is added Successfully";
                            return RedirectToAction("Index");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageInterview"] = "Meeting addition is Failed";
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
                TempData["SuccessErrorMessageInterview"] = "Meeting addition is Failed";
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
                var myInterview=  _meetingService.GetAllAssignedInterviews();
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
        public async Task<IActionResult> AddComments(int interviewId)
        {
            try
            {
                InterviewComments comments = _meetingService.GetInterviewComments(interviewId);
                if (comments == null)
                    comments = new InterviewComments();
                
                var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                               select new { ID = (int)s, Name =s.GetDescription()};
                comments.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

                var fraudCheckStatuses = from FraudCheckStatus s in Enum.GetValues(typeof(FraudCheckStatus))
                                         select new { ID = (int)s, Name = s.GetDescription()};
                comments.FraudCheckStatusList = new SelectList(fraudCheckStatuses, "ID", "Name");

                var qcStatuses = from QCStatus s in Enum.GetValues(typeof(QCStatus))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                comments.QCStatusList = new SelectList(qcStatuses, "ID", "Name");

                var trainingStatuses = from TrainingStatus s in Enum.GetValues(typeof(TrainingStatus))
                                         select new { ID = (int)s, Name = s.GetDescription() };
                comments.TrainingStatusList = new SelectList(trainingStatuses, "ID", "Name");

                return View("InterviewComments", comments);
            }
            catch(Exception e)
            {
               await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
               return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddComments(InterviewComments interviewComments)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_meetingService.UpdateComments(interviewComments))
                    {
                        TempData["SuccessErrorMessageInterview"] = "Comments are updated Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessageInterview"] = "Comment update is failed";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    InterviewComments comments = new InterviewComments();
                    var interviewStatuses = from InterviewStatus s in Enum.GetValues(typeof(InterviewStatus))
                                            select new { ID = (int)s, Name = s.GetDescription() };
                    comments.InterviewStatusList = new SelectList(interviewStatuses, "ID", "Name");

                    var fraudCheckStatuses = from FraudCheckStatus s in Enum.GetValues(typeof(FraudCheckStatus))
                                             select new { ID = (int)s, Name = s.GetDescription() };
                    comments.FraudCheckStatusList = new SelectList(fraudCheckStatuses, "ID", "Name");

                    var qcStatuses = from QCStatus s in Enum.GetValues(typeof(QCStatus))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    comments.FraudCheckStatusList = new SelectList(fraudCheckStatuses, "ID", "Name");

                    var trainingStatuses = from TrainingStatus s in Enum.GetValues(typeof(TrainingStatus))
                                           select new { ID = (int)s, Name = s.GetDescription() };
                    comments.FraudCheckStatusList = new SelectList(fraudCheckStatuses, "ID", "Name");

                    return View("InterviewComments", comments);
                }
            }
            catch(Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                TempData["SuccessErrorMessageInterview"] = "Comment update is failed";
                return RedirectToAction("Index");
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

        public async Task<IActionResult> UnassignYourself(int id)
        {
            try
            {
                if (_meetingService.UnassignYourself(id))
                {
                    TempData["SuccessErrorMessageMyInterview"] = "Unassignment is done Successfully";
                    return Json(new { url = Url.Action("MyInterview", "Interview") });
                }
                else
                {
                    TempData["SuccessErrorMessageMyInterview"] = "Unassignment is Failed";
                    return Json(new { url = Url.Action("MyInterview", "Interview") });
                }
            }

            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessageMyInterview"] = "Unassignment is Failed";
                return RedirectToAction("MyInterview");
            }
        }

        [HttpGet]
        public IActionResult AddReport(int interviewId, int irfId)
        {
            try
            {
                ViewBag.Level = new SelectList(_iRFRepository.GetLevels(), "id", "Name");
                var model = _meetingService.GetInteviewReport(interviewId, irfId).Result;
                return View(model);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyInterview");
            }

        }

        [HttpPost]
        public IActionResult AddReport(InterviewReport model)
        {
            try
            {
                if (_meetingService.AddInterviewReport(model).Result)
                {
                    TempData["SuccessErrorMessage"] = "Report saved Successfully";
                    return RedirectToAction("MyInterview");
                }
                else
                {
                    TempData["SuccessErrorMessage"] = "Report was not saved.";
                    return RedirectToAction("MyInterview");
                }

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Report was not saved.";
                return RedirectToAction("Index");
            }
        }


    }

    public static class EnumExtensionMethods
    {
        public static string GetDescription(this Enum GenericEnum)
        {
            Type genericEnumType = GenericEnum.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
            if ((memberInfo != null && memberInfo.Length > 0))
            {
                var _Attribs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
                if ((_Attribs != null && _Attribs.Count() > 0))
                {
                    return ((System.ComponentModel.DescriptionAttribute)_Attribs.ElementAt(0)).Description;
                }
            }
            return GenericEnum.ToString();
        }

    }
}