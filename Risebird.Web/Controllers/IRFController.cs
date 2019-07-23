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
            var irfList = _irfRepository.GetAllIRF();
            if (TempData["SuccessErrorMessage"] != null)
            {
                ViewBag.Message = TempData["SuccessErrorMessage"].ToString();
            }
            return View(irfList);
        }
        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
            ViewBag.Level = new SelectList(_irfRepository.GetLevels(), "id", "Name");
            ViewBag.Skill = new SelectList(_irfRepository.GetSkills(), "id", "Name");
            ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "FirstName");
            ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
            ViewBag.Interviewers = new SelectList(_irfRepository.GetInterviewers(), "id", "FirstName");
            ViewBag.QualityController = new SelectList(_irfRepository.GetQualityController(), "id", "FirstName");
            ViewBag.RiseBirdHR = new SelectList(_irfService.GetRiseBirdHR(), "id", "FirstName");

            return View();
        }

        [HttpPost]
        public IActionResult AddIRF(IRFVm irfDetails)
        {
            try
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

        public IActionResult AddComments(string id)
        {
            TechSkillComments model = new TechSkillComments();
            ViewBag.Skills = new SelectList(_irfService.GetTechSkills(Convert.ToInt16(id)), "id", "Name");
            model.IRFId = Convert.ToInt16(id);
            return View(model);
        }

        [HttpPost]
        public IActionResult AddComments(TechSkillComments model)
        {
            try
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
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                TempData["SuccessErrorMessage"] = "Adding IRF has failed";
                return RedirectToAction("Index");
            }
        }

        //public List<string> GetTechSkills(string id)
        //{
        //    var name = _irfService.GetTechSkills(Convert.ToInt16(id));
        //    return name;
        //}
    }
}