using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;

namespace Risebird.Web.Controllers
{
    public class CandidateController : Controller
    {

        private readonly ICandidateService _candidateService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        public CandidateController(ICandidateService candidateService, IApplicationErrorLogger applicationErrorLogger)
        {
            _candidateService = candidateService;
            _applicationErrorLogger = applicationErrorLogger;
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
