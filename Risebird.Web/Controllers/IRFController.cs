using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Web.Controllers
{
    public class IRFController : Controller
    {
        private IIRFRepository _irfRepository;
        private IApplicationErrorLogger _applicationErrorLogger;
        public IRFController(IIRFRepository iRFRepository, IApplicationErrorLogger applicationErrorLogger)
        {
            _irfRepository = iRFRepository;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.Clients = new SelectList(_irfRepository.GetClients(), "id", "Name");
            ViewBag.Level = new SelectList( _irfRepository.GetLevels(), "id", "Name");
            ViewBag.Skill = new SelectList(_irfRepository.GetSkills(), "id", "Name");
            ViewBag.ClientUser = new SelectList(_irfRepository.GetCientUsers(), "id", "FirstName");
            ViewBag.ClientRole = new SelectList(_irfRepository.GetClientRoles(), "id", "Name");
            ViewBag.Interviewers = new SelectList(_irfRepository.GetInterviewers(), "id", "FirstName");
            ViewBag.QualityController = new SelectList(_irfRepository.GetQualityController(), "id", "FirstName");
            return View();
        }

        [HttpPost]
        public IActionResult AddIRF(IRFVm iRF)
        {
            return View();
        }
    }
}