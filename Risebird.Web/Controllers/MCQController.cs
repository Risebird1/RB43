using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects.DataClasses;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Risebird.CommonHelper;
using Risebird.Entities.ViewModel;
using Twilio.Rest.Trunking.V1;

namespace Risebird.Web.Controllers
{
    public class MCQController : Controller
    {
        private readonly IConfiguration _configuration;

        #region MCQDetails
        public MCQController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            //add validation 
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var result = _azureTableStorageService.GetAllEntities<MCQVMset>("RisebirdMCQSet");
            var questions = _azureTableStorageService.GetAllEntities<MCQVM>("RisebirdMCQquestions");
            foreach (var item in result)
            {
                item.QuestionCount = questions.Where(x => x.Id == item.PartitionKey).Count();
            }
            return View(result);
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
            int number = 1;
            MCQVMset _lstset = new MCQVMset();
            var date = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
            _lstset.PartitionKey = date;
            _lstset.RowKey = Guid.NewGuid().ToString();
            _lstset.Title = value;
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
                await _azureTableStorageService.InsertEntity("RisebirdMCQSet", _lstset);
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
            //add validation 
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var questions = _azureTableStorageService.GetAllEntities<MCQVM>("RisebirdMCQquestions");
            var model = questions.Where(x => x.Id == Id).OrderBy(x => x.QuestionNumber).ToList();
            return View(model);
        }
        #endregion

        #region CandidateSkillTest
        public IActionResult SkillTest(string Id)
        {
            ViewBag.Questionsetid = Id;
            return View();
        }
        [HttpPost]
        public IActionResult CandidateQuestionpage(CandidateDetailsForTest values)
        {
            string Id = values.Id;
            if (!ModelState.IsValid)
            {
                return View("SkillTest", values);
            }
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var candidateDetails = _azureTableStorageService.GetAllEntities<CandidateDetailsForTest>("RisebirdMCQCandidate");
            if (candidateDetails.Where(x => x.Id == Id && x.CandidateEmail.ToUpper() == values.CandidateEmail.ToUpper()).Count() > 0)
            {
                return RedirectToAction("TestComplete", new { status = "TestCompleted" });
            }
            HttpContext.Session.SetString("TestCandidateDetails", JsonConvert.SerializeObject(values));
            var questions = _azureTableStorageService.GetAllEntities<MCQVM>("RisebirdMCQquestions");
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
                    await _azureTableStorageService.InsertEntity("RisebirdMCQCandidateQuestions", item);
                }
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
            return View("CandidateQuestionpage", modeldetailset);
        }
        public IActionResult TestComplete(string status)
        {
            ViewBag.Action = status;
            return View();
        }
        #endregion

        public IActionResult CandidateTestDetails(string id)
        {
            AzureStorageHelper _azureTableStorageService = new AzureStorageHelper(_configuration);
            var candidateDetails = _azureTableStorageService.GetAllEntities<CandidateDetailsForTest>("RisebirdMCQCandidate");
            return View();
        }
    }
}
