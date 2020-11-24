using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.Business.Interface;
using Risebird.CommonHelper;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;

namespace Risebird.Web.Controllers
{
    public class MCQController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly INotificationHelper _notificationHelper;
        private ISupportService _supportService;
        private ISkillService _skillService;

        #region MCQDetails
        public MCQController(IConfiguration configuration, IUserRepository userRepository, INotificationHelper notificationHelper, ISupportService supportService, ISkillService skillService)
        {
            _configuration = configuration;
            _userRepository = userRepository;
            _notificationHelper = notificationHelper;
            _supportService = supportService;
            _skillService = skillService;
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                    var result = _azureTableStorageService.GetAllEntities<MCQVMset>("RisebirdMCQSet");
                    var questions = _azureTableStorageService.GetAllEntities<MCQVM>("RisebirdMCQquestions");
                    var Heuristics = from Heuristicsscorelevel s in Enum.GetValues(typeof(Heuristicsscorelevel))
                                     select new { ID = (int)s, Name = s.GetDescription() };
                    var skillset = _skillService.GetSkills();
                    var resultset = new MCQDropdowndetails();
                    resultset._ProfileTypeSkills.ProfileTypeList = new SelectList(Heuristics, "ID", "Name");
                    resultset._ProfileTypeSkills.PrimarySkillsList = new SelectList(skillset, "id", "Name");
                    foreach (var item in result)
                    {
                        item.QuestionCount = questions.Where(x => x.Id == item.PartitionKey).Count();
                        item.ProfileTypeName = Enum.GetName(typeof(Heuristicsscorelevel), item.ProfileType);
                    }
                    resultset._MCQVMset = (List<MCQVMset>)result;
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
        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }
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
            var ProfileType = Convert.ToInt32(Request.Form["ProfileType"].ToString());
            int number = 1;
            MCQVMset _lstset = new MCQVMset();
            var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
            _lstset.PartitionKey = date;
            _lstset.RowKey = Guid.NewGuid().ToString();
            _lstset.Title = value;
            _lstset.Timer = time;
            _lstset.ProfileType = ProfileType;
            if (status == "Edit")
            {
                _lstset.PartitionKey = partialid;
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
                                    && reader.GetValue(5).ToString() == "Option3" && reader.GetValue(6).ToString() == "Option4" && reader.GetValue(7).ToString() == "Answer")
                                {

                                }
                                else
                                {
                                    return Json("File format is invilid, please download templete");
                                }
                            }
                            else
                            {
                                if (reader.GetValue(1).ToString() == "" || reader.GetValue(2).ToString() == "" || reader.GetValue(3).ToString() == "" || reader.GetValue(4).ToString() == ""
                                   || reader.GetValue(5).ToString() == "" || reader.GetValue(6).ToString() == "" || reader.GetValue(7).ToString() == "")
                                {
                                    return Json("Question Fileds should not be blank.");
                                }
                                else
                                {
                                    var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                                    _lstMCQ.Add(new MCQVM
                                    {
                                        PartitionKey = date1,
                                        RowKey = Guid.NewGuid().ToString(),
                                        QuestionType = reader.GetValue(1).ToString(),
                                        Question = reader.GetValue(2).ToString(),
                                        Option1 = reader.GetValue(3).ToString(),
                                        Option2 = reader.GetValue(4).ToString(),
                                        Option3 = reader.GetValue(5).ToString(),
                                        Option4 = reader.GetValue(6).ToString(),
                                        Answer = reader.GetValue(7).ToString(),
                                        Id = _lstset.PartitionKey,
                                        QuestionNumber = number
                                    });
                                    number++;
                                }
                            }
                            i++;
                        }
                    }
                }
                System.IO.File.Delete(path);

                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                if (status == "Edit")
                {
                    var result = _azureTableStorageService.GetAllEntities<MCQVMset>("RisebirdMCQSet");
                    var list = result.Where(x => x.PartitionKey == _lstset.PartitionKey).FirstOrDefault();
                    list.Title = _lstset.Title;
                    list.Timer = _lstset.Timer;
                    list.ProfileType = _lstset.ProfileType;
                    await _azureTableStorageService.InsertEntity("RisebirdMCQSet", list);
                    var questions = _azureTableStorageService.GetAllEntities<MCQVM>("RisebirdMCQquestions");
                    questions = questions.Where(x => x.Id == partialid).ToList();
                    foreach (var item in questions)
                    {
                        await _azureTableStorageService.DeleteEntity("RisebirdMCQquestions", item);
                    }
                }
                else
                {
                    await _azureTableStorageService.InsertEntity("RisebirdMCQSet", _lstset);
                }
                foreach (var item in _lstMCQ)
                {
                    try
                    {
                        await _azureTableStorageService.InsertEntity("RisebirdMCQquestions", item);
                    }
                    catch (Exception ex)
                    {

                    }
                }

            }
            return Json("Successfully Uploaded");
        }
        public IActionResult Questionpage(string Id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                    var questions = _azureTableStorageService.GetAllEntities<MCQVM>("RisebirdMCQquestions");
                    var model = questions.Where(x => x.Id == Id).OrderBy(x => x.QuestionNumber).ToList();
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
        #endregion

        #region CandidateSkillTest
        public IActionResult SkillTest(string Id)
        {
            ViewBag.Questionsetid = Id;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CandidateQuestionpage(CandidateDetailsForTest values)
        {
            string Id = values.Id;
            if (!ModelState.IsValid)
            {
                ViewBag.Questionsetid = Id;
                return View("SkillTest", values);
            }
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var candidateShare = _azureTableStorageService.GetAllEntities<MCQVMShare>("RisebirdMCQShareList");
            if (candidateShare.Where(x => x.EmailAddress.ToUpper() == values.CandidateEmail.ToUpper() && x.Id == Id).Count() == 0)
            {
                return RedirectToAction("TestComplete", new { status = "Unauthorized" });
            }
            var itemshareupdate = candidateShare.FirstOrDefault();
            itemshareupdate.status = "1";
            await _azureTableStorageService.InsertEntity("RisebirdMCQShareList", itemshareupdate);

            var candidateDetails = _azureTableStorageService.GetAllEntities<CandidateDetailsForTest>("RisebirdMCQCandidate");
            if (candidateDetails.Where(x => x.Id == Id && x.CandidateEmail.ToUpper() == values.CandidateEmail.ToUpper()).Count() > 0)
            {
                return RedirectToAction("TestComplete", new { status = "TestCompleted" });
            }
            HttpContext.Session.SetString("TestCandidateDetails", JsonConvert.SerializeObject(values));
            var questions = _azureTableStorageService.GetAllEntities<MCQVM>("RisebirdMCQquestions");
            var questionstimmer = _azureTableStorageService.GetAllEntities<MCQVMset>("RisebirdMCQSet");
            var model = questions.Where(x => x.Id == Id).OrderBy(x => x.QuestionNumber).ToList();
            foreach (var item in model)
            {
                item.QuestionId = item.PartitionKey;
            }
            HttpContext.Session.SetString("QuestionSet", JsonConvert.SerializeObject(model));
            var result = model.Where(x => x.QuestionNumber == 1).FirstOrDefault();
            if (model.Count() == 1)
            {
                result.final = "true";
            }
            QuestionDetailsList modelset = new QuestionDetailsList();
            var _questionlist = (from _Q in model
                                 select new QuestionList
                                 {
                                     QuestionNumber = _Q.QuestionNumber,
                                     QuestionAnswered = String.IsNullOrEmpty(_Q.QuestionAnswer) ? false : true
                                 }).ToList();
            modelset._question = result;
            modelset._questionlist = _questionlist;
            modelset._timmerdetails = questionstimmer.Where(x => x.PartitionKey == Id).FirstOrDefault();
            modelset._timmerdetails.Timer = (Convert.ToInt32(modelset._timmerdetails.Timer) * 60).ToString();
            return View(modelset);
        }
        [HttpPost]
        public async Task<IActionResult> CandidateQuestions(QuestionDetailsList modelset)
        {
            var model = modelset._question;
            var resultset = JsonConvert.DeserializeObject<List<MCQVM>>(HttpContext.Session.GetString("QuestionSet"));
            var values = JsonConvert.DeserializeObject<CandidateDetailsForTest>(HttpContext.Session.GetString("TestCandidateDetails"));
            HttpContext.Session.SetString("TestCandidateDetails", JsonConvert.SerializeObject(values));
            foreach (var item in resultset.Where(x => x.QuestionNumber == model.QuestionNumber))
            {
                item.MultiAnswerOption1 = model.MultiAnswerOption1;
                item.MultiAnswerOption2 = model.MultiAnswerOption2;
                item.MultiAnswerOption3 = model.MultiAnswerOption3;
                item.MultiAnswerOption4 = model.MultiAnswerOption4;
                item.SingleAnswer = model.SingleAnswer;
                if (model.QuestionType == "Single")
                {
                    item.QuestionAnswer = model.SingleAnswer;
                    if (model.SingleAnswer.ToUpper() == item.Answer.ToUpper())
                    {
                        item.Rating = 1;
                    }
                }
                else
                {
                    var flag = true;
                    if (model.MultiAnswerOption1 == true)
                    {
                        item.QuestionAnswer = "1";
                        if (!item.Answer.ToUpper().Contains("1"))
                        {
                            flag = false;
                        }
                    }
                    if (model.MultiAnswerOption2 == true)
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
                    if (model.MultiAnswerOption3 == true)
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
                    if (model.MultiAnswerOption4 == true)
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
                    if (model.MultiAnswerOption1 == false && model.MultiAnswerOption2 == false && model.MultiAnswerOption3 == false && model.MultiAnswerOption4 == false)
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
            if (model.final == "true")
            {
                values = JsonConvert.DeserializeObject<CandidateDetailsForTest>(HttpContext.Session.GetString("TestCandidateDetails"));
                var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                var _list = new CandidateDetailsForTest();
                _list.PartitionKey = date;
                _list.RowKey = Guid.NewGuid().ToString();
                _list.Id = values.Id;
                _list.Skills = values.Skills;
                _list.TotalExprience = values.TotalExprience;
                _list.CandidateEmail = values.CandidateEmail;
                _list.ApplyingCompany = values.ApplyingCompany;
                _list.ClientHR = values.ClientHR;
                _list.CandidateScore = resultset.Sum(x => x.Rating);
                _list.TotalQuestions = resultset.Count();
                AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                await _azureTableStorageService.InsertEntity("RisebirdMCQCandidate", _list);
                foreach (var item in resultset)
                {
                    var date1 = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    item.PartitionKey = date1;
                    item.RowKey = Guid.NewGuid().ToString();
                    item.CandidateID = date;
                    await _azureTableStorageService.InsertEntity("RisebirdMCQCandidateQuestions", item);
                }
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
                Mail _mail = new Mail
                {
                    TO = HrEmailaddress,
                    Subject = "Candidate " + values.CandidateEmail + " Complete Test for " + values.ApplyingCompany
                };
                var resultretrun = _notificationHelper.CandidateCompleteTestLink(_mail, values.CandidateEmail, values.ApplyingCompany);
                return RedirectToAction("TestComplete", new { status = "TestSubmit" });
            }
            HttpContext.Session.SetString("QuestionSet", JsonConvert.SerializeObject(resultset));
            var questionvalue = model.QuestionNumber + 1;
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
            modeldetailset._timmerdetails = new MCQVMset();
            modeldetailset._timmerdetails.Timer = modelset._timmerdetails.Timer;
            return View("CandidateQuestionpage", modeldetailset);
        }
        public IActionResult TestComplete(string status)
        {
            ViewBag.Action = status;
            return View();
        }
        #endregion

        #region MCQ Actions
        public IActionResult CandidateTestDetails(string id)
        {
            if (HttpContext.Session.GetString("email") != null && HttpContext.Session.GetString("profileName") != null)
            {
                var isclient = false;
                var clientname = string.Empty;
                var profileName = HttpContext.Session.GetString("profileName")?.Trim();
                profileName = profileName.Replace("\"", "");
                if (profileName == "Admin" || profileName == "HR" || profileName == "Client")
                {
                    AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
                    var candidateDetails = _azureTableStorageService.GetAllEntities<CandidateDetailsForTest>("RisebirdMCQCandidate");
                    if (profileName == "Client")
                    {
                        var emailid = HttpContext.Session.GetString("email")?.Trim();
                        emailid = emailid.Replace("\"", "");
                        var client = _userRepository.GetClientUserDetails(emailid);
                        clientname = client.Result.ClientName.ToUpper();
                        isclient = true;
                        candidateDetails = candidateDetails.Where(x => x.ApplyingCompany.ToUpper() == clientname);
                    }

                    ViewBag.Isclient = isclient;
                    return View(candidateDetails);
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
        [HttpPost]
        public async Task<JsonResult> DeleteCandidate(string id)
        {
            //validation
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var candidateDetails = _azureTableStorageService.GetAllEntities<CandidateDetailsForTest>("RisebirdMCQCandidate");
            var model = candidateDetails.Where(x => x.PartitionKey == id).FirstOrDefault();
            await _azureTableStorageService.DeleteEntity("RisebirdMCQCandidate", model);
            var questions = _azureTableStorageService.GetAllEntities<MCQVM>("RisebirdMCQCandidateQuestions");
            questions = questions.Where(x => x.CandidateID == id).ToList();
            foreach (var item in questions)
            {
                await _azureTableStorageService.DeleteEntity("RisebirdMCQCandidateQuestions", item);
            }
            return Json("success");
        }
        [HttpPost]
        public async Task<JsonResult> ShareTestLink(string Emailids, string Id, string ClientName)
        {
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var resultset = _azureTableStorageService.GetAllEntities<MCQVMset>("RisebirdMCQSet");
            var details = resultset.Where(x => x.PartitionKey == Id).FirstOrDefault();
            var timmer = details.Timer + " min";
            string currenturl = "https://" + Request.Host.ToString() + "/MCQ/SkillTest/" + Id;
            Mail _mail = new Mail
            {
                TO = Emailids,
                Subject = "Invitation for " + details.Title + " : " + ClientName
            };
            var result = _notificationHelper.SharedTestLink(_mail, details.Title, timmer, currenturl, ClientName);
            if (result.Contains("Mail sent successfully"))
            {
                MCQVMShare _list = new MCQVMShare();
                _list.status = "0";
                _list.MCQSet = details.Title;
                foreach (var item in Emailids.Split(";"))
                {
                    var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
                    _list.PartitionKey = date;
                    _list.RowKey = Guid.NewGuid().ToString();
                    _list.Id = Id;
                    _list.EmailAddress = item;
                    await _azureTableStorageService.InsertEntity("RisebirdMCQShareList", _list);
                }
                return Json("success");
            }
            else
            {
                return Json("Fail");
            }
        }
        #endregion
    }
}
