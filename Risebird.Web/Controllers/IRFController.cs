using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using Risebird.CommonHelper;

namespace Risebird.Web.Controllers
{
    public class IRFController : Controller
    {
        private IIRFRepository _irfRepository;
        private IIRFService _irfService;
        private IApplicationErrorLogger _applicationErrorLogger;
        public IRFController(IIRFRepository iRFRepository, IApplicationErrorLogger applicationErrorLogger, IIRFService irfService)
        {
            _irfRepository = iRFRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfService;
        }
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                var irfList = _irfRepository.GetAllIRF();
                if (TempData["SuccessErrorMessage"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
                }
                return View(irfList);
            }
            else
            {
                return RedirectToAction("SignOut", "Account");
            }
        }
        [HttpGet]
        public IActionResult Add()
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
                ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
                ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                ViewBag.Skill = new SelectList(_irfRepository.GetSkills(), "id", "Name");
                ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "EmailId");
                ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
                ViewBag.Interviewers = new SelectList(_irfRepository.GetInterviewers(), "id", "EmailId");
                ViewBag.QualityController = _irfService.GetQualityController(); //new SelectList(_irfRepository.GetQualityController(), "id", "FirstName",1);
                ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "EmailId");
                var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                select new { ID = (int)s, Name = s.GetDescription() };
                ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name"); ;
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Account");
            }
        }

        [HttpPost]
        public IActionResult AddIRF(IRFVm irfDetails)
        {
            try
            {
                
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (_irfService.AddIRFDetails(irfDetails).Result)
                    {
                        TempData["SuccessErrorMessage"] = "IRF is added Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Adding Skill is Failed";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Adding IRF has failed";
                return RedirectToAction("Index");
            }

        }

        public string GetInterviewerSkills(string id)
        {
            var name = _irfService.GetInterviewerSkills(Convert.ToInt16(id));
            return name;
        }
        public string GetQCSkills(string id)
        {
            return _irfService.GetQCSkills(Convert.ToInt16(id));
        }

        public IActionResult AddComments(string id)
        {
            if (HttpContext.Session.GetString("email") != null)
            {
                TechSkillComments model = new TechSkillComments();
                var result = _irfService.GetTechSkillComments(Convert.ToInt16(id));
                //if (result.Count() > 0)
                model.SkillCollection = result;
                ViewBag.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt16(id)), "id", "Name");
                model.IRFId = Convert.ToInt16(id);
                return View(model);
            }
            else
            {
                return RedirectToAction("SignOut", "Account");
            }
        }

        [HttpPost]
        public IActionResult AddComments(TechSkillComments model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (_irfService.AddSkillComments(model))
                    {
                        TempData["SuccessErrorMessage"] = "Comments saved Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "Comments were not saved.";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Comments were not saved";
                return RedirectToAction("Index");
            }
        }

        public IActionResult DeleteIRF(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (_irfService.DeleteIRF(Convert.ToInt16(id)).Result)
                    {
                        TempData["SuccessErrorMessage"] = "IRF deleted Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "IRF delete Failed";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "IRF delete Failed";
                return RedirectToAction("Index");
            }

        }

        public IActionResult GetInterviewMeetings(string id, string irfId)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    var meetings = _irfService.GetIRFInterviewMeetings(Convert.ToInt16(id));
                    ViewBag.IRF = irfId;
                    foreach (var m in meetings)
                    {
                        if (m.L2InterviewStatus.HasValue)
                        {
                            m.L2InterviewStatDesc = ((L2InterviewStatus)(m.L2InterviewStatus)).GetDescription();
                        }
                        if (m.L2InterviewStatus.HasValue)
                        {
                            m.ClientInterviewStatDesc = ((ClientInterviewStatus)(m.ClientInterviewStatus)).GetDescription();
                        }

                    }
                    return View(meetings);
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        [HttpGet]
        public IActionResult EditIRF(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
                    ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
                    ViewBag.LevelTech = new SelectList(_irfRepository.GetLevelTech(), "id", "Name");
                    ViewBag.Skill = new SelectList(_irfRepository.GetSkills(), "id", "Name");
                    ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "FirstName");
                    ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
                    ViewBag.Interviewers = new SelectList(_irfRepository.GetInterviewers(), "id", "FirstName");
                    ViewBag.QualityController = _irfService.GetQualityController();//new SelectList(_irfRepository.GetQualityController(), "id", "FirstName",1);
                    ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "FirstName");
                    var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                    select new { ID = (int)s, Name = s.GetDescription() };
                    ViewBag.JobLevelList = new SelectList(jobLevels, "ID", "Name"); ;
                    var model = _irfService.GetEditIRF(Convert.ToInt16(id)).Result;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("Index");
            }

        }

        [HttpPost]
        public IActionResult EditIRF(IRFVm model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null)
                {
                    if (_irfService.UpdateIRF(model))
                    {
                        TempData["SuccessErrorMessage"] = "IRF saved Successfully";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        TempData["SuccessErrorMessage"] = "IRF edits were not saved.";
                        return RedirectToAction("Index");
                    }

                }
                else
                {
                    return RedirectToAction("SignOut", "Account");
                }
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "IRF edits were not saved";
                return RedirectToAction("Index");
            }
        }

        //private void GetDropDownList(IRFVm model)
        //{
        //    var jobLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
        //                            select new { ID = (int)s, Name = s.GetDescription() };
        //    model.JobLevelList = new SelectList(interviewStatuses, "ID", "Name");
            
        //}

        public IActionResult Stats()
        {
            return View();
            
        }

    }
}