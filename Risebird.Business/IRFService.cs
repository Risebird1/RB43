using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using Risebird.Repository.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class IRFService :IIRFService
    {
        private IIRFRepository _irfRepository;
        private readonly INotificationHelper _notificationHelper;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly ITwilioHelper _twilioHelper;
        private readonly IClientService _clientService;
        private readonly IConfiguration _configuration;


        public IRFService(IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger, ITwilioHelper twilioHelper, INotificationHelper notificationHelper, IClientService clientService, IConfiguration configuration)
        {
            _irfRepository = irfRepository;
            _applicationErrorLogger = applicationErrorLogger;
            _twilioHelper = twilioHelper;
            _notificationHelper = notificationHelper;
            _clientService = clientService;
            _configuration = configuration;
        }

        #region IRF Functions
        public async Task<bool> AddIRFDetails(IRFVm irfDetails)
        {
            try
            {
                irfDetails.CreatedOn = DateTime.UtcNow;
                return  _irfRepository.AddIRFDetails(irfDetails).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public IEnumerable<Users> GetRiseBirdHR()
        {
            try
            {

                //var call = _twilioHelper.MakePhoneCall("+919284487005");

                var hrList = new List<Users>();
                var users = _irfRepository.GetUsers();
                foreach (var item in users)
                {
                    if(item.idProfile == 2)
                    {
                        hrList.Add(item);
                    }
                }
                return hrList;
                
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public SelectList GetQualityController()
        {
            try
            {
                return new SelectList(_irfRepository.GetQualityController(), "id", "EmailId");
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public string GetInterviewerSkills(int id)
        {
            try
            {
                var interviewers = _irfRepository.GetInterviewerSkills();
                string Name = String.Empty;
                foreach (var item in interviewers)
                {
                    if (item.UserId == id)
                    {
                        Name = item.PrimarySkill;
                        break;
                    }

                }
                return Name;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<Interviewer> GetInterviewers(string skills)
        {
            try
            {
                var interviewers = _irfRepository.GetInterviewerSkills().Where(x=> x.IsApproved==true);
                var skillList = skills.Split(",");
                //skillList.RemoveAt()
                var list = new List<Interviewer>();
                foreach (var item in interviewers)
                {
                    foreach (var skill in skillList)
                    {
                        if (skill == "")
                            continue;
                        var skillName = _irfRepository.GetSkills().Where(x=> x.id == Convert.ToInt16(skill)).FirstOrDefault().Name;
                        if(item.PrimarySkill.Contains(skillName))
                        {
                            list.Add(item);
                            break;
                        }
                    }
                }
                list.Distinct();
                
                return list;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public string GetQCSkills(int id)
        {
            try
            {
                return _irfRepository.GetQCSkills(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public string GetClientUserRole(int id)
        {
            try
            {
                return _irfRepository.GetClientUserRole(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public string GetQuestionSkills(string id)
        {
            try
            {
                return _irfRepository.GetQuestionSkills(id);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public IEnumerable<Skill> GetTechSkills(int id)
        {
            try
            {
                List<Skill> list = new List<Skill>();
                var skills = _irfRepository.GetSkillsbyIRFid(id);
                var result = _irfRepository.GetSkills();
                foreach (var item in skills)
                {

                   var obj= result.Where(x => x.id == item.SkillId).FirstOrDefault();
                    list.Add(obj);
                }
                
                return list;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool AddSkillComments(TechSkillComments skillComments)
        {
            try
            {
                return _irfRepository.AddSkillComments(skillComments);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<bool> DeleteIRF(int id)
        {
            try
            {
                return _irfRepository.DeleteIRF(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> UnArchiveIRF(int id)
        {
            try
            {
                return _irfRepository.UnArchiveIRF(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public async Task<bool> DeleteCandidate(int id)
        {
            try
            {
                return _irfRepository.DeleteCandidate(id).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


        public List<MeetingVM> GetIRFInterviewMeetings(int id,string profileName, string email)
        {
            try
            {
                int role =0;
                var result= _irfRepository.GetIRFInterviewMeetings(id).OrderBy(x => x.interviewDate).ToList();
                
                if(email != string.Empty && profileName == "Client")
                {
                    var clients = _irfRepository.GetTransClientUser(id);
                    foreach (var item in clients)
                    {
                        if (item.EmailId == email)
                        {
                            role = item.ClientRoleId;
                            break;
                        }
                    }
                    if(role ==0)
                    {
                        return new List<MeetingVM>();
                    }
                    if (role == 1)
                    {
                        result = result.Where(m => m.ClientHrContact == email).ToList();
                        return result;
                    }
                    else if (role == 4)
                    {
                        result = result.Where(m => m.staffingAgency == email).ToList();
                        return result;
                    }
                    else
                        return result;
                }
                else
                {
                    return result;
                }
                
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IRFVm> GetEditIRF(int id)
        {
            try
            {
                var model = new IRFVm();

                //get all values from database
                var IRF = _irfRepository.EditIRF(id);
                var transTechSkills = _irfRepository.GetSkillsbyIRFid(id);
                var transSkillsGeneral = _irfRepository.GetSkillsGeneralbyIRFid(id);
                var transJoinability = _irfRepository.GetTransJoinability(id);
                var transClientUser = _irfRepository.GetTransClientUser(id);
                var transInterviewer = _irfRepository.GetTransInterviewer(id);
                var transQualityController = _irfRepository.GetTransQualityController(id);
                var transSelectability = _irfRepository.GetTransSelectability(id);
                var transEmergencyInterviewer = _irfRepository.GetTransEmergencyInterviewer(id);
                var transResumeEvaluator = _irfRepository.GetTransResumeEvaluator(id);
                var transHR = _irfRepository.GetTransHR(id);


                //assign values retrieved to the model
                model.id = IRF.id;
                model.ClientCompName = IRF.ClientCompName;
                model.IRFId = IRF.IRFId;
                model.InterviewerCollection = (IList<TransInterviewer>)(transInterviewer);
                model.EmergencyInterviewerCollection = (IList<TransEmergencyInterviewer>)(transEmergencyInterviewer);
                model.JDLink = IRF.JDLink;
                model.JDTiltle = IRF.JDTiltle;
                model.JobCountry = IRF.JobCountry;
                model.Joinability = (IList<TransJoinability>)(transJoinability);
                model.NoOfIntterview = IRF.NoOfIntterview;
                model.NoOfPosition = IRF.NoOfPosition;
                model.PerMintuteRate = IRF.PerMintuteRate;
                model.QualityController = (IList<TransQualityController>)(transQualityController);
                model.RisebirdHr = IRF.RisebirdHr;
                model.SkillCollection = (IList<TransSkill>)(transTechSkills);
                model.SkillGeneral = (IList<TransSkillGeneral>)(transSkillsGeneral);
                model.Selectability = (IList<TransSelectability>)(transSelectability);
                model.StartDate = IRF.StartDate;
                model.ClientCollection= (IList<TransClientUser>)(transClientUser);
                model.ResumeEvaluatorCollection = (IList<TransResumeEvaluator>)(transResumeEvaluator);
                model.HRCollection = (IList<TransHR>)(transHR);
                model.ClientId = IRF.ClientId;
                model.ClientName = IRF.ClientName;
                model.CompanyType = IRF.CompanyType;
                model.EndDate = IRF.EndDate;
                model.JobLevel = IRF.JobLevel;
                model.CalendlyLink = IRF.CalendlyLink;
                model.TotalInvites = IRF.TotalInvites;
                model.TotalBudget = IRF.TotalBudget;

                var skillCount = transTechSkills.Count();
                return model;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateIRF(IRFVm model)
        {
            try
            {
                return _irfRepository.UpdateIRFDetails(model).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        #endregion
        public IList<SkillComment> GetTechSkillComments(int id)
        {
            try
            {
                var result = (IList<SkillComment>)_irfRepository.GetTechSkillComments(id);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public List<string> GetListOfInterviwerBasedOnIRFId(string irfId)
        {
            try
            {
                return _irfRepository.GetListOfInterviwerBasedOnIRFId(irfId).Result.ToList();
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool CheckUniqueness(string value)
        {
            try
            {
                return _irfRepository.CheckUniqueness(value);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public IEnumerable<JoinabilityMatrix> GetJoinabilityMatrix()
        {
            try
            {
                return _irfRepository.GetJoinabilityMatrix();

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<GeneralSkillMatrix> GetGeneralSkillMatrix()
        {
            try
            {
                return _irfRepository.GetGeneralSkillMatrix();

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<SelectabilityMatrix> GetSelectabilityMatrix()
        {
            try
            {
                return _irfRepository.GetSelectabilityMatrix();

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public IEnumerable<Users> GetClientUser(int id)
        {
            try
            {

                return _irfRepository.GetClientUser(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
            
        }
        public InterviewPlanner GetInterviewPlanner(int id)
        {
            try
            {
                return _irfRepository.GetInterviewPlanner(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public InvoiceModel GetInvoiceModel(int irfId, DateTime startDate, DateTime endDate, string srNumber, string poNumber, string reqNumber, string currencyCode, string projectCode, bool isGST,string discount, string clientAddress)
        {
            try
            {
                var irfDetails = _irfRepository.GetIRFbyId(irfId);
                var currentMonth = DateTime.Now.Month;
                var interviewDetails = _irfRepository.GetIRFInterviewMeetings(irfId).Where(x => x.MeetingStatus.HasValue && (x.MeetingStatus ==4 || x.MeetingStatus == 2) && x.interviewDate.Date<=endDate.Date && x.interviewDate.Date>= startDate.Date).ToList();// && x.interviewDate.Month ==currentMonth).ToList();
                var invoiceModel = new InvoiceModel();
                if(irfDetails!=null && invoiceModel!=null)
                {
                    //var address = irfDetails.ClientAddress;
                    //var address = "Bagmane World Technology Center, Marathalli Ring Road, Doddanakhundi Village,Bangalore 560048";
                    var strb = new StringBuilder();
                    var addressArray = clientAddress.Split(",");
                    for (int i=1; i <= addressArray.Length; i++)
                    {
                        if(i!= addressArray.Length)
                        {
                            strb.Append(addressArray[i-1] + "," + "<br/>");
                        }
                        else
                        {
                            strb.Append(addressArray[i - 1]);
                        }
                    }
                    //foreach (var item in address.Split(","))
                    //{
                    //    strb.Append(item + "," + "<br/>");
                    //}
                    invoiceModel.CurrentMonth = DateTime.Now.ToString("MMMM yyyy");
                    Random generator = new Random();
                    invoiceModel.SRNumber = srNumber;
                    invoiceModel.PONumber = poNumber;
                    invoiceModel.ProjectCode = projectCode;
                    invoiceModel.RequestNumber = reqNumber;
                    invoiceModel.CurrencyCode = currencyCode;
                    invoiceModel.IsGST = isGST;
                    invoiceModel.StartDate = startDate;
                    invoiceModel.EndDate = endDate;
                    invoiceModel.InvoiceNumber= generator.Next(0, 999999).ToString("D6");
                    invoiceModel.InvoiceReferenceCode = "RB" + irfDetails.ClientCompName.Substring(0, 2).ToUpper() + irfDetails.JDTiltle.Split("|")[0].Substring(0, 2).ToUpper() + invoiceModel.CurrentMonth.ToString().Substring(0, 2).ToUpper();
                    invoiceModel.InterviewCount = interviewDetails.Count();
                    invoiceModel.ClientAddress = strb.ToString();//irfDetails.ClientAddress;
                    invoiceModel.ClientGSTIN = irfDetails.ClientGST;
                    invoiceModel.ClientName = irfDetails.ClientCompName;
                    invoiceModel.RisebirdAddress = _configuration["RisebirdDetails:Address"];
                    invoiceModel.PricePerInterview = irfDetails.PerMintuteRate;
                    invoiceModel.RisebirdAccountName = _configuration["RisebirdDetails:AccountName"];
                    invoiceModel.RisebirdAccountNumber = _configuration["RisebirdDetails:AccountNumber"];
                    invoiceModel.RisebirdBankName = _configuration["RisebirdDetails:BankName"];
                    invoiceModel.RisebirdPAN = _configuration["RisebirdDetails:PAN"];
                    invoiceModel.RisebirdTAN = _configuration["RisebirdDetails:TAN"];
                    invoiceModel.RisebirdGSTIN = _configuration["RisebirdDetails:GSTIN"];
                    invoiceModel.RisebirdCIN = _configuration["RisebirdDetails:CIN"];
                    invoiceModel.RisebirdIFSC = _configuration["RisebirdDetails:IFSC"];
                    invoiceModel.RisebirdICICIBankSwiftCode = _configuration["RisebirdDetails:ICICIBankSWIFTCode"];
                    invoiceModel.PartialInterviewCount = invoiceModel.InterviewCount;
                    invoiceModel.Discount = Convert.ToDouble(discount);//irfDetails.Discount;
                    invoiceModel.DiscountedPricePerInterview = (1 - (invoiceModel.Discount / 100)) * irfDetails.PerMintuteRate;
                    invoiceModel.PartialPayable = invoiceModel.PartialInterviewCount * invoiceModel.DiscountedPricePerInterview;
                    if(isGST)
                    {
                        invoiceModel.IGST = (0.18) * (invoiceModel.PartialPayable);
                        invoiceModel.IGSTSymbol = "18%";
                    }
                    else
                    {
                        invoiceModel.IGST = 0;
                        invoiceModel.IGSTSymbol = "NA";

                    }
                    invoiceModel.Netpayable = invoiceModel.PartialPayable + invoiceModel.IGST;
                }
                
                return invoiceModel;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public IEnumerable<Candidate> GetIRFCandidates(int id)
        {
            try
            {
                return _irfRepository.GetIRFCandidates(id);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

        public IEnumerable<Candidate> GetCandidateIRFInvites(string emailId)
        {
            try
            {
                return _irfRepository.GetCandidateIRFInvites(emailId);//.Where(x=> x.EndDate.Date <= DateTime.Now.Date);
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public async Task<string> AddCandidate(Candidate obj, string emailId, string profile)
        {
            try
            {
                //check if the candidate email exists for the IRFId
                var duplicate = _irfRepository.GetIRFCandidates(obj.IRFId).Where(x=> x.EmailId==obj.EmailId);
                if(duplicate != null && duplicate.Count()>0)
                {
                    return "Sorry! Candidate already exists for the IRF.";
                }
                var userId = _irfRepository.GetUserId(emailId, profile);
                obj.AddedByUserId = userId;
                var result = _irfRepository.AddCandidate(obj).Result;
                if (result.Contains("successfully"))
                {
                    //get all resume evaluators and hrs to send mails to
                    var resumeEvaluators =_irfRepository.GetResumeEvaluatorsMailIds(obj.IRFId);
                    _notificationHelper.SendCandidateOnboardedMail(resumeEvaluators.ToList(), obj.Name, obj.IRFName, "Evaluator");

                    var hrs = _irfRepository.GetHRsMailIds(obj.IRFId);
                    //send mails
                    _notificationHelper.SendCandidateOnboardedMail(hrs.ToList(), obj.Name, obj.IRFName, "HR");
                }
                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not add candidate.";
            }
        }
        public async Task<string> AddMultipleCandidates(List<Candidate> candidateList, string emailId, string profile)
        {
            try
            {
                var candidates = _irfRepository.GetIRFCandidates(candidateList.FirstOrDefault().IRFId);
                foreach (var candidate in candidateList)
                {
                    //check if the candidate email exists for the IRFId
                    var duplicate = candidates.Where(x => x.EmailId == candidate.EmailId);
                    if (duplicate != null && duplicate.Count() > 0)
                    {
                        return "Sorry! One or more candidates already exist for the IRF.";
                    }
                }
                var userId = _irfRepository.GetUserId(emailId, profile);
                return _irfRepository.AddMultipleCandidates(candidateList, userId).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Sorry! Could not import candidates.";
            }
        }
        public Candidate GetCandidate(int id, int irfId)
        {
            try
            {
                var candidate = _irfRepository.GetIRFCandidates(irfId).Where(x => x.id == id).FirstOrDefault();                
                return candidate;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public bool UpdateCandidate(Candidate obj)
        {
            try
            {
                return _irfRepository.UpdateCandidate(obj);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public bool SendInvite(int irfId, string email)
        {
            try
            {
                //update irf Invite count here
                var irf = _irfRepository.GetIRFbyId(irfId);
                var inviteCount = irf.TotalInvites;
                var calendlyLink = irf.CalendlyLink;
                var candidate = _irfRepository.GetIRFCandidates(irfId).Where(x => x.EmailId == email).FirstOrDefault();
                //var candidateInviteCount = candidate.TotalInvites;
                if (email != null && calendlyLink!=null)
                {
                    var jdLink = irf.JDLink;
                    var ClientName = _clientService.GetClient(irf.ClientId)?.Name;
                    var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x=>x.ClientRoleId==1 || x.ClientRoleId == 4);
                    _notificationHelper.SendCandidateInvite(ClientName, jdLink, irfClientUsers.ToList(),irf.JDTiltle.Split("|")[0],calendlyLink,email);
                    if(candidate.TotalInvites==0)
                    {
                        _irfRepository.UpdateIRFInvites(irfId, (inviteCount + 1));

                    }
                    _irfRepository.UpdateCandidateInvites(candidate.id, (candidate.TotalInvites +1));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public bool CancelInvite(int irfId, string email)
        {
            try
            {
                //update irf Invite count here
                var irf = _irfRepository.GetIRFbyId(irfId);
                //var inviteCount = irf.TotalInvites;
                //var calendlyLink = irf.CalendlyLink;
                var candidate = _irfRepository.GetIRFCandidates(irfId).Where(x => x.EmailId == email).FirstOrDefault();
                //var candidateInviteCount = candidate.TotalInvites;
                if (email != null)
                {
                    var jdLink = irf.JDLink;
                    var ClientName = _clientService.GetClient(irf.ClientId)?.Name;
                    var irfClientUsers = _irfRepository.GetTransClientUser(irfId).Where(x => x.ClientRoleId == 1 || x.ClientRoleId == 4);
                    var result=_notificationHelper.CancelCandidateInvite(ClientName,  irfClientUsers.ToList(), irf.JDTiltle.Split("|")[0],  email); 
                    if(result.ToLower().Contains("success"))
                    {
                        return true;
                    }
                    
                }
                return false;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        
        public async Task<bool> AddUpdateInterviewPlanner(InterviewPlanner model)
        {
            try
            {
                return _irfRepository.AddUpdateInterviewPlanner(model).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }
        public IRFFunnel GetFunnelData(int irfId, string irfName)
        {
            try
            {
                //total number of candidates
                int waste = 0;
                var emailList = new List<string>();
                var funnelData = new IRFFunnel();
                var candidates = _irfRepository.GetIRFCandidates(irfId);
                if(candidates.Count()==0)
                {
                    //get candidates from Meeting table by passing IRFId
                    emailList  = _irfRepository.GetIRFCandidatesEmail(irfName).ToList();
                    funnelData.TotalCandidates = emailList.Count();
                    funnelData.TotalInvites = emailList.Count(); 
                }
                else
                {
                    emailList = candidates.Select(x => x.EmailId).ToList();
                    funnelData.TotalCandidates = candidates.Count();
                    funnelData.TotalInvites = candidates.Where(x => x.TotalInvites > 0).Count();
                }
               
                //funnelData.InvitesRate = funnelData.TotalCandidates != 0 ? ((funnelData.TotalInvites * 100 / funnelData.TotalCandidates) ) : 0;

                //get interviewMeetings where candidateEmail and irfName matches
                var interviewIdList = new List<int>();
                foreach (var email in emailList)
                {
                    var interviewId = _irfRepository.GetInterviewMeetings(irfName, email);
                    if(interviewId!=0)
                    {
                        interviewIdList.Add(interviewId);
                    }
                }

                funnelData.TotalConfirmations = interviewIdList.Count();
                //funnelData.ConfirmationRate = funnelData.TotalInvites != 0 ? ((funnelData.TotalConfirmations * 100 / funnelData.TotalInvites)) : 0;

                string ids = string.Join<int>(",", interviewIdList);
                var meetingData = _irfRepository.GetInterviewComments(ids);
                if(meetingData != null)
                {
                    var completed = meetingData.Where(x => x.MeetingStatus == 4);
                    //var totalCompleted = completed.Count();
                    funnelData.TotalCompleted = completed.Count();
                    //funnelData.CompletionRate = funnelData.TotalConfirmations != 0 ? ((totalCompleted * 100 / funnelData.TotalConfirmations) ) : 0;
                    var risebirdSelected = completed.Where(x => x.InterviewStatus.HasValue && (x.InterviewStatus == 2 || x.InterviewStatus == 4));
                    //var totalRisebirdSelected = risebirdSelected.Count();
                    funnelData.TotalRisebirdSelection = risebirdSelected.Count();
                    //funnelData.RisebirdSelectionRate = totalCompleted!=0?(totalRisebirdSelected * 100 / totalCompleted) : 0;
                    var approved = risebirdSelected.Where(x => x.IsApproved.HasValue && x.IsApproved == 1);
                    //var totalApproved = approved.Count();
                    funnelData.TotalApproved = approved.Count();
                    var approvalFilter = risebirdSelected.Where(x => x.IsApproved.HasValue && x.IsApproved > 2);
                    //var approvalDenominator = totalRisebirdSelected - approvalFilter.Count();
                    //funnelData.ApprovalRate = approvalDenominator != 0 ? (totalApproved * 100 / approvalDenominator) : 0;
                    waste = waste + approvalFilter.Where(x => x.IsApproved > 3).Count();
                    var finalSelected = approved.Where(x => x.IsSelected.HasValue && x.IsSelected == 1);
                    //var totalFinalSelected = finalSelected.Count();
                    funnelData.TotalFinalSelections = finalSelected.Count();
                    //var finalSelectedDeductions = approved.Where(x => x.IsSelected.HasValue && x.IsSelected != 1 && x.IsSelected != 2 && x.IsSelected != 6).Count();
                    //var finalSelectionDenominator = totalApproved - finalSelectedDeductions;
                    //funnelData.FinalSelectionRate = finalSelectionDenominator != 0 ? ((totalFinalSelected * 100 / finalSelectionDenominator) * 100) : 0;
                    waste = waste + approved.Where(x => x.IsSelected.HasValue && x.IsSelected > 7).Count();
                    var offered = finalSelected.Where(x => x.OfferStatus.HasValue && x.OfferStatus == 1);
                    //var totalOffered = offered.Count();
                    funnelData.TotalOffered = offered.Count();
                    var offerDeductions = finalSelected.Where(x => x.OfferStatus.HasValue && x.OfferStatus > 3);
                    //var offerDenominator = totalFinalSelected - offerDeductions.Count();
                    //funnelData.OfferRate = offerDenominator != 0 ? ((totalOffered * 100 / offerDenominator) * 100) : 0;
                    waste = waste + offerDeductions.Where(x => x.OfferStatus.HasValue && x.OfferStatus != 4).Count();
                    var joined = offered.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus == 1);
                    //var totalJoined = joined.Count();
                    funnelData.TotalJoined = joined.Count();
                    var joinedDeductions = offered.Where(x => x.JoiningStatus.HasValue && x.JoiningStatus > 2);
                    //var joinedDenominator = totalOffered - joinedDeductions.Count();
                    //funnelData.JoiningRate = joinedDenominator != 0 ? ((totalJoined * 100 / joinedDenominator) * 100) : 0;
                    waste = waste + joinedDeductions.Where(x =>x.JoiningStatus.HasValue && x.JoiningStatus >= 5).Count();
                    funnelData.WasteRate = risebirdSelected.Count() != 0?(waste * 100 / risebirdSelected.Count()) : 0;
                }

                return funnelData;
            }
            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }

        public int GetUserId(string emailId, string profile)
        {
            int result = 0;
            try
            {
               result= _irfRepository.GetUserId(emailId, profile);
                return result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return result;
            }
        }

    }
}
