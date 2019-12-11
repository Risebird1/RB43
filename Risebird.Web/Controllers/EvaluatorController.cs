using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;

namespace Risebird.Web.Controllers
{
    public class EvaluatorController : Controller
    {
        private IEvaluatorService _evaluatorService;
        private IApplicationErrorLogger _applicationErrorLogger;
       
        public EvaluatorController(IEvaluatorService evaluatorService, IApplicationErrorLogger applicationErrorLogger)
        {
            _evaluatorService = evaluatorService;
            _applicationErrorLogger = applicationErrorLogger;
           
        }

        public IActionResult Index()
        {
            return View();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenResumeEvaluations()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = "ResumeEvaluator";//HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "ResumeEvaluator" || profileName == "HR")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var modelList = _evaluatorService.GetOpenEvaluations("rohan.evans@gmail.com");
                        if (TempData["SuccessErrorMessageEvaluator"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageEvaluator"].ToString();
                            TempData.Remove("SuccessErrorMessageEvaluator");
                        }
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

        [Authorize]
        //[HttpPost]
        public async Task<IActionResult> AssignYourself(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = "ResumeEvaluator";// profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "ResumeEvaluator" || profileName == "HR")
                    {
                        var result = _evaluatorService.AssignSelectabilityYourself(id, "rohan.evans@gmail.com");

                        //if (profileName== "ResumeEvaluator")
                        //{

                        //}
                        

                        TempData["SuccessErrorMessageEvaluator"] = result;
                        ViewBag.Message = result;
                        return RedirectToAction("OpenResumeEvaluations");
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
                return RedirectToAction("OpenResumeEvaluations");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> MyEvaluations()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = "ResumeEvaluator";//profileName.Replace("\"", "");
                    if (profileName == "ResumeEvaluator" || profileName == "HR")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myEvaluationList = _evaluatorService.GetAssignedSelectabilityEvaluations("rohan.evans@gmail.com");
                        foreach (var item in myEvaluationList)
                        {
                            if (!string.IsNullOrEmpty(item.ResumeLink))
                            {
                                try
                                {
                                    item.ResumeLink = new Uri(item.ResumeLink, UriKind.Absolute).ToString();

                                }
                                catch (Exception e)
                                {
                                    item.ResumeLink = "";
                                }
                            }
                            else
                            {
                                item.ResumeLink = "";
                            }

                            
                        }
                        if (TempData["SuccessErrorMessageEvaluator"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageEvaluator"].ToString();
                            TempData.Remove("SuccessErrorMessageEvaluator");
                        }

                        return View(myEvaluationList);
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
        public async Task<IActionResult> UnassignYourself(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = "ResumeEvaluator";// profileName.Replace("\"", "");
                    var email = HttpContext.Session.GetString("email");
                    email = email.Replace("\"", "");
                    if (profileName == "ResumeEvaluator" || profileName == "HR")
                    {
                        bool result;
                        result = _evaluatorService.UnAssignSelectability(id);
                        if (result)
                        {
                            TempData["SuccessErrorMessageEvaluator"] = "Unassigned successfully.";
                            return Json(new { url = Url.Action("MyEvaluations", "Evaluator") });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageEvaluator"] = "Sorry! Could not unassign!";
                            return Json(new { url = Url.Action("MyEvaluations", "Evaluator") });
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
                TempData["SuccessErrorMessageMyEvaluations"] = "Unassignment is Failed";
                return RedirectToAction("MyEvaluations");
            }
        }

        [HttpGet]
        public IActionResult SelectabilityReport(int interviewId, int irfId, bool isClient = false, bool isClientSelected = false)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = "ResumeEvaluator";//profileName.Replace("\"", "");
                if (profileName == "ResumeEvaluator")
                {
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    //var model =
                    return View();
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }

                

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("MyEvaluations");
            }
        }
    }
}