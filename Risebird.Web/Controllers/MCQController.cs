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
        private readonly IIRFRepository _iRFRepository;
        private readonly IIRFService _irfService;
        private readonly IClientService _clientService;
        private readonly IAadhaarService _aadhaarService;

        #region MCQDetails
        public MCQController(IConfiguration configuration, IUserRepository userRepository, INotificationHelper notificationHelper, ISupportService supportService, ISkillService skillService, IAzureStorageHelper azureStorageHelper, IFileMimeTypeHelper fileMimeTypeHelper, IMCQService mCQService, IApplicationErrorLogger applicationErrorLogger, IIRFRepository iRFRepository, IIRFService irfService, IClientService clientService, IAadhaarService aadhaarService)
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
            _iRFRepository = iRFRepository;
            _irfService = irfService;
            _aadhaarService = aadhaarService;
            _clientService = clientService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin" || profileName == "SeniorHR")
                {
                    string clientname = string.Empty;
                    if (profileName == "Client")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var client = _userRepository.GetClientUserDetails(emailid);
                        if (client != null)
                        {
                            clientname = client.Result.ClientName.ToUpper();
                        }
                    }
                    var result = await _mCQService.FetchAllMCQSets(clientname, profileName, false);
                    if (profileName == "Client")
                    {
                        List<MCQVMset> filteredByClient = new List<MCQVMset>();
                        foreach (var item in result)
                        {
                            if (!string.IsNullOrEmpty(item.Clients))
                            {
                                var clientsMapped = item.Clients.Split(",");
                                if (clientsMapped != null && clientsMapped.Any(x => !string.IsNullOrEmpty(x) && x.ToLower() == clientname?.ToLower()))
                                {
                                    filteredByClient.Add(item);
                                }
                            }
                        }
                        result = filteredByClient;
                    }
                    var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    var seniorityLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    var employmentTypes = from EmploymentTypeOptions s in Enum.GetValues(typeof(EmploymentTypeOptions))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    //var Compentencs = from CompetencyLevel s in Enum.GetValues(typeof(CompetencyLevel))
                    //                  select new { ID = (int)s, Name = s.GetDescription() };
                    var skillset = await _skillService.GetSkills();

                    var resultset = new MCQDropdowndetails();

                    resultset._ProfileTypeSkills.ProfileTypeList = new SelectList(Heuristics, "ID", "Name");
                    resultset._ProfileTypeSkills.SeniorityLevelList = new SelectList(seniorityLevels, "ID", "Name");
                    resultset._ProfileTypeSkills.EmploymentTypeOptions = new SelectList(employmentTypes, "ID", "Name");
                    //resultset._ProfileTypeSkills.CompentencyLevelList = new SelectList(Compentencs, "ID", "Name");

                    resultset._ProfileTypeSkills.PrimarySkillsList = new SelectList(skillset, "id", "Name");

                    resultset.ClientList = new SelectList(_clientService.GetAllClients().Result, "Name", "Name");

                    foreach (var item in result ?? new List<MCQVMset>())
                    {
                        if (!string.IsNullOrEmpty(item.ProfileType))
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
                            //if (filter.SecondarySkill != null)
                            //{
                            //    IEnumerable<MCQVMset> resultFilteredBySecondarySkill = null;

                            //    resultFilteredBySecondarySkill = result.Where(c => filter.SecondarySkill.Contains(c.SecondarySkill));


                            //    if (resultFilteredBySecondarySkill != null && resultFilteredBySecondarySkill.Count() > 0)//filtering only if there is nmore than one in the resultFilteredBySecondarySkill variable  
                            //    {
                            //        result = resultFilteredBySecondarySkill;
                            //    }
                            //}

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
                    ViewBag.profile = profileName;
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
                    if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin" || profileName == "SeniorHR")
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
            var AadhaarVerification = Convert.ToBoolean(Request.Form["AadhaarVerification"]);
            var PremiumMCQ = Convert.ToBoolean(Request.Form["PremiumMCQ"]);
            var clients = Request.Form["Clients"].ToString();
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
            _lstset.AadhaarVerification = AadhaarVerification;
            _lstset.PremiumMCQ = PremiumMCQ;
            _lstset.Clients = clients;
            //_lstset.PerHourRate = PerHourRate;
            if (status == "Edit")
            {
                var id = Convert.ToInt32(Request.Form["id"].ToString());
                _lstset.PartitionKey = partialid;
                _lstset.id = id;
                if (Request.Form.Files.Count() == 0)
                {
                    var MCQId = _mCQService.AddorUpdateMCQSet(_lstset);
                }
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
                await _mCQService.AddMCQQuestions(_lstMCQ, _lstset.PartitionKey);
            }
            return Json("Successfully Uploaded");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Questionpage(string Id, string Type)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin" || profileName == "SupportAdmin" || profileName == "SeniorHR" || profileName == "FreemiumClient")
                {
                    var questions = await _mCQService.GetMCQquestionsByMCQId(Id);
                    var model = questions.Where(x => x.MCQId == Id).OrderBy(x => x.QuestionNumber).ToList();
                    ViewBag.Type = Type;
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
                    await _mCQService.DeleteMCQSet(Id);
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
        public async Task<IActionResult> SkillTest(string Id, string uniqueid)
        {
            var result = await _mCQService.FetchMCQSetById(Id);
            ViewBag.Questionsetid = Id;
            var model = new CandidateDetailsForTest();
            model.MCQId = Id;
            model.uniqueid = uniqueid;
            model.AadhaarVerification = result.FirstOrDefault().AadhaarVerification;
            return View(model);
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
            var candidateShare = await _mCQService.GetShareMCQEmailListByMCQId(Id);
            if (candidateShare.Where(x => x.EmailAddress.ToUpper() == values.CandidateEmail.ToUpper() && x.MCQId == Id).Count() == 0)
            {
                return RedirectToAction("TestComplete", new { status = "Unauthorized" });
            }

            var itemshareupdate = candidateShare.FirstOrDefault();
            await _mCQService.UpdateShareMCQEmailStatusByid(itemshareupdate.id, 1);

            values.ApplyingCompany = itemshareupdate.ApplyingCompany;
            values.ClientHR = itemshareupdate.ClientHR;
            var candidateDetails = await _mCQService.GetMCQCandidateListByMCQId(Id);
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
            var questions = await _mCQService.GetMCQquestionsByMCQId(Id);
            var questionstimmer = await _mCQService.FetchMCQSetById(Id);
            var model = questions.OrderBy(x => x.QuestionNumber).ToList();
            foreach (var item in model)
            {
                item.QuestionId = item.PartitionKey;
            }
            QuestionDetailsList modelset = new QuestionDetailsList();
            modelset._timmerdetails = questionstimmer.Where(x => x.PartitionKey == Id).FirstOrDefault();
            modelset._timmerdetails.Timer = (Convert.ToInt32(modelset._timmerdetails.Timer) * 60).ToString();
            if (HttpContext.Session.GetString("QuestionSet") != null)
            {
                modelset._timmerdetails.Timer = "";
            }
            else
            {
                HttpContext.Session.SetString("QuestionSet", JsonConvert.SerializeObject(model));
            }
            ViewBag.VideoRecording = itemshareupdate.Videorecording;
            return View(modelset);
        }

        [HttpPost]
        public JsonResult CandidateSelectQuestionsD(string id)
        {
            var resultset = JsonConvert.DeserializeObject<List<MCQVM>>(HttpContext.Session.GetString("QuestionSet"));
            var values = JsonConvert.DeserializeObject<CandidateDetailsForTest>(HttpContext.Session.GetString("TestCandidateDetails"));
            HttpContext.Session.SetString("TestCandidateDetails", JsonConvert.SerializeObject(values));

            HttpContext.Session.SetString("QuestionSet", JsonConvert.SerializeObject(resultset));

            var questionvalue = Convert.ToInt32(id);
            if (HttpContext.Session.GetString("TestCandidateQuestion") != null && questionvalue == 1)
            {
                questionvalue = JsonConvert.DeserializeObject<int>(HttpContext.Session.GetString("TestCandidateQuestion"));
            }
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
                                     QuestionAnswered = string.IsNullOrEmpty(_Q.QuestionAnswer) ? false : true
                                 }).ToList();
            modeldetailset._question = result;
            modeldetailset._questionlist = _questionlist;
            return Json(modeldetailset);
        }

        [HttpPost]
        public JsonResult CandidateQuestionsD()
        {
            var Option1 = Convert.ToBoolean(Request.Form["Option1"].ToString());
            var Option2 = Convert.ToBoolean(Request.Form["Option2"].ToString());
            var Option3 = Convert.ToBoolean(Request.Form["Option3"].ToString());
            var Option4 = Convert.ToBoolean(Request.Form["Option4"].ToString());
            var SingleAnswer = Request.Form["SingleAnswer"].ToString();
            var QuestionNumber = Convert.ToInt32(Request.Form["QuestionNumber"].ToString());
            var final = Request.Form["final"].ToString();
            var QuestionType = Request.Form["QuestionType"].ToString();

            HttpContext.Session.SetString("TestCandidateQuestion", JsonConvert.SerializeObject(QuestionNumber));
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
                        if (string.IsNullOrEmpty(item.QuestionAnswer))
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
                        if (string.IsNullOrEmpty(item.QuestionAnswer))
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
                        if (string.IsNullOrEmpty(item.QuestionAnswer))
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
                                     QuestionAnswered = string.IsNullOrEmpty(_Q.QuestionAnswer) ? false : true
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
                await _mCQService.AddMCQCandidates(_list);
                if (!string.IsNullOrWhiteSpace(values.Name))
                {
                    var aadhaarKYCmodel = new AadhaarData();
                    aadhaarKYCmodel.Name = values.Name;
                    aadhaarKYCmodel.Address = values.Address;
                    aadhaarKYCmodel.gender = values.Gender;
                    aadhaarKYCmodel.dob = values.DOB;
                    aadhaarKYCmodel.doc_face = values.CandidateImage;
                    aadhaarKYCmodel.email = values.CandidateEmail;
                    aadhaarKYCmodel.reference_id = values.MCQId;
                    aadhaarKYCmodel.verified_using = "MCQ";
                    await _aadhaarService.SaveAadhaarDetails(aadhaarKYCmodel);
                }
                foreach (var item in resultset)
                {
                    var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    item.PartitionKey = date1;
                    item.CandidateID = _id;
                }
                await _mCQService.AddMCQCandidateQuestions(resultset);
                //Send mail to support team
                var HrEmailaddresslist = await _supportService.GetAllSupportHR();
                string HrEmailaddress = string.Empty;
                foreach (var item in HrEmailaddresslist)
                {
                    if (string.IsNullOrEmpty(HrEmailaddress))
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
                var resultretrun = await _notificationHelper.CandidateCompleteTestLink(_mail, values.CandidateEmail, values.ApplyingCompany, candidatescore);
                HttpContext.Session.Remove("TestCandidateQuestion");
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
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin")
                {
                    candidateset = (List<CandidateDetailsForTestDuplicate>)await _mCQService.GetMCQCandidateDetailsByMCQId(id, false);
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
        public IActionResult CandidateTestDetails(InterviewFilter filter, string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin")
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
                await _mCQService.DeleteMCQCandidate(id, indexid);
                return Json("success");
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public async Task<JsonResult> ShareTestLink(string Emailids, string Id, string ClientName, string ClientEmail, DateTime MeetingDate, int Expiretime, bool Candidate, bool botactive)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    var resultset = await _mCQService.FetchMCQSetById(Id);
                    var details = resultset.Where(x => x.PartitionKey == Id).FirstOrDefault();
                    var timmer = details.Timer + " min";
                    string currenturl = "https://" + Request.Host.ToString() + "/MCQ/SkillTest?Id=" + Id;
                    var botlinkslist = new MCQBotResponse();
                    if (botactive)
                    {
                        currenturl = currenturl + "&uniqueid=322";
                        var aadharverificationflag = "N";
                        var _interviewerflag = "N";
                        if (details.AadhaarVerification)
                        {
                            aadharverificationflag = "Y";
                        }
                        //if (Candidate)
                        //{
                        //    _interviewerflag = "Y";
                        //}
                        botlinkslist = await GenerateBotLink(Emailids.Replace(";", ","), aadharverificationflag, currenturl, _interviewerflag);
                    }
                    else
                    {
                        currenturl = currenturl + "&uniqueid=448";
                    }
                    if (profileName == "Client")
                    {
                        var client = _userRepository.GetClientUserDetails(emailid);
                        if (client != null)
                        {
                            ClientName = client.Result.ClientName.ToUpper();
                        }
                    }
                    if (string.IsNullOrEmpty(ClientName))
                    {
                        ClientName = "";
                    }
                    MCQVMShare _list = new MCQVMShare();
                    _list.status = 0;
                    _list.MCQSet = details.Title;
                    _list.ClientHR = ClientEmail;
                    _list.ApplyingCompany = ClientName;
                    _list.WhoShared = emailid;

                    foreach (var item in Emailids.Split(";"))
                    {
                        try
                        {
                            var result = string.Empty;
                            var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                            var _meetinglinks = new ShareMCQRisebirdMeetingResponse();
                            if (botactive)
                            {
                                var _testurl = botlinkslist.emailBotUrls.Where(x => x.email == item).FirstOrDefault().botTestLink;
                                _meetinglinks.roomId = _testurl.Split("/").LastOrDefault();
                                Mail _mail = new Mail
                                {
                                    TO = item,
                                    Subject = "Invitation for " + details.Title + " : " + ClientName
                                };
                                result = await _notificationHelper.SharedBotTestLink(_mail, details.Title, timmer, _testurl, ClientName);
                            }
                            else
                            {
                                _meetinglinks = ShareRisebirdMeeting("Risebird", item.Trim(), MeetingDate);
                                if (string.IsNullOrWhiteSpace(_meetinglinks.roomId))
                                {
                                    _meetinglinks = ShareRisebirdMeeting("Risebird", item.Trim(), MeetingDate);
                                }
                                Mail _mail = new Mail
                                {
                                    TO = item,
                                    Subject = "Invitation for " + details.Title + " : " + ClientName
                                };
                                result = await _notificationHelper.SharedTestLink(_mail, details.Title, timmer, currenturl, ClientName, _meetinglinks.meetingAttendee, MeetingDate, -330);
                                //var uniqueid = await _aadhaarService.FetchUniqueIdByEmailid(item);
                                string Reporturl = "https://" + Request.Host.ToString() + "/MCQ/CandidateMCQReport?MCQId=" + Id + "&SMCQId=" + date1;
                                Mail _mail1 = new Mail
                                {
                                    TO = ClientEmail,
                                    Subject = "Invitation for " + details.Title + " : " + item
                                };
                                await _notificationHelper.SharedTestLinkClient(_mail1, details.Title, timmer, currenturl, item, _meetinglinks.meetingAgent, MeetingDate, Reporturl, -330);
                            }
                            if (result.Contains("Mail sent successfully"))
                            {
                                _list.PartitionKey = date1;
                                _list.MCQId = Id;
                                _list.EmailAddress = item.Trim();
                                _list.roomId = _meetinglinks.roomId;
                                _list.Expiretime = Expiretime;
                                _list.ShareType = Candidate;
                                _list.botactive = botactive;
                                await _mCQService.AddMCQSetShareList(_list);
                            }
                        }
                        catch (Exception ex)
                        {
                            await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
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
            DateTime currentTime = TimeZoneInfo.ConvertTime(datetime, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
            var datetime1 = currentTime.ToString("yyyy-MM-dd'T'HH:mm:ss.000Z");
            _model.datetime = datetime1;
            var _meeting = new RestClient("https://meet.risebird.io/api/v1/room");
            var request = new RestRequest("https://meet.risebird.io/api/v1/room", Method.Post);
            request.AddHeader("authorization", "v7sy3J8cAFdEJp5S");
            request.AddBody(_model);
            RestResponse response = _meeting.Execute(request);
            var content = response.Content;
            var convertedResponse = JsonConvert.DeserializeObject<ShareMCQRisebirdMeetingResponse>(content);

            if (convertedResponse != null)
            {
                return convertedResponse;
            }
            return new ShareMCQRisebirdMeetingResponse();
        }
        public async Task<IActionResult> CandidateMCQReport(string MCQId, string CMCQId, string SMCQId)
        {
            try
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
                var _candidate = new CandidateDetailsForTestDuplicate();
                var candidateset = await _mCQService.GetMCQReportByMCQId(MCQId);
                if (!string.IsNullOrWhiteSpace(CMCQId))
                {
                    _candidate = candidateset.Where(x => x.Id == CMCQId).FirstOrDefault();
                }
                else
                {
                    _candidate = candidateset.Where(x => x.SMCQId == SMCQId).FirstOrDefault();
                }
                if (_candidate == null)
                {
                    _candidate = new CandidateDetailsForTestDuplicate();
                }
                var questions = await _mCQService.GetMCQCandidateQuestions(_candidate.Id);
                if (questions == null)
                {
                    questions = new List<MCQVM>();
                }
                _candidate.QuestionDetails = (List<MCQVM>)(questions.OrderBy(x => x.QuestionNumber).ToList());
                _candidate.Level = new SelectList(await _iRFRepository.GetLevels(), "id", "Name");
                _candidate.SelectabilityEvaluatorReportCollection = new List<SelectabilityEvaluatorReport>();
                _candidate.JoinabilityHRReportCollection = new List<JoinabilityHRReport>();
                _candidate = await _mCQService.FetchMCQReportDetails(_candidate);
                SetSelectabilityorJoniblity(_candidate);
                if (string.IsNullOrEmpty(_candidate.VideoLink))
                {
                    if (!string.IsNullOrEmpty(_candidate.roomId))
                    {
                        if (_candidate.botactive)
                        {
                            var _meeting = new RestClient("https://bot.risebird.io/api/v1/bot/invite/" + _candidate.roomId);
                            var request = new RestRequest("https://bot.risebird.io/api/v1/bot/invite/" + _candidate.roomId, Method.Get);
                            RestResponse response = _meeting.Execute(request);
                            var content = response.Content;
                            if (!string.IsNullOrEmpty(content))
                            {
                                content = content.Replace("\n", "").Replace("\t", "").Replace("\r", ""); //.Replace("\"", "")
                            }
                            try
                            {
                                var convertedResponse = JsonConvert.DeserializeObject<MCQBotReport>(content).responseBody;
                                _candidate.VideoLink = convertedResponse.recording;
                                if (_candidate != null && _candidate.VideoLink != null)
                                {
                                    await _mCQService.UpdateVideoURLForCandidate(_candidate);
                                }
                                //save aadhaar face details
                                if (!string.IsNullOrWhiteSpace(convertedResponse.aadharVerification.data.name))
                                {
                                    var values = convertedResponse.aadharVerification.data;
                                    var _face = convertedResponse.faceVerification.data;
                                    var aadhaarKYCmodel = new AadhaarData();
                                    aadhaarKYCmodel.Name = values.name;
                                    aadhaarKYCmodel.Address = values.address;
                                    aadhaarKYCmodel.gender = values.gender;
                                    aadhaarKYCmodel.dob = values.dob;
                                    aadhaarKYCmodel.doc_face = values.faceBase64;
                                    aadhaarKYCmodel.email = _candidate.CandidateEmail;
                                    aadhaarKYCmodel.reference_id = _candidate.SameID;
                                    aadhaarKYCmodel.verified_using = "BOT";
                                    aadhaarKYCmodel.matchPercentage = _face.score;
                                    aadhaarKYCmodel.passport_image = _face.liveImage;
                                    await _aadhaarService.SaveAadhaarDetails(aadhaarKYCmodel);
                                }
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            }
                        }
                        else
                        {
                            var _meeting = new RestClient("https://meet.risebird.io/api/v1/room/" + _candidate.roomId);
                            var request = new RestRequest("https://meet.risebird.io/api/v1/room/" + _candidate.roomId, Method.Get);
                            //request.AddHeader("authorization", "v7sy3J8cAFdEJp5S");
                            request.AddHeader("authorization", _configuration["RBMeetingDetails:apiKey"]);
                            RestResponse response = _meeting.Execute(request);
                            var content = response.Content;
                            if (!string.IsNullOrEmpty(content))
                            {
                                content = content.Replace("\n", "").Replace("\t", "").Replace("\r", ""); //.Replace("\"", "")
                            }
                            try
                            {
                                var convertedResponse = JsonConvert.DeserializeObject<ShareMCQRisebirdMeetingResponse>(content);
                                _candidate.VideoLink = convertedResponse.videoUrl;
                                if (_candidate != null && _candidate.VideoLink != null)
                                {
                                    await _mCQService.UpdateVideoURLForCandidate(_candidate);
                                }
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            }
                        }
                    }
                }
                if (TempData["SuccessErrorMCQMessage"] != null)
                {
                    ViewBag.Message = TempData["SuccessErrorMCQMessage"].ToString();
                    TempData.Remove("SuccessErrorMCQMessage");
                }
                return View(_candidate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        [HttpPost]
        public async Task<IActionResult> CandidateMCQReport(CandidateDetailsForTestDuplicate _candidate)
        {
            try
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
                if (await _mCQService.SaveMCQReportDetails(_candidate))
                {
                    TempData["SuccessErrorMCQMessage"] = "Report has been saved Successfully!";
                    return RedirectToAction("CandidateMCQReport", new { MCQId = _candidate.SameID, CMCQId = _candidate.Id });
                }
                else
                {
                    TempData["SuccessErrorMCQMessage"] = "Fail to save the report!";
                    return RedirectToAction("CandidateMCQReport", new { MCQId = _candidate.SameID, CMCQId = _candidate.Id });
                }
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        public void SetSelectabilityorJoniblity(CandidateDetailsForTestDuplicate _candidate)
        {
            var _selectabilitycount = _candidate.SelectabilityEvaluatorReportCollection.Count();
            var _joinabilitycount = _candidate.JoinabilityHRReportCollection.Count();
            for (int i = 0; i < (5 - _selectabilitycount); i++)
            {
                var _selectability = new SelectabilityEvaluatorReport();
                _selectability.RequiredLevelId = 3;
                _candidate.SelectabilityEvaluatorReportCollection.Add(_selectability);
            }
            for (int i = 0; i < (5 - _joinabilitycount); i++)
            {
                var _joinability = new JoinabilityHRReport();
                _joinability.RequiredLevelId = 3;
                _candidate.JoinabilityHRReportCollection.Add(_joinability);
            }
            _candidate.JoinabilityTypeList = new SelectList(_irfService.GetJoinabilityMatrix(), "id", "Name");
            _candidate.SelectabilityTypeList = new SelectList(_irfService.GetSelectabilityMatrix(), "id", "Name");
        }
        #region MCQ All Candidate Actions
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CandidateListDetails()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var isclient = false;
                var clientname = string.Empty;
                var candidateset = new List<CandidateDetailsForTestDuplicate>();
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin")
                {
                    candidateset = (List<CandidateDetailsForTestDuplicate>)await _mCQService.GetMCQCandidateDetails();
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
                        if (HttpContext.Session.GetString("MCQAllCandidateList") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("MCQAllCandidateList"));
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
        public IActionResult CandidateListDetails(InterviewFilter filter)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin")
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("MCQAllCandidateList", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("CandidateListDetails");
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
        public IActionResult ResetMCQCandidateListFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("MCQAllCandidateList");
                    return RedirectToAction("CandidateListDetails");
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
        #endregion
        #region Interviewer MCQ
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> InterviewerMCQ()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "SupportAdmin")
                {
                    string clientname = string.Empty;
                    var result = await _mCQService.FetchAllMCQSets(clientname, profileName, true);
                    var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    var seniorityLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    var employmentTypes = from EmploymentTypeOptions s in Enum.GetValues(typeof(EmploymentTypeOptions))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    var skillset = await _skillService.GetSkills();

                    var resultset = new MCQDropdowndetails();

                    resultset._ProfileTypeSkills.ProfileTypeList = new SelectList(Heuristics, "ID", "Name");
                    resultset._ProfileTypeSkills.SeniorityLevelList = new SelectList(seniorityLevels, "ID", "Name");
                    resultset._ProfileTypeSkills.EmploymentTypeOptions = new SelectList(employmentTypes, "ID", "Name");

                    resultset._ProfileTypeSkills.PrimarySkillsList = new SelectList(skillset, "id", "Name");

                    resultset.ClientList = new SelectList(_clientService.GetAllClients().Result, "Name", "Name");

                    foreach (var item in result ?? new List<MCQVMset>())
                    {
                        if (!string.IsNullOrEmpty(item.ProfileType))
                        {
                            var listProfileTypes = new List<string>();
                            foreach (var profile in item.ProfileType.Split(','))
                            {
                                var name = Enum.GetName(typeof(Heuristicsscorelevel), Convert.ToInt16(profile));
                                listProfileTypes.Add(name);
                            }
                            item.ProfileTypeName = string.Join(",", listProfileTypes);
                        }
                    }
                    if (HttpContext.Session.GetString("InterviewerMCQSetList") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<MCQFilter>(HttpContext.Session.GetString("InterviewerMCQSetList"));
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
                            if (!string.IsNullOrEmpty(filter.CountryType))
                            {
                                IEnumerable<MCQVMset> resultFilteredByCountryType = null;

                                resultFilteredByCountryType = result.Where(c => c.CountryType.HasValue && c.CountryType.Value == Convert.ToInt16(filter.CountryType));
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
                    }
                    resultset._MCQVMset = result != null ? (List<MCQVMset>)result : new List<MCQVMset>();
                    ViewBag.TemplateUrl = _configuration["MCQTemplateUrl"].ToString();
                    ViewBag.profile = profileName;
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
        public async Task<IActionResult> InterviewerMCQ(MCQFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    if (profileName == "Admin" || profileName == "HR" || profileName == "SupportAdmin")
                    {
                        if (filter != null)
                        {
                            HttpContext.Session.SetString("InterviewerMCQSetList", JsonConvert.SerializeObject(filter));
                        }
                        return RedirectToAction("InterviewerMCQ");
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
        public async Task<IActionResult> ResetInterviewerMCQFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("InterviewerMCQSetList");
                    return RedirectToAction("InterviewerMCQ");
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
        public async Task<IActionResult> InterviewerTestDetails(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var isclient = false;
                var clientname = string.Empty;
                var candidateset = new List<CandidateDetailsForTestDuplicate>();
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin")
                {
                    candidateset = (List<CandidateDetailsForTestDuplicate>)await _mCQService.GetMCQCandidateDetailsByMCQId(id, true);
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
                        if (HttpContext.Session.GetString("MCQInterviewerList") != null)
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
        public IActionResult InterviewerTestDetails(InterviewFilter filter, string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client" || profileName == "SupportAdmin")
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("MCQInterviewerList", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("InterviewerTestDetails", new { id = id });
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

        public IActionResult ResetInterviewerMCQCandidateFilter(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("MCQInterviewerList");
                    return RedirectToAction("InterviewerTestDetails", new { id = id });
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
        #endregion
        #region BotLinks
        public async Task<MCQBotResponse> GenerateBotLink(string emailds, string AadhaarVerificationRequired, string MCQTestlink, string _interviewerflag)
        {
            var model = new MCQBotRequest();
            model.tenantUuid = "e5f37005-42a3-4510-b211-0a3c0ee675b7";
            model.email = emailds;
            model.aadharVerification = AadhaarVerificationRequired;
            model.faceVerification = "Y";
            model.interviewerTraining = _interviewerflag;
            model.companyEmailVerification = "N";
            model.videoBio = "N";
            model.careerQuestions = "N";
            model.testLink.mcq = MCQTestlink;
            model.testLink.code = "";
            var client = new RestClient("https://bot.risebird.io/api/v1/bot/invite");
            var request = new RestRequest("https://bot.risebird.io/api/v1/bot/invite", Method.Post);
            request.AddHeader("content-type", "application/json");
            request.AddJsonBody(model);
            RestResponse response = await client.ExecuteAsync(request);
            var convertedResponse = JsonConvert.DeserializeObject<MCQBotResponse>(response.Content);
            return convertedResponse;
        }
        #endregion
        #region MCQ For FreemiumClient
        //Client Premium With Limit Share or attempt
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> RecruiterInterview()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "FreemiumClient" || profileName == "SupportAdmin")
                {
                    string clientname = string.Empty;
                    if (profileName == "FreemiumClient")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var client = await _userRepository.GetClientPremiumUserDetails(emailid);
                        if (client != null)
                        {
                            clientname = client.ClientName.ToUpper();
                        }
                    }
                    var result = await _mCQService.FetchAllMCQSetsPremium(clientname, profileName, false);
                    if (profileName == "Client")
                    {
                        List<MCQVMset> filteredByClient = new List<MCQVMset>();
                        foreach (var item in result)
                        {
                            if (!string.IsNullOrEmpty(item.Clients))
                            {
                                var clientsMapped = item.Clients.Split(",");
                                if (clientsMapped != null && clientsMapped.Any(x => !string.IsNullOrEmpty(x) && x.ToLower() == clientname?.ToLower()))
                                {
                                    filteredByClient.Add(item);
                                }
                            }
                        }
                        result = filteredByClient;
                    }
                    var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    var seniorityLevels = from JobLevels s in Enum.GetValues(typeof(JobLevels))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    var employmentTypes = from EmploymentTypeOptions s in Enum.GetValues(typeof(EmploymentTypeOptions))
                                          select new { ID = (int)s, Name = s.GetDescription() };
                    var skillset = await _skillService.GetSkills();
                    var resultset = new MCQDropdowndetails();
                    resultset._ProfileTypeSkills.ProfileTypeList = new SelectList(Heuristics, "ID", "Name");
                    resultset._ProfileTypeSkills.SeniorityLevelList = new SelectList(seniorityLevels, "ID", "Name");
                    resultset._ProfileTypeSkills.EmploymentTypeOptions = new SelectList(employmentTypes, "ID", "Name");
                    resultset._ProfileTypeSkills.PrimarySkillsList = new SelectList(skillset, "id", "Name");
                    resultset.ClientList = new SelectList(_clientService.GetAllClients().Result, "Name", "Name");
                    foreach (var item in result ?? new List<MCQVMset>())
                    {
                        if (!string.IsNullOrEmpty(item.ProfileType))
                        {
                            var listProfileTypes = new List<string>();
                            foreach (var profile in item.ProfileType.Split(','))
                            {
                                var name = Enum.GetName(typeof(Heuristicsscorelevel), Convert.ToInt16(profile));
                                listProfileTypes.Add(name);
                            }
                            item.ProfileTypeName = string.Join(",", listProfileTypes);
                        }
                    }
                    if (HttpContext.Session.GetString("MCQPremiumSetList") != null)
                    {
                        var filter = JsonConvert.DeserializeObject<MCQFilter>(HttpContext.Session.GetString("MCQPremiumSetList"));
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
                            if (!string.IsNullOrEmpty(filter.CountryType))
                            {
                                IEnumerable<MCQVMset> resultFilteredByCountryType = null;
                                resultFilteredByCountryType = result.Where(c => c.CountryType.HasValue && c.CountryType.Value == Convert.ToInt16(filter.CountryType));
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
                    }
                    resultset._MCQVMset = result.Count() != 0 ? (List<MCQVMset>)result : new List<MCQVMset>();
                    ViewBag.TemplateUrl = _configuration["MCQTemplateUrl"].ToString();
                    ViewBag.profile = profileName;
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
        public async Task<IActionResult> RecruiterInterview(MCQFilter filter)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("MCQPremiumSetList", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("RecruiterInterview");
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
        public async Task<IActionResult> ResetRecruiterInterviewFilter()
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("MCQPremiumSetList");
                    return RedirectToAction("RecruiterInterview");
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
        [HttpPost]
        public async Task<JsonResult> SharePerimumTestLink(string Emailids, string Id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                    profileName = profileName.Replace("\"", "");
                    var emailid = HttpContext.Session.GetString("email")?.Trim();
                    emailid = emailid.Replace("\"", "");
                    string ClientEmail = emailid;
                    string ClientName = "";
                    var totalinviteallowed = await _mCQService.FetchTotalInviteAllowed(emailid);
                    var totalinvitesent = await _mCQService.FetchTotalInviteSent(emailid);
                    if (Emailids.Split(";").Count() + totalinvitesent <= totalinviteallowed)
                    {
                        if (profileName == "FreemiumClient")
                        {
                            var client = _userRepository.GetClientPremiumUserDetails(emailid);
                            if (client != null)
                            {
                                ClientName = client.Result.ClientName.ToUpper();
                            }
                        }
                        var resultset = await _mCQService.FetchMCQSetById(Id);
                        var details = resultset.Where(x => x.PartitionKey == Id).FirstOrDefault();
                        var timmer = details.Timer + " min";
                        string currenturl = "https://" + Request.Host.ToString() + "/MCQ/SkillTestPremium?Id=" + Id;
                        var botlinkslist = new MCQBotResponse();
                        var aadharverificationflag = "N";
                        if (details.AadhaarVerification)
                        {
                            aadharverificationflag = "Y";
                        }
                        botlinkslist = await GenerateBotLink(Emailids.Replace(";", ","), aadharverificationflag, currenturl, "N");
                        MCQVMShare _list = new MCQVMShare();
                        _list.status = 0;
                        _list.MCQSet = details.Title;
                        _list.ClientHR = ClientEmail;
                        _list.ApplyingCompany = ClientName;
                        _list.WhoShared = emailid;

                        foreach (var item in Emailids.Split(";"))
                        {
                            try
                            {
                                var result = string.Empty;
                                var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                                var _meetinglinks = new ShareMCQRisebirdMeetingResponse();
                                var _testurl = botlinkslist.emailBotUrls.Where(x => x.email == item).FirstOrDefault().botTestLink;
                                _meetinglinks.roomId = _testurl.Split("/").LastOrDefault();
                                Mail _mail = new Mail
                                {
                                    TO = item,
                                    Subject = "Invitation for " + details.Title + " : " + ClientName
                                };
                                //Check Mail to send
                                result = await _notificationHelper.SharedBotTestLink(_mail, details.Title, timmer, _testurl, ClientName);
                                if (result.Contains("Mail sent successfully"))
                                {
                                    _list.PartitionKey = date1;
                                    _list.MCQId = Id;
                                    _list.EmailAddress = item.Trim();
                                    _list.roomId = _meetinglinks.roomId;
                                    _list.Expiretime = 48;
                                    await _mCQService.AddMCQSetShareListPremium(_list);
                                }
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            }

                        }
                        return Json("success");
                    }
                    else
                    {
                        return Json("You can not send more than 10 invite. Already send " + totalinvitesent + "/" + totalinviteallowed);
                    }
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

        [HttpGet]
        public async Task<IActionResult> SkillTestPremium(string Id)
        {
            var result = await _mCQService.FetchMCQSetById(Id);
            ViewBag.Questionsetid = Id;
            var model = new CandidateDetailsForTest();
            model.MCQId = Id;
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> CandidateQuestionpagePremium(CandidateDetailsForTest values)
        {
            string Id = values.MCQId;
            if (!ModelState.IsValid)
            {
                ViewBag.Questionsetid = Id;
                return View("SkillTestPremium", values);
            }
            var candidateShare = await _mCQService.GetShareMCQEmailListByMCQIdPremium(Id);
            if (candidateShare.Where(x => x.EmailAddress.ToUpper() == values.CandidateEmail.ToUpper() && x.MCQId == Id).Count() == 0)
            {
                return RedirectToAction("TestCompletePremium", new { status = "Unauthorized" });
            }

            var itemshareupdate = candidateShare.FirstOrDefault();
            await _mCQService.UpdateShareMCQEmailStatusByidPremium(itemshareupdate.id, 1);

            values.ApplyingCompany = itemshareupdate.ApplyingCompany;
            values.ClientHR = itemshareupdate.ClientHR;
            var candidateDetails = await _mCQService.GetMCQCandidateListByMCQIdPremium(Id);
            if (candidateDetails.Where(x => x.MCQId == Id && x.CandidateEmail.ToUpper() == values.CandidateEmail.ToUpper()).Count() > 0)
            {
                return RedirectToAction("TestCompletePremium", new { status = "TestCompleted" });
            }
            //Check Time Validation
            var _candidate = candidateShare.Where(x => x.EmailAddress.ToUpper() == values.CandidateEmail.ToUpper() && x.MCQId == Id).FirstOrDefault();
            if (_candidate.Expiretime > 0)
            {
                if (_candidate.ShareDate.AddHours(_candidate.Expiretime) <= DateTime.Now)
                {
                    return RedirectToAction("TestCompletePremium", new { status = "TimeExpired" });
                }
            }
            HttpContext.Session.SetString("TestCandidateDetailsPremium", JsonConvert.SerializeObject(values));
            var questions = await _mCQService.GetMCQquestionsByMCQId(Id);
            var questionstimmer = await _mCQService.FetchMCQSetById(Id);
            var model = questions.OrderBy(x => x.QuestionNumber).ToList();
            foreach (var item in model)
            {
                item.QuestionId = item.PartitionKey;
            }
            QuestionDetailsList modelset = new QuestionDetailsList();
            modelset._timmerdetails = questionstimmer.Where(x => x.PartitionKey == Id).FirstOrDefault();
            modelset._timmerdetails.Timer = (Convert.ToInt32(modelset._timmerdetails.Timer) * 60).ToString();
            if (HttpContext.Session.GetString("QuestionSetPremium") != null)
            {
                modelset._timmerdetails.Timer = "";
            }
            else
            {
                HttpContext.Session.SetString("QuestionSetPremium", JsonConvert.SerializeObject(model));
            }
            ViewBag.VideoRecording = itemshareupdate.Videorecording;
            return View(modelset);
        }

        [HttpPost]
        public JsonResult CandidateSelectQuestionsDPremium(string id)
        {
            var resultset = JsonConvert.DeserializeObject<List<MCQVM>>(HttpContext.Session.GetString("QuestionSetPremium"));
            var values = JsonConvert.DeserializeObject<CandidateDetailsForTest>(HttpContext.Session.GetString("TestCandidateDetailsPremium"));
            HttpContext.Session.SetString("TestCandidateDetailsPremium", JsonConvert.SerializeObject(values));

            HttpContext.Session.SetString("QuestionSetPremium", JsonConvert.SerializeObject(resultset));

            var questionvalue = Convert.ToInt32(id);
            if (HttpContext.Session.GetString("TestCandidateQuestionPremium") != null && questionvalue == 1)
            {
                questionvalue = JsonConvert.DeserializeObject<int>(HttpContext.Session.GetString("TestCandidateQuestionPremium"));
            }
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
                                     QuestionAnswered = string.IsNullOrEmpty(_Q.QuestionAnswer) ? false : true
                                 }).ToList();
            modeldetailset._question = result;
            modeldetailset._questionlist = _questionlist;
            return Json(modeldetailset);
        }

        [HttpPost]
        public JsonResult CandidateQuestionsDPremium()
        {
            var Option1 = Convert.ToBoolean(Request.Form["Option1"].ToString());
            var Option2 = Convert.ToBoolean(Request.Form["Option2"].ToString());
            var Option3 = Convert.ToBoolean(Request.Form["Option3"].ToString());
            var Option4 = Convert.ToBoolean(Request.Form["Option4"].ToString());
            var SingleAnswer = Request.Form["SingleAnswer"].ToString();
            var QuestionNumber = Convert.ToInt32(Request.Form["QuestionNumber"].ToString());
            var final = Request.Form["final"].ToString();
            var QuestionType = Request.Form["QuestionType"].ToString();

            HttpContext.Session.SetString("TestCandidateQuestionPremium", JsonConvert.SerializeObject(QuestionNumber));
            var resultset = JsonConvert.DeserializeObject<List<MCQVM>>(HttpContext.Session.GetString("QuestionSetPremium"));
            var values = JsonConvert.DeserializeObject<CandidateDetailsForTest>(HttpContext.Session.GetString("TestCandidateDetailsPremium"));
            HttpContext.Session.SetString("TestCandidateDetailsPremium", JsonConvert.SerializeObject(values));
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
                        if (string.IsNullOrEmpty(item.QuestionAnswer))
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
                        if (string.IsNullOrEmpty(item.QuestionAnswer))
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
                        if (string.IsNullOrEmpty(item.QuestionAnswer))
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
                HttpContext.Session.SetString("TestCandidateDetailsPremium", JsonConvert.SerializeObject(values));
                HttpContext.Session.SetString("QuestionSetPremium", JsonConvert.SerializeObject(resultset));
                return Json("");
            }
            HttpContext.Session.SetString("QuestionSetPremium", JsonConvert.SerializeObject(resultset));
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
                                     QuestionAnswered = string.IsNullOrEmpty(_Q.QuestionAnswer) ? false : true
                                 }).ToList();
            modeldetailset._question = result;
            modeldetailset._questionlist = _questionlist;
            return Json(modeldetailset);
        }

        [HttpGet]
        public IActionResult TestCompletePremium(string status)
        {
            ViewBag.Action = status;
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> SubmitTestPremium()
        {
            try
            {
                var rating = Convert.ToInt32(Request.Form["rating"].ToString());
                var comments = Request.Form["comments"].ToString();

                var resultset = JsonConvert.DeserializeObject<List<MCQVM>>(HttpContext.Session.GetString("QuestionSetPremium"));
                var values = JsonConvert.DeserializeObject<CandidateDetailsForTest>(HttpContext.Session.GetString("TestCandidateDetailsPremium"));
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
                await _mCQService.AddMCQCandidatesPremium(_list);
                foreach (var item in resultset)
                {
                    var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    item.PartitionKey = date1;
                    item.CandidateID = _id;
                }
                await _mCQService.AddMCQCandidateQuestionsPremium(resultset);
                //Send mail to support team
                var HrEmailaddresslist = await _supportService.GetAllSupportHR();
                string HrEmailaddress = string.Empty;
                foreach (var item in HrEmailaddresslist)
                {
                    if (string.IsNullOrEmpty(HrEmailaddress))
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
                var resultretrun = await _notificationHelper.CandidateCompleteTestLink(_mail, values.CandidateEmail, values.ApplyingCompany, candidatescore);
                HttpContext.Session.Remove("TestCandidateQuestionPremium");
                return Json("TestSubmit");
            }
            catch (Exception)
            {
                return Json("Failed to submit Test");
            }
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CandidateTestDetailsPremium(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var isclient = false;
                var clientname = string.Empty;
                var candidateset = new List<CandidateDetailsForTestDuplicate>();
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "FreemiumClient" || profileName == "SupportAdmin")
                {
                    candidateset = (List<CandidateDetailsForTestDuplicate>)await _mCQService.GetMCQCandidateDetailsByMCQIdPremium(id);
                    if (candidateset != null)
                    {
                        if (profileName == "FreemiumClient")
                        {
                            var emailid = HttpContext.Session.GetString("email")?.Trim();
                            emailid = emailid.Replace("\"", "");
                            var client = _userRepository.GetClientPremiumUserDetails(emailid);
                            if (client != null)
                            {
                                clientname = client.Result.ClientName.ToUpper();
                                isclient = true;
                                var clientemailid = emailid.ToUpper();
                                candidateset = candidateset.Where(x => x.ClientHR.ToUpper() == clientemailid).ToList();
                            }
                        }
                        if (HttpContext.Session.GetString("MCQCandidateListPremium") != null)
                        {
                            var filter = JsonConvert.DeserializeObject<InterviewFilter>(HttpContext.Session.GetString("MCQCandidateListPremium"));
                            if (filter.StartDate != null && filter.EndDate != null)
                            {
                                candidateset = candidateset.Where(t => t.Sharetime >= filter.StartDate && t.Sharetime <= filter.EndDate).ToList();
                            }
                            if (profileName != "FreemiumClient")
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
        public IActionResult CandidateTestDetailsPremium(InterviewFilter filter, string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "FreemiumClient" || profileName == "SupportAdmin")
                {
                    if (filter != null)
                    {
                        HttpContext.Session.SetString("MCQCandidateListPremium", JsonConvert.SerializeObject(filter));
                    }
                    return RedirectToAction("CandidateTestDetailsPremium", new { id = id });
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
        public IActionResult ResetMCQCandidatePremiumFilter(string id)
        {
            try
            {
                if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
                {
                    HttpContext.Session.Remove("MCQCandidateListPremium");
                    return RedirectToAction("CandidateTestDetailsPremium", new { id = id });
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
        public async Task<IActionResult> CandidateMCQReportPremium(string MCQId, string CMCQId, string SMCQId)
        {
            try
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
                var _candidate = new CandidateDetailsForTestDuplicate();
                var candidateset = await _mCQService.GetMCQReportByMCQIdPremium(MCQId);
                if (!string.IsNullOrWhiteSpace(CMCQId))
                {
                    _candidate = candidateset.Where(x => x.Id == CMCQId).FirstOrDefault();
                }
                else
                {
                    _candidate = candidateset.Where(x => x.SMCQId == SMCQId).FirstOrDefault();
                }
                if (_candidate == null)
                {
                    _candidate = new CandidateDetailsForTestDuplicate();
                }
                var questions = await _mCQService.GetMCQCandidateQuestionsPremium(_candidate.Id);
                if (questions == null)
                {
                    questions = new List<MCQVM>();
                }
                _candidate.QuestionDetails = (List<MCQVM>)(questions.OrderBy(x => x.QuestionNumber).ToList());
                _candidate.Level = new SelectList(await _iRFRepository.GetLevels(), "id", "Name");
                _candidate.SelectabilityEvaluatorReportCollection = new List<SelectabilityEvaluatorReport>();
                _candidate.JoinabilityHRReportCollection = new List<JoinabilityHRReport>();
                if (string.IsNullOrEmpty(_candidate.VideoLink))
                {
                    if (!string.IsNullOrEmpty(_candidate.roomId))
                    {
                        if (_candidate.botactive)
                        {
                            var _meeting = new RestClient("https://bot.risebird.io/api/v1/bot/invite/" + _candidate.roomId);
                            var request = new RestRequest("https://bot.risebird.io/api/v1/bot/invite/" + _candidate.roomId, Method.Get);
                            RestResponse response = _meeting.Execute(request);
                            var content = response.Content;
                            if (!string.IsNullOrEmpty(content))
                            {
                                content = content.Replace("\n", "").Replace("\t", "").Replace("\r", ""); //.Replace("\"", "")
                            }
                            try
                            {
                                var convertedResponse = JsonConvert.DeserializeObject<MCQBotReport>(content).responseBody;
                                _candidate.VideoLink = convertedResponse.recording;
                                if (_candidate != null && _candidate.VideoLink != null)
                                {
                                    await _mCQService.UpdateVideoURLForCandidatePremium(_candidate);
                                }
                                //save aadhaar face details
                                if (!string.IsNullOrWhiteSpace(convertedResponse.aadharVerification.data.name))
                                {
                                    var values = convertedResponse.aadharVerification.data;
                                    var _face = convertedResponse.faceVerification.data;
                                    var aadhaarKYCmodel = new AadhaarData();
                                    aadhaarKYCmodel.Name = values.name;
                                    aadhaarKYCmodel.Address = values.address;
                                    aadhaarKYCmodel.gender = values.gender;
                                    aadhaarKYCmodel.dob = values.dob;
                                    aadhaarKYCmodel.doc_face = values.faceBase64;
                                    aadhaarKYCmodel.email = _candidate.CandidateEmail;
                                    aadhaarKYCmodel.reference_id = _candidate.SameID;
                                    aadhaarKYCmodel.verified_using = "BOT";
                                    aadhaarKYCmodel.matchPercentage = _face.score;
                                    aadhaarKYCmodel.passport_image = _face.liveImage;
                                    await _aadhaarService.SaveAadhaarDetails(aadhaarKYCmodel);
                                }
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            }
                        }
                        else
                        {
                            var _meeting = new RestClient("https://meet.risebird.io/api/v1/room/" + _candidate.roomId);
                            var request = new RestRequest("https://meet.risebird.io/api/v1/room/" + _candidate.roomId, Method.Get);
                            //request.AddHeader("authorization", "v7sy3J8cAFdEJp5S");
                            request.AddHeader("authorization", _configuration["RBMeetingDetails:apiKey"]);
                            RestResponse response = _meeting.Execute(request);
                            var content = response.Content;
                            if (!string.IsNullOrEmpty(content))
                            {
                                content = content.Replace("\n", "").Replace("\t", "").Replace("\r", ""); //.Replace("\"", "")
                            }
                            try
                            {
                                var convertedResponse = JsonConvert.DeserializeObject<ShareMCQRisebirdMeetingResponse>(content);
                                _candidate.VideoLink = convertedResponse.videoUrl;
                                if (_candidate != null && _candidate.VideoLink != null)
                                {
                                    await _mCQService.UpdateVideoURLForCandidatePremium(_candidate);
                                }
                            }
                            catch (Exception ex)
                            {
                                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                            }
                        }
                    }
                }
                return View(_candidate);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return RedirectToAction("SignOut", "Session");
            }
        }
        #endregion
    }
}
