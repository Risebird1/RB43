using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Risebird.Web.Controllers
{
    public class MCQController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly INotificationHelper _notificationHelper;
        private ISupportService _supportService;
        private ISkillService _skillService;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly IFileMimeTypeHelper _fileMimeTypeHelper;
        private readonly IMCQService _mCQService;
        private readonly IApplicationErrorLogger _applicationErrorLogger;

        #region MCQDetails
        public MCQController(IConfiguration configuration, IUserRepository userRepository, INotificationHelper notificationHelper, ISupportService supportService, ISkillService skillService, IAzureStorageHelper azureStorageHelper, IFileMimeTypeHelper fileMimeTypeHelper, IMCQService mCQService, IApplicationErrorLogger applicationErrorLogger)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _notificationHelper = notificationHelper;
            _supportService = supportService;
            _skillService = skillService;
            _azureStorageHelper = azureStorageHelper;
            _fileMimeTypeHelper = fileMimeTypeHelper;
            _mCQService = mCQService;
            _applicationErrorLogger = applicationErrorLogger;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    var result = _mCQService.FetchAllMCQSets();
                    var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    var seniorityLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    var employmentTypes = from EmploymentTypeOptions s in Enum.GetValues(typeof(EmploymentTypeOptions))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    //var Compentencs = from CompetencyLevel s in Enum.GetValues(typeof(CompetencyLevel))
                    //                  select new { ID = (int)s, Name = s.GetDescription() };
                    var skillset = _skillService.GetSkills();
                    var resultset = new MCQDropdowndetails();

                    resultset._ProfileTypeSkills.ProfileTypeList = new SelectList(Heuristics, "ID", "Name");
                    resultset._ProfileTypeSkills.SeniorityLevelList = new SelectList(seniorityLevels, "ID", "Name");
                    resultset._ProfileTypeSkills.EmploymentTypeOptions = new SelectList(employmentTypes, "ID", "Name");
                    //resultset._ProfileTypeSkills.CompentencyLevelList = new SelectList(Compentencs, "ID", "Name");

                    resultset._ProfileTypeSkills.PrimarySkillsList = new SelectList(skillset, "id", "Name");
                    foreach (var item in result ?? new List<MCQVMset>())
                    {
                        if(!string.IsNullOrEmpty(item.ProfileType))
                        {
                            var listProfileTypes = new List<string>();
                            foreach (var profile in item.ProfileType.Split(','))
                            {
                                var name = Enum.GetName(typeof(Heuristicsscorelevel), Convert.ToInt16(profile));
                                listProfileTypes.Add(name);
                            }
                            item.ProfileTypeName = string.Join(",", listProfileTypes);
                        }
                        //item.ProfileTypeName = Enum.GetName(typeof(Heuristicsscorelevel), item.ProfileType);
                        //item.CompentencyLevelName = Enum.GetName(typeof(CompetencyLevel), item.CompentencyLevel);
                    }
                    if (HttpContext.Session.GetString("MCQSetList") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<MCQFilter>(HttpContext.Session.GetString("MCQSetList"));
                        filter.PrimarySkill = filter.PrimarySkill == null ? filter.PrimarySkill = result.Select(x => x.PrimarySkills).ToArray() : filter.PrimarySkill = filter.PrimarySkill;
                        filter.SecondarySkill = filter.SecondarySkill == null ? filter.SecondarySkill = result.Select(x => x.SecondarySkill).ToArray() : filter.SecondarySkill = filter.SecondarySkill;


                        if (result != null && result.Count() > 0)
                        {
                            bool continueApplyingFilter = true;

                            if (filter.PrimarySkill != null)
                            {
                                IEnumerable<MCQVMset> resultFilteredByPrimarySkill = null;

                                resultFilteredByPrimarySkill = result.Where(c => filter.PrimarySkill.Contains(c.PrimarySkills));
                                if (resultFilteredByPrimarySkill != null)
                                {
                                    result = resultFilteredByPrimarySkill;
                                }
                            }
                            if (filter.SecondarySkill != null)
                            {
                                IEnumerable<MCQVMset> resultFilteredBySecondarySkill = null;

                                resultFilteredBySecondarySkill = result.Where(c => filter.SecondarySkill.Contains(c.SecondarySkill));
                                
                                
                                if (resultFilteredBySecondarySkill != null && resultFilteredBySecondarySkill.Count() > 0)//filtering only if there is nmore than one in the resultFilteredBySecondarySkill variable  
                                {
                                    result = resultFilteredBySecondarySkill;
                                }
                            }

                            if (!string.IsNullOrEmpty(filter.CountryType))
                            {
                                IEnumerable<MCQVMset> resultFilteredByCountryType = null;

                                resultFilteredByCountryType = result.Where(c => c.CountryType.HasValue && c.CountryType.Value == Convert.ToInt16(filter.CountryType));
                                //result = resultFilteredByCountryType;
                                if (resultFilteredByCountryType != null && resultFilteredByCountryType.Count() > 0)
                                {
                                    result = resultFilteredByCountryType;
                                }
                                else
                                {
                                    continueApplyingFilter = false;
                                }
                            }


                            if (!string.IsNullOrEmpty(filter.CompanyType) && continueApplyingFilter)
                            {
                                IEnumerable<MCQVMset> resultFilteredByCompanyType = null;

                                resultFilteredByCompanyType = result.Where(c => c.CompanyType.HasValue && c.CompanyType.Value == Convert.ToInt16(filter.CompanyType));
                                if (resultFilteredByCompanyType != null && resultFilteredByCompanyType.Count() > 0)
                                {
                                    result = resultFilteredByCompanyType;
                                }
                                else
                                {
                                    continueApplyingFilter = false;
                                }
                            }


                            if (!string.IsNullOrEmpty(filter.SeniorityLevel) && continueApplyingFilter)
                            {
                                IEnumerable<MCQVMset> resultFilteredBySeniority = null;

                                resultFilteredBySeniority = result.Where(c => c.SeniorityLevel != null && c.SeniorityLevel.Contains(filter.SeniorityLevel));
                                if (resultFilteredBySeniority != null && resultFilteredBySeniority.Count() > 0)
                                {
                                    result = resultFilteredBySeniority;
                                }
                                else
                                {
                                    continueApplyingFilter = false;
                                }
                            }


                            if (!string.IsNullOrEmpty(filter.DifficultyLevel) && continueApplyingFilter)
                            {
                                IEnumerable<MCQVMset> resultFilteredByDifficulty = null;

                                resultFilteredByDifficulty = result.Where(c => c.ProfileType != null && c.ProfileType.Contains(filter.DifficultyLevel));
                                if (resultFilteredByDifficulty != null && resultFilteredByDifficulty.Count() > 0)
                                {
                                    result = resultFilteredByDifficulty;
                                }
                                else
                                {
                                    continueApplyingFilter = false;
                                }
                            }


                            if (!string.IsNullOrEmpty(filter.EmploymentType) && continueApplyingFilter)
                            {
                                IEnumerable<MCQVMset> resultFilteredByEmploymentType = null;

                                resultFilteredByEmploymentType = result.Where(c => c.EmploymentType != null && c.EmploymentType.Contains(filter.EmploymentType));
                                if (resultFilteredByEmploymentType != null && resultFilteredByEmploymentType.Count() > 0)
                                {
                                    result = resultFilteredByEmploymentType;
                                }
                            }

                        }

                        result = result?.ToList();
                        //result = result.Where(c => (filter.PrimarySkill.Count() > 0 ? filter.PrimarySkill.Contains(c.PrimarySkills) : c.PrimarySkills == null)
                        //        && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.ProfileTypeName) : c.ProfileTypeName == null)
                        //        && (filter.SecondarySkill.Count() > 0 ? filter.SecondarySkill.Contains(c.SecondarySkill) : c.SecondarySkill == null)
                        //        && (filter.CompentencyLevel.Count() > 0 ? filter.CompentencyLevel.Contains(c.CompentencyLevelName) : c.CompentencyLevelName == null)).ToList();
                    }
                    resultset._MCQVMset = result != null ? (List<MCQVMset>)result : new List<MCQVMset>();

                    //ViewBag.ProfileType = Heuristics.Select(x => x.Name).Distinct().ToList();
                    //ViewBag.Skills = result.Select(x => x.PrimarySkills).Distinct().ToList();
                    //ViewBag.SecondarySkills = result.Select(x => x.SecondarySkill).Distinct().ToList();
                    //ViewBag.CompentencyLevel = Compentencs.Select(x => x.Name).Distinct().ToList();
                    ViewBag.TemplateUrl = _configuration["MCQTemplateUrl"].ToString();
                    ViewBag.Profile = profileName;
                    return View(resultset);
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

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Index(MCQFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("MCQSetList", JsonConvert.SerializeObject(filter));
                        }
                        return RedirectToAction("Index");
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
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ResetMCQFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("MCQSetList");
                    return RedirectToAction("Index");
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

        [HttpGet]
        public async Task<IActionResult> Download(string filename)
        {
            if (filename == null)
                return Content("filename not present");

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Template", filename);
            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, GetContentType(path), Path.GetFileName(path));
        }

        [HttpGet]
        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        [HttpGet]
        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformatsofficedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
        }

        [HttpPost]
        public async Task<JsonResult> UploadQuestions()
        {
            var value = Request.Form["title"].ToString();
            var time = Request.Form["time"].ToString();
            var status = Request.Form["status"].ToString();
            var partialid = Request.Form["PartialId"].ToString();
            //var ProfileType = Convert.ToInt32(Request.Form["ProfileType"].ToString());
            var ProfileType = Request.Form["ProfileType"].ToString();
            var PrimarySkill = Request.Form["PrimarySkill"].ToString();
            var SecondarySkill = Request.Form["SecondarySkill"].ToString();
            var SeniorityLevel = Request.Form["SeniorityLevel"].ToString();
            var EmploymentType = Request.Form["EmploymentType"].ToString();
            var CompanyType = Convert.ToInt32(Request.Form["CompanyType"].ToString());
            var CountryType = Convert.ToInt32(Request.Form["CountryType"].ToString());
            //var PerHourRate = Convert.ToInt32(Request.Form["PerHourRate"].ToString());
            //var id = Convert.ToInt32(Request.Form["id"].ToString());
            //var CompentencyLevel = Convert.ToInt32(Request.Form["CompentencyLevel"].ToString());
            int number = 1;
            MCQVMset _lstset = new MCQVMset();
            var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
            _lstset.PartitionKey = date;
            _lstset.Title = value;
            _lstset.Timer = time;
            _lstset.ProfileType = ProfileType;
            _lstset.PrimarySkills = PrimarySkill;
            //_lstset.CompentencyLevel = CompentencyLevel;
            _lstset.SecondarySkill = SecondarySkill;
            _lstset.SeniorityLevel = SeniorityLevel;
            _lstset.EmploymentType = EmploymentType;
            _lstset.CompanyType = CompanyType;
            _lstset.CountryType = CountryType;
            //_lstset.PerHourRate = PerHourRate;
            if (status == "Edit")
            {
                var id = Convert.ToInt32(Request.Form["id"].ToString());

                _lstset.PartitionKey = partialid;
                _lstset.id = id;
            }
            if (Request.Form.Files.Count() > 0)
            {
                var file = Request.Form.Files[0];
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                List<MCQVM> _lstMCQ = new List<MCQVM>();
                using (var stream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        int i = 0;
                        while (reader.Read()) //Each ROW
                        {
                            if (i == 0)
                            {
                                if (reader.GetValue(1).ToString() == "QuestionType" && reader.GetValue(2).ToString() == "Question" && reader.GetValue(3).ToString() == "Option1" && reader.GetValue(4).ToString() == "Option2"
                                    && reader.GetValue(5).ToString() == "Option3" && reader.GetValue(6).ToString() == "Option4" && reader.GetValue(7).ToString() == "Answer" && reader.GetValue(8).ToString() == "Explanation")
                                {

                                }
                                else
                                {
                                    return Json("File format is invilid, please download templete");
                                }
                            }
                            else if (i == 1)
                            {

                            }
                            else
                            {
                                if (reader.GetValue(1) == null || reader.GetValue(2) == null || reader.GetValue(3) == null || reader.GetValue(4) == null
                                   || reader.GetValue(5) == null || reader.GetValue(6) == null || reader.GetValue(7) == null)
                                {
                                    if (reader.GetValue(1) == null && reader.GetValue(2) == null && reader.GetValue(3) == null && reader.GetValue(4) == null
                                   && reader.GetValue(5) == null && reader.GetValue(6) == null && reader.GetValue(7) == null)
                                    {

                                    }
                                    else
                                    {
                                        return Json("Question Fileds should not be blank.");
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                                        _lstMCQ.Add(new MCQVM
                                        {
                                            PartitionKey = date1,
                                            QuestionType = reader.GetValue(1).ToString(),
                                            Question = reader.GetValue(2).ToString().Replace("\n", "<br/>"),
                                            Option1 = reader.GetValue(3).ToString(),
                                            Option2 = reader.GetValue(4).ToString(),
                                            Option3 = reader.GetValue(5).ToString(),
                                            Option4 = reader.GetValue(6).ToString(),
                                            Answer = reader.GetValue(7).ToString(),
                                            Explanation = reader.GetValue(8) == null ? reader.GetValue(7).ToString() : "",
                                            MCQId = _lstset.PartitionKey,
                                            QuestionNumber = number
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        return Json(ex.Message);
                                    }
                                    number++;
                                }
                            }
                            i++;
                        }
                    }
                }
                System.IO.File.Delete(path);
                _lstset.QuestionCount = _lstMCQ.Count();
                var MCQId = _mCQService.AddorUpdateMCQSet(_lstset);
                _mCQService.AddMCQQuestions(_lstMCQ, _lstset.PartitionKey);
            }
            return Json("Successfully Uploaded");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Questionpage(string Id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    var questions = _mCQService.GetMCQquestionsByMCQId(Id);
                    var model = questions.Where(x => x.MCQId == Id).OrderBy(x => x.QuestionNumber).ToList();
                    return View(model);
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

        [Authorize]
        [HttpPost]
        public async Task<JsonResult> DeleteMCQ(string Id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin")
                {
                    _mCQService.DeleteMCQSet(Id);
                    return Json("Successfully deleted");
                }
                else
                {
                    return Json("You are not authorized to delete MCQ.");
                }
            }
            else
            {
                return Json("Fail to delete, please try again.");
            }
        }
        #endregion

        #region CandidateSkillTest
        [HttpGet]
        public IActionResult SkillTest(string Id)
        {
            ViewBag.Questionsetid = Id;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CandidateQuestionpage(CandidateDetailsForTest values)
        {
            string Id = values.MCQId;
            if (!ModelState.IsValid)
            {
                ViewBag.Questionsetid = Id;
                return View("SkillTest", values);
            }
            var candidateShare = _mCQService.GetShareMCQEmailListByMCQId(Id);
            if (candidateShare.Where(x => x.EmailAddress.ToUpper() == values.CandidateEmail.ToUpper() && x.MCQId == Id).Count() == 0)
            {
                return RedirectToAction("TestComplete", new { status = "Unauthorized" });
            }

            var itemshareupdate = candidateShare.FirstOrDefault();
            _mCQService.UpdateShareMCQEmailStatusByid(itemshareupdate.id, 1);

            values.ApplyingCompany = itemshareupdate.ApplyingCompany;
            values.ClientHR = itemshareupdate.ClientHR;
            var candidateDetails = _mCQService.GetMCQCandidateListByMCQId(Id);
            if (candidateDetails.Where(x => x.MCQId == Id && x.CandidateEmail.ToUpper() == values.CandidateEmail.ToUpper()).Count() > 0)
            {
                return RedirectToAction("TestComplete", new { status = "TestCompleted" });
            }
            //Check Time Validation
            var _candidate = candidateShare.Where(x => x.EmailAddress.ToUpper() == values.CandidateEmail.ToUpper() && x.MCQId == Id).FirstOrDefault();
            if (_candidate.Expiretime > 0)
            {
                if (_candidate.ShareDate.AddHours(_candidate.Expiretime) <= DateTime.Now)
                {
                    return RedirectToAction("TestComplete", new { status = "TimeExpired" });
                }
            }
            HttpContext.Session.SetString("TestCandidateDetails", JsonConvert.SerializeObject(values));
            var questions = _mCQService.GetMCQquestionsByMCQId(Id);
            var questionstimmer = _mCQService.FetchMCQSetById(Id);
            var model = questions.OrderBy(x => x.QuestionNumber).ToList();
            foreach (var item in model)
            {
                item.QuestionId = item.PartitionKey;
            }
            HttpContext.Session.SetString("QuestionSet", JsonConvert.SerializeObject(model));
            QuestionDetailsList modelset = new QuestionDetailsList();
            modelset._timmerdetails = questionstimmer.Where(x => x.PartitionKey == Id).FirstOrDefault();
            modelset._timmerdetails.Timer = (Convert.ToInt32(modelset._timmerdetails.Timer) * 60).ToString();
            ViewBag.VideoRecording = itemshareupdate.Videorecording;
            return View(modelset);
        }

        [HttpPost]
        public async Task<JsonResult> CandidateSelectQuestionsD(string id)
        {
            var resultset = JsonConvert.DeserializeObject<List<MCQVM>>(HttpContext.Session.GetString("QuestionSet"));
            var values = JsonConvert.DeserializeObject<CandidateDetailsForTest>(HttpContext.Session.GetString("TestCandidateDetails"));
            HttpContext.Session.SetString("TestCandidateDetails", JsonConvert.SerializeObject(values));

            HttpContext.Session.SetString("QuestionSet", JsonConvert.SerializeObject(resultset));
            var questionvalue = Convert.ToInt32(id);
            var result = resultset.Where(x => x.QuestionNumber == questionvalue).FirstOrDefault();
            if (resultset.Count() == questionvalue)
            {
                result.final = "true";
            }
            QuestionDetailsList modeldetailset = new QuestionDetailsList();
            var _questionlist = (from _Q in resultset
                                 select new QuestionList
                                 {
                                     QuestionNumber = _Q.QuestionNumber,
                                     QuestionAnswered = String.IsNullOrEmpty(_Q.QuestionAnswer) ? false : true
                                 }).ToList();
            modeldetailset._question = result;
            modeldetailset._questionlist = _questionlist;
            return Json(modeldetailset);
        }

        [HttpPost]
        public async Task<JsonResult> CandidateQuestionsD()
        {
            var Option1 = Convert.ToBoolean(Request.Form["Option1"].ToString());
            var Option2 = Convert.ToBoolean(Request.Form["Option2"].ToString());
            var Option3 = Convert.ToBoolean(Request.Form["Option3"].ToString());
            var Option4 = Convert.ToBoolean(Request.Form["Option4"].ToString());
            var SingleAnswer = Request.Form["SingleAnswer"].ToString();
            var QuestionNumber = Convert.ToInt32(Request.Form["QuestionNumber"].ToString());
            var final = Request.Form["final"].ToString();
            var QuestionType = Request.Form["QuestionType"].ToString();


            var resultset = JsonConvert.DeserializeObject<List<MCQVM>>(HttpContext.Session.GetString("QuestionSet"));
            var values = JsonConvert.DeserializeObject<CandidateDetailsForTest>(HttpContext.Session.GetString("TestCandidateDetails"));
            HttpContext.Session.SetString("TestCandidateDetails", JsonConvert.SerializeObject(values));
            foreach (var item in resultset.Where(x => x.QuestionNumber == QuestionNumber))
            {
                item.MultiAnswerOption1 = Option1;
                item.MultiAnswerOption2 = Option2;
                item.MultiAnswerOption3 = Option3;
                item.MultiAnswerOption4 = Option4;
                item.SingleAnswer = SingleAnswer;
                if (QuestionType == "Single")
                {
                    item.QuestionAnswer = SingleAnswer;
                    if (SingleAnswer.ToUpper() == item.Answer.ToUpper())
                    {
                        item.Rating = 1;
                    }
                }
                else
                {
                    var flag = true;
                    if (Option1 == true)
                    {
                        item.QuestionAnswer = "1";
                        if (!item.Answer.ToUpper().Contains("1"))
                        {
                            flag = false;
                        }
                    }
                    if (Option2 == true)
                    {
                        if (String.IsNullOrEmpty(item.QuestionAnswer))
                            item.QuestionAnswer = "2";
                        else
                            item.QuestionAnswer = item.QuestionAnswer + ",2";
                        if (!item.Answer.ToUpper().Contains("2"))
                        {
                            flag = false;
                        }
                    }
                    if (Option3 == true)
                    {
                        if (String.IsNullOrEmpty(item.QuestionAnswer))
                            item.QuestionAnswer = "3";
                        else
                            item.QuestionAnswer = item.QuestionAnswer + ",3";
                        if (!item.Answer.ToUpper().Contains("3"))
                        {
                            flag = false;
                        }
                    }
                    if (Option4 == true)
                    {
                        if (String.IsNullOrEmpty(item.QuestionAnswer))
                            item.QuestionAnswer = "4";
                        else
                            item.QuestionAnswer = item.QuestionAnswer + ",4";
                        if (!item.Answer.ToUpper().Contains("4"))
                        {
                            flag = false;
                        }
                    }
                    if (Option1 == false && Option2 == false && Option3 == false && Option4 == false)
                    {
                        flag = false;
                    }
                    if (flag == true)
                    {
                        item.Rating = 1;
                    }
                    else
                    {
                        item.Rating = 0;
                    }
                }
                break;
            }


            if (final == "true")
            {
                //if (Request.Form.Files.Count() > 0)
                //{
                //    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(Request.Form.Files[0]);
                //    if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4"
                //        || fileMimeType == "video/quicktime")
                //    {
                //        var file = Request.Form.Files[0];
                //        var filename = values.CandidateEmail.Split("@")[0].ToString() + "_" + Guid.NewGuid().ToString() + ".mp4";
                //        await _azureStorageHelper.InsertBlob(filename, "videos", file);
                //        var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                //        var blob = containerRef.GetBlockBlobReference(filename);
                //        string uploadedvideoFileURL = blob.Uri.ToString();
                //        values.VideoLink = uploadedvideoFileURL;
                //    }
                //}
                HttpContext.Session.SetString("TestCandidateDetails", JsonConvert.SerializeObject(values));
                HttpContext.Session.SetString("QuestionSet", JsonConvert.SerializeObject(resultset));
                return Json("");
            }
            HttpContext.Session.SetString("QuestionSet", JsonConvert.SerializeObject(resultset));
            var questionvalue = QuestionNumber + 1;
            var result = resultset.Where(x => x.QuestionNumber == questionvalue).FirstOrDefault();
            if (resultset.Count() == questionvalue)
            {
                result.final = "true";
            }
            QuestionDetailsList modeldetailset = new QuestionDetailsList();
            var _questionlist = (from _Q in resultset
                                 select new QuestionList
                                 {
                                     QuestionNumber = _Q.QuestionNumber,
                                     QuestionAnswered = String.IsNullOrEmpty(_Q.QuestionAnswer) ? false : true
                                 }).ToList();
            modeldetailset._question = result;
            modeldetailset._questionlist = _questionlist;
            return Json(modeldetailset);
        }

        [HttpGet]
        public IActionResult TestComplete(string status)
        {
            ViewBag.Action = status;
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> SubmitTest()
        {
            try
            {
                var rating = Convert.ToInt32(Request.Form["rating"].ToString());
                var comments = Request.Form["comments"].ToString();

                var resultset = JsonConvert.DeserializeObject<List<MCQVM>>(HttpContext.Session.GetString("QuestionSet"));
                var values = JsonConvert.DeserializeObject<CandidateDetailsForTest>(HttpContext.Session.GetString("TestCandidateDetails"));
                var _list = new CandidateDetailsForTest();
                var _id = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                _list.PartitionKey = _id;
                //_list.RowKey = Guid.NewGuid().ToString();
                _list.MCQId = values.MCQId;
                _list.Skills = values.Skills;
                _list.TotalExprience = values.TotalExprience;
                _list.CandidateEmail = values.CandidateEmail;
                _list.ApplyingCompany = values.ApplyingCompany;
                _list.ClientHR = values.ClientHR;
                _list.CandidateScore = resultset.Sum(x => x.Rating);
                _list.TotalQuestions = resultset.Count();
                _list.VideoLink = values.VideoLink;
                _list.FeedbackComment = comments;
                _list.Feedbackreating = rating;
                //if (Request.Form.Files.Count() > 0)
                //{
                //    var fileMimeType = _fileMimeTypeHelper.GetFileMimeType(Request.Form.Files[0]);
                //    if (fileMimeType == "video/mpeg" || fileMimeType == "video/mp4"
                //        || fileMimeType == "video/quicktime")
                //    {
                //        var file = Request.Form.Files[0];
                //        var filename = values.CandidateEmail.Split("@")[0].ToString() + "_" + Guid.NewGuid().ToString() + ".mp4";
                //        await _azureStorageHelper.InsertBlob(filename, "videos", file);
                //        var containerRef = _azureStorageHelper.GetCloudBlobContainer("videos");
                //        var blob = containerRef.GetBlockBlobReference(filename);
                //        string uploadedvideoFileURL = blob.Uri.ToString();
                //        _list.VideoLink = uploadedvideoFileURL;
                //    }
                //}
                _mCQService.AddMCQCandidates(_list);
                foreach (var item in resultset)
                {
                    var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    item.PartitionKey = date1;
                    item.CandidateID = _id;
                }
                _mCQService.AddMCQCandidateQuestions(resultset);
                //Send mail to support team
                var HrEmailaddresslist = _supportService.GetAllSupportHR();
                string HrEmailaddress = string.Empty;
                foreach (var item in HrEmailaddresslist)
                {
                    if (String.IsNullOrEmpty(HrEmailaddress))
                    {
                        HrEmailaddress = item.EmailId;
                    }
                    else
                    {
                        HrEmailaddress = HrEmailaddress + ";" + item.EmailId;
                    }
                }
                int candidatescore = ((resultset.Sum(x => x.Rating) * 100) / resultset.Count());
                Mail _mail = new Mail
                {
                    TO = values.ClientHR,
                    CC = HrEmailaddress,
                    Subject = "Candidate " + values.CandidateEmail + " Completed Test for " + values.ApplyingCompany
                };
                var resultretrun = _notificationHelper.CandidateCompleteTestLink(_mail, values.CandidateEmail, values.ApplyingCompany, candidatescore);
                return Json("TestSubmit");
            }
            catch (Exception)
            {
                return Json("Failed to submit Test");
            }
        }
        #endregion

        #region MCQ Actions
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CandidateTestDetails(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var isclient = false;
                var clientname = string.Empty;
                var candidateset = new List<CandidateDetailsForTestDuplicate>();
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    candidateset = (List<CandidateDetailsForTestDuplicate>)_mCQService.GetMCQCandidateDetailsByMCQId(id);
                    if (candidateset != null)
                    {
                        if (profileName == "Client")
                        {
                            var emailid = HttpContext.Session.GetString("email")?.Trim();
                            emailid = emailid.Replace("\"", "");
                            var client = _userRepository.GetClientUserDetails(emailid);
                            if (client != null)
                            {
                                clientname = client.Result.ClientName.ToUpper();
                                isclient = true;
                                candidateset = candidateset.Where(x => x.ApplyingCompany.ToUpper() == clientname).ToList();
                            }
                        }
                        if (HttpContext.Session.GetString("MCQCandidateList") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("MCQCandidateList"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                candidateset = candidateset.Where(t => t.Sharetime >= filter.StartDate && t.Sharetime <= filter.EndDate).ToList();
                            }
                            if (profileName != "Client")
                            {
                                filter.ClientHR = filter.ClientHR == null ? filter.ClientHR = candidateset.Select(x => x.ClientHR).ToArray() : filter.ClientHR = filter.ClientHR;
                                filter.SelectionStatus = filter.SelectionStatus == null ? filter.SelectionStatus = candidateset.Select(x => x.ApplyingCompany).ToArray() : filter.SelectionStatus = filter.SelectionStatus;
                                candidateset = candidateset.Where(c => (filter.ClientHR.Count() > 0 ? filter.ClientHR.Contains(c.ClientHR) : c.ClientHR == null)
                                        && (filter.SelectionStatus.Count() > 0 ? filter.SelectionStatus.Contains(c.ApplyingCompany) : c.ApplyingCompany == null)).ToList();
                            }
                        }
                        ViewBag.ID = id;
                        ViewBag.Isclient = isclient;
                        ViewBag.Clients = candidateset.Select(x => x.ApplyingCompany).Distinct().ToList();
                        ViewBag.ClientHR = candidateset.Select(x => x.ClientHR).Distinct().ToList();
                        return View(candidateset);
                    }
                }
                return View(candidateset);
            }
            else
            {
                return RedirectToAction("SignOut", "Session");
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CandidateTestDetails(InterviewFilter filter, string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("MCQCandidateList", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("CandidateTestDetails", new { id = id });
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

        public IActionResult ResetMCQCandidateFilter(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("MCQCandidateList");
                    return RedirectToAction("CandidateTestDetails", new { id = id });
                }
                else
                {
                    return RedirectToAction("SignOut", "Session");
                }
            }
            catch (Exception)
            {
                return null;
            }

        }

        [HttpPost]
        public async Task<JsonResult> DeleteCandidate(string id, string indexid)
        {
            try
            {
                _mCQService.DeleteMCQCandidate(id, indexid);
                return Json("success");
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public async Task<JsonResult> ShareTestLink(string Emailids, string Id, string ClientName, string ClientEmail, DateTime MeetingDate, int Expiretime)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var resultset = _mCQService.FetchMCQSetById(Id);
                    var details = resultset.Where(x => x.PartitionKey == Id).FirstOrDefault();
                    var timmer = details.Timer + " min";
                    string currenturl = "https://" + Request.Host.ToString() + "/MCQ/SkillTest?Id=" + Id;
                    if (profileName == "Client")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var client = _userRepository.GetClientUserDetails(emailid);
                        if (client != null)
                        {
                            ClientName = client.Result.ClientName.ToUpper();
                        }
                    }
                    if (!String.IsNullOrEmpty(ClientName))
                    {
                        ClientName = "";
                    }
                    MCQVMShare _list = new MCQVMShare();
                    _list.status = 0;
                    _list.MCQSet = details.Title;
                    _list.ClientHR = ClientEmail;
                    _list.ApplyingCompany = ClientName;

                    foreach (var item in Emailids.Split(";"))
                    {
                        try
                        {
                            var _meetinglinks = ShareRisebirdMeeting("Risebird", item, MeetingDate);
                            Mail _mail = new Mail
                            {
                                TO = item,
                                Subject = "Invitation for " + details.Title + " : " + ClientName
                            };
                            var result = _notificationHelper.SharedTestLink(_mail, details.Title, timmer, currenturl, ClientName, _meetinglinks.meetingAttendee, MeetingDate, 0);
                            Mail _mail1 = new Mail
                            {
                                TO = ClientEmail,
                                Subject = "Invitation for " + details.Title + " : " + item
                            };
                            _notificationHelper.SharedTestLinkClient(_mail1, details.Title, timmer, currenturl, item, _meetinglinks.meetingAgent, MeetingDate, 0);
                            if (result.Contains("Mail sent successfully"))
                            {
                                var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                                _list.PartitionKey = date1;
                                _list.MCQId = Id;
                                _list.EmailAddress = item;
                                _list.roomId = _meetinglinks.roomId;
                                _list.Expiretime = Expiretime;
                                _mCQService.AddMCQSetShareList(_list);
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                    return Json("success");
                }
                else
                {
                    return Json("success");
                }
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        #endregion
        public ShareMCQRisebirdMeetingResponse ShareRisebirdMeeting(string agentname, string visitorName, DateTime datetime)
        {
            var _model = new ShareMCQRisebirdMeeting();
            _model.agentname = agentname;
            _model.visitorName = visitorName;
            _model.datetime = datetime.ToString();
            var _meeting = new RestClient("https://meet.risebird.io/api/v1/room");
            var request = new RestRequest(Method.POST);
            request.AddHeader("authorization", "v7sy3J8cAFdEJp5S");
            request.AddBody(_model);
            IRestResponse response = _meeting.Execute(request);
            var content = response.Content;
            var convertedResponse = JsonConvert.DeserializeObject<ShareMCQRisebirdMeetingResponse>(content);
            if (convertedResponse != null)
            {
                return convertedResponse;
            }
            return new ShareMCQRisebirdMeetingResponse();
        }
        public async Task<IActionResult> CandidateMCQReport(string MCQId, string EmailId)
        {
            ViewBag.PublicLayout = false;
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                ViewBag.PublicLayout = true;
            }
            else
            {
                HttpContext.Session.SetString("Logo", "");
            }
            var candidateset = _mCQService.GetMCQCandidateDetailsByMCQId(MCQId);
            EmailId = EmailId.ToUpper();
            var _candidate = candidateset.Where(x => x.CandidateEmail.ToUpper() == EmailId).FirstOrDefault();
            var questions = _mCQService.GetMCQCandidateQuestions(_candidate.Id);
            _candidate.QuestionDetails = (List<MCQVM>)(questions.OrderBy(x => x.QuestionNumber).ToList());
            if (String.IsNullOrEmpty(_candidate.VideoLink))
            {
                if (!String.IsNullOrEmpty(_candidate.roomId))
                {
                    var _meeting = new RestClient("https://meet.risebird.io/api/v1/room/" + _candidate.roomId);
                    var request = new RestRequest(Method.GET);
                    request.AddHeader("authorization", "v7sy3J8cAFdEJp5S");
                    IRestResponse response = _meeting.Execute(request);
                    var content = response.Content;
                    var convertedResponse = JsonConvert.DeserializeObject<ShareMCQRisebirdMeetingResponse>(content);
                    _candidate.VideoLink = convertedResponse.videoUrl;
                    _mCQService.UpdateVideoURLForCandidate(_candidate);
                }
            }
            return View(_candidate);
        }
    }
}
