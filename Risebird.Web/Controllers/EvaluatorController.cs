using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    [Authorize]
    public class EvaluatorController : Controller
    {
        private IEvaluatorService _evaluatorService;
        private IApplicationErrorLogger _applicationErrorLogger;
        private IAzureStorageHelper _azureStorageHelper;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;


        public EvaluatorController(IEvaluatorService evaluatorService, IApplicationErrorLogger applicationErrorLogger, IAzureStorageHelper azureStorageHelper, IFileMimeTypeHelper fileMimeTypeHelper)
        {
            _evaluatorService = evaluatorService;
            _applicationErrorLogger = applicationErrorLogger;
            _azureStorageHelper = azureStorageHelper;
            _fileMimeTypeHelper = fileMimeTypeHelper;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenEvaluatorEvaluations()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "ResumeEvaluator")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var modelList = _evaluatorService.GetOpenEvaluations(emailid);
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
        [HttpGet]
        //[HttpPost]
        public async Task<IActionResult> AssignSelectabilityYourself(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "ResumeEvaluator")
                    {
                        var result = _evaluatorService.AssignSelectabilityYourself(id, emailid);

                        TempData["SuccessErrorMessageEvaluator"] = result;
                        ViewBag.Message = result;
                        return RedirectToAction("OpenEvaluatorEvaluations");
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
                return RedirectToAction("OpenEvaluatorEvaluations");
            }
        }
        
        [Authorize]
        [HttpGet]
        //[HttpPost]
        public async Task<IActionResult> AssignJoinabilityYourself(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    if (profileName == "HR")
                    {
                        var result = _evaluatorService.AssignJoinabilityYourself(id, emailid);

                        TempData["SuccessErrorMessageEvaluator"] = result;
                        ViewBag.Message = result;
                        return RedirectToAction("OpenHREvaluations");
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
                return RedirectToAction("OpenHREvaluations");
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
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "ResumeEvaluator" || profileName == "HR")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myEvaluationList = _evaluatorService.GetAssignedSelectabilityEvaluations(emailid);
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
                        else if (TempData["SuccessErrorMessageAudio"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageAudio"].ToString();
                            TempData.Remove("SuccessErrorMessageAudio");
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
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "ResumeEvaluator")
                    {
                        bool result;
                        result = _evaluatorService.UnAssignSelectability(id);
                        if (result)
                        {
                            TempData["SuccessErrorMessageEvaluator"] = "Unassignment has been Successfully!";
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
                TempData["SuccessErrorMessageEvaluator"] = "Sorry! Could not unassign!";
                return RedirectToAction("MyEvaluations");
            }
        }

        [HttpGet]
        public IActionResult SelectabilityReport(int candidateId, int irfId)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "ResumeEvaluator" || profileName == "HR")
                {
                    var model = _evaluatorService.GetEvaluatorReport(candidateId, irfId).Result;
                    model.HRReport = _evaluatorService.GetHRJoinabilityReport(candidateId, irfId).Result;
                    model.TechSkillEvaluatorReportCollection = _evaluatorService.GetTechReport(candidateId, irfId).Result;
                    model.SkillGeneralEvaluatorReportCollection = _evaluatorService.GetSkillGeneralReport(candidateId, irfId).Result;
                    model.CandidateAudioCollection = _evaluatorService.GetCandidateAudios(candidateId, irfId).Result.ToList().Where(x => !x.IsVoiceBiometric).ToList();
                    return View(model);
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectabilityReport(EvaluatorReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "ResumeEvaluator" || profileName == "HR")
                    {

                        if (_evaluatorService.AddSelectabilityReport(model).Result)
                        {
                            TempData["SuccessErrorMessageEvaluator"] = "Data has been saved Successfully!";
                            return RedirectToAction("MyEvaluations");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageEvaluator"] = "Sorry! Could not save data!";
                            return RedirectToAction("MyEvaluations");
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
                TempData["SuccessErrorMessageEvaluator"] = "Sorry! Could not save data!";
                return RedirectToAction("MyEvaluations");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> OpenHREvaluations()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "HR")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var modelList = _evaluatorService.GetEvaluationsHR(emailid);
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
        [HttpGet]
        public async Task<IActionResult> MyHREvaluations()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "HR" || profileName == "ResumeEvaluator")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myEvaluationList = _evaluatorService.GetAssignedJoinabilityEvaluations(emailid);
                        foreach (var item in myEvaluationList)
                        {
                            if (!string.IsNullOrEmpty(item.ResumeLink))
                            {
                                try
                                {
                                    item.ResumeLink = new Uri(item.ResumeLink, UriKind.Absolute).ToString();

                                }
                                catch (Exception)
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
                        else if (TempData["SuccessErrorMessageAudio"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageAudio"].ToString();
                            TempData.Remove("SuccessErrorMessageAudio");
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
        public async Task<IActionResult> UnassignHRJoinability(int id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");

                    if (profileName == "HR")
                    {
                        bool result;
                        result = _evaluatorService.UnAssignJoinability(id);
                        if (result)
                        {
                            TempData["SuccessErrorMessageEvaluator"] = "Unassignment has been done Successfully!";
                            return Json(new { url = Url.Action("MyHREvaluations", "Evaluator") });
                        }
                        else
                        {
                            TempData["SuccessErrorMessageEvaluator"] = "Sorry! Could not unassign!";
                            return Json(new { url = Url.Action("MyHREvaluations", "Evaluator") });
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
                TempData["SuccessErrorMessageEvaluator"] = "Sorry! Could not unassign!";
                return RedirectToAction("MyHREvaluations");
            }
        }

        [HttpGet]
        public IActionResult JoinabilityReport(int candidateId, int irfId)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "HR" || profileName == "ResumeEvaluator")
                {
                    var model = _evaluatorService.GetHRJoinabilityReport(candidateId, irfId).Result;
                    model.EvaluatorReport = _evaluatorService.GetEvaluatorReport(candidateId, irfId).Result;
                    model.TechSkillEvaluatorReportCollection = _evaluatorService.GetTechReport(candidateId, irfId).Result;
                    model.SkillGeneralEvaluatorReportCollection = _evaluatorService.GetSkillGeneralReport(candidateId, irfId).Result;
                    model.CandidateAudioCollection = _evaluatorService.GetCandidateAudios(candidateId, irfId).Result.ToList().Where(x => !x.IsVoiceBiometric).ToList();
                    return View(model);
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinabilityReport(HRReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "HR" || profileName == "ResumeEvaluator")
                    {
                        if (_evaluatorService.AddJoinabilityReport(model).Result)
                        {
                            TempData["SuccessErrorMessageEvaluator"] = "Data has been saved Successfully!";
                            return RedirectToAction("MyHREvaluations");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageEvaluator"] = "Sorry! Could not save data!";
                            return RedirectToAction("MyHREvaluations");
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
                TempData["SuccessErrorMessageEvaluator"] = "Sorry! Could not save data!";
                return RedirectToAction("MyHREvaluations");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RecruiterCandidates()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Recruiter")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var myCandidates = _evaluatorService.GetRecruiterCandidates(emailid, profileName);
                        foreach (var item in myCandidates)
                        {
                            if (!string.IsNullOrEmpty(item.ResumeLink))
                            {
                                try
                                {
                                    item.ResumeLink = new Uri(item.ResumeLink, UriKind.Absolute).ToString();

                                }
                                catch (Exception)
                                {
                                    item.ResumeLink = "";
                                }
                            }
                            else
                            {
                                item.ResumeLink = "";
                            }


                        }
                        if (TempData["SuccessErrorMessageRecruiter"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageRecruiter"].ToString();
                            TempData.Remove("SuccessErrorMessageRecruiter");
                        }
                        else if (TempData["SuccessErrorMessageAudio"] != null)
                        {
                            ViewBag.Message = TempData["SuccessErrorMessageAudio"].ToString();
                            TempData.Remove("SuccessErrorMessageAudio");
                        }

                        return View(myCandidates);
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

        public IActionResult RecruiterReport(int candidateId, int irfId)
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Recruiter")
                {
                    var model = _evaluatorService.GetEvaluatorReport(candidateId, irfId).Result;
                    model.HRReport = _evaluatorService.GetHRJoinabilityReport(candidateId, irfId).Result;
                    model.TechSkillEvaluatorReportCollection = _evaluatorService.GetTechReport(candidateId, irfId).Result;
                    model.SkillGeneralEvaluatorReportCollection = _evaluatorService.GetSkillGeneralReport(candidateId, irfId).Result;
                    model.CandidateAudioCollection = _evaluatorService.GetCandidateAudios(candidateId, irfId).Result.ToList().Where(x => !x.IsVoiceBiometric).ToList();
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }



            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("RecruiterCandidates");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecruiterReport(EvaluatorReport model)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Recruiter")
                    {

                        if (_evaluatorService.AddSelectabilityReport(model).Result)
                        {
                            TempData["SuccessErrorMessageRecruiter"] = "Data has been saved Successfully!";
                            return RedirectToAction("RecruiterCandidates");
                        }
                        else
                        {
                            TempData["SuccessErrorMessageRecruiter"] = "Sorry! Could not save data!";
                            return RedirectToAction("RecruiterCandidates");
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
                TempData["SuccessErrorMessageRecruiter"] = "Sorry! Could not save data!";
                return RedirectToAction("RecruiterCandidates");
            }
        }

        [HttpGet]
        public async Task<IActionResult> UploadAudio(int candidateId, int irfId, string actMethod = "")
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "ResumeEvaluator" || profileName == "HR" || profileName == "Recruiter")
                {
                    var model = await _evaluatorService.GetCandidate(candidateId, irfId);
                    TempData["actMethod"] = actMethod;
                    ViewBag.BackMethod = actMethod;
                    var list = new SelectList(new[]
                    {
                        new { ID = "Enrollment1", Name = "Enrollment 1"},
                        new { ID = "Enrollment2", Name = "Enrollment 2"  },
                        new { ID = "Enrollment3", Name = "Enrollment 3" }
                    }, "ID", "Name", "Enrollment1");


                    ViewData["list"] = list;
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }



            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction(actMethod);
            }
        }

        [Authorize]
        [HttpPost]
        [DisableRequestSizeLimit]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAudio(CandidateAudio model)
        {
            string actMethod = string.Empty;
            if (TempData["actMethod"] != null)
            {
                actMethod = TempData["actMethod"].ToString();
            }
            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "ResumeEvaluator" || profileName == "HR" || profileName == "Recruiter")
                    {
                        if (model.AudioFile != null)
                        {
                            var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(model.AudioFile);

                            if (fileMimeType == "audio/basic" || fileMimeType == "audio/mpeg"
                            || fileMimeType == "audio/x-wav")
                            {
                                await _azureStorageHelper.InsertBlob(model.AudioFile.FileName, "audios", model.AudioFile);
                                var containerRef = _azureStorageHelper.GetCloudBlobContainer("audios");
                                var blob = containerRef.GetBlockBlobReference(model.AudioFile.FileName);
                                string uploadedaudioFileURL = blob.Uri.ToString();
                                if (uploadedaudioFileURL == null)
                                {
                                    ModelState.AddModelError(String.Empty, "Audio Upload failed!");
                                    return View("UploadAudio", model);
                                }
                                else
                                {
                                    model.AudioFileLink = uploadedaudioFileURL;
                                }
                            }
                            
                        }

                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");

                        if (_evaluatorService.AddCandidateAudio(model, emailid, profileName).Result)
                        {
                            TempData["SuccessErrorMessageAudio"] = "Audio has been uploaded Successfully!";
                            return RedirectToAction(actMethod);
                        }
                        else
                        {
                            TempData["SuccessErrorMessageAudio"] = "Failed to upload Audio!";
                            return RedirectToAction(actMethod);
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
                TempData["SuccessErrorMessageAudio"] = "Audio Upload failed!";
                return RedirectToAction(actMethod);
            }
        }

        [HttpGet]
        public IActionResult EnrollVoiceItUser(int candidateId, int irfId, string actMethod = "")
        {
            try
            {

                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "ResumeEvaluator" || profileName == "HR" || profileName == "Recruiter")
                {
                    var model = (IList<CandidateAudio>)_evaluatorService.GetCandidateAudios(candidateId, irfId).Result.Where(x => x.IsVoiceBiometric == true).ToList();
                    TempData["actMethod"] = actMethod;
                    ViewBag.BackMethod = actMethod;
                    if (TempData["SuccessErrorMessageEnroll"] != null)
                    {
                        ViewBag.Message = TempData["SuccessErrorMessageEnroll"].ToString();
                        TempData.Remove("SuccessErrorMessageEnroll");
                    }
                    return View(model);
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }



            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction(actMethod);
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollVoiceItUser(IList<CandidateAudio> model)
        {
            string actionMethod = string.Empty;
            if (TempData["actMethod"] != null)
            {
                actionMethod = TempData["actMethod"].ToString();
            }
            try
            {
                if (HttpContext.Session.GetString("email") != null &&
                    HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "ResumeEvaluator" || profileName == "HR" || profileName == "Recruiter")
                    {

                        var result = _evaluatorService.EnrollCandidate(model);
                        TempData["SuccessErrorMessageEnroll"] = result;
                        return RedirectToAction("EnrollVoiceItUser", new { candidateId = model.First().CandidateId, irfId = model.First().IRFId, actMethod = actionMethod });

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
                TempData["SuccessErrorMessageEnrollment"] = "Failed!";
                return RedirectToAction("EnrollVoiceItUser", new { candidateId = model.First().CandidateId, irfId = model.First().IRFId, actMethod = actionMethod });
            }
        }
    }
}