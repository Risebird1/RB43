using System;
using Risebird.Business.Interface;
using Risebird.CommonHelper.Interface;
using Risebird.Entities.Data;
using Risebird.Repository.Interface;
using System.Collections.Generic;
using System.Linq;
using Risebird.Entities.Enum;
using Risebird.Entities.ViewModel;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Risebird.CommonHelper;
namespace Risebird.Business
{
    public class MeetingService : IMeetingService
    {
        private IMeetingRespository _meetingRespository;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly IIRFService _irfService;
        private readonly INotificationHelper _notificationHelper;
        private IIRFRepository _irfRepository;
        private IUserService _userService;
        private IEvaluatorRepository _evaluatorRespository;
        private IVoiceItRepository _voiceItRepository;
        private ISupportRepository _supportRespository;
        public MeetingService(IMeetingRespository meetingRespository, IIRFService irfFService, IUserService userService, INotificationHelper notificationHelper, IIRFRepository irfRepository, IApplicationErrorLogger applicationErrorLogger, IEvaluatorRepository evaluatorRespository, IVoiceItRepository voiceItRepository, ISupportRepository supportRespository)
        {
            _meetingRespository = meetingRespository;
            _applicationErrorLogger = applicationErrorLogger;
            _irfService = irfFService;
            _notificationHelper = notificationHelper;
            _irfRepository = irfRepository;
            _userService = userService;
            _evaluatorRespository = evaluatorRespository;
            _voiceItRepository = voiceItRepository;
            _supportRespository = supportRespository;
        }

        public List<MeetingVM> GetMeetingList()
        {
            try
            {
                return _meetingRespository.GetAllInterviewMeetings().Result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<MeetingVM> GetAllMeetingList(DateTime startDate, DateTime endDate)
        {
            try
            {
                return _meetingRespository.GetAllInterviewMeetings(startDate, endDate).Result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<MeetingVM> GetRiseTubeMeetings(int clientId, string profile, string Emailid)
        {
            try
            {

                return _meetingRespository.GetRiseTubeMeetings(clientId, profile, Emailid).Result.OrderByDescending(x => x.interviewDate).ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// FInd duplicate interview invite based on Skill Channel
        /// </summary>
        /// <param name="IRFId"></param>
        /// <returns></returns>
        public List<MeetingVM> IRFMultipleCandidateInvites(string IRFId)
        {
            try
            {

                return _meetingRespository.IRFMultipleCandidateInvites(IRFId).Result.ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        /// <summary>
        /// Delete Interview Record
        /// </summary>
        /// <param name="imid"></param>
        /// <returns></returns>
        public async Task<bool> DeleteInterview(int imid)
        {
            try
            {
                await _meetingRespository.DeleteInterview(imid);
                return true;
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
               
            }
        }

        public List<MeetingVM> MultipleCandidateInvites()
        {
            try
            {

                return _meetingRespository.MultipleCandidateInvites().Result.ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<MeetingVM> GetClientMeetingList(string emailId)
        {
            try
            {
                var meetings = _meetingRespository.GetClientMeetings(emailId).Result.OrderByDescending(x => x.interviewDate).ToList();
               
                return meetings?? new List<MeetingVM>();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<string> GetClientHRContactList(string emailId, List<MeetingVM> meetingList)
        {
            try
            {
                //var clientHrList = _meetingRespository.GetClientHRContactList().Result.ToList();
                //Get client user details
                var clientDetails = _meetingRespository.GetClientDetails(emailId).ToList().FirstOrDefault();
                meetingList = meetingList.Where(x => x.ClientId == clientDetails.ClientId && x.ClientHrContact != string.Empty).ToList();
                var finalList = meetingList.Select(x => x.ClientHrContact).ToList();

                foreach (var item in finalList)
                {
                    if (item != null)
                        item.Replace("\n", "");
                }
                finalList = finalList.Distinct().ToList();

                return finalList;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<string> GetEndClientList(string emailId, List<MeetingVM> meetingList)
        {
            try
            {
                //var clientHrList = _meetingRespository.GetClientHRContactList().Result.ToList();
                //Get client user details
                var clientDetails = _meetingRespository.GetClientDetails(emailId).ToList().FirstOrDefault();
                meetingList = meetingList.Where(x => x.ClientId == clientDetails.ClientId /*&& x.ClientHrContact != string.Empty*/).ToList();
                var finalList = meetingList.Select(x => x.ClientName).ToList();
                foreach (var item in finalList)
                {
                    item.Replace("\n", "");
                }
                finalList = finalList.Distinct().ToList();

                return finalList;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<string> GetIRFList(string emailId, List<MeetingVM> meetingList)
        {
            try
            {
                //var clientHrList = _meetingRespository.GetClientHRContactList().Result.ToList();
                //Get client user details
                var clientDetails = _meetingRespository.GetClientDetails(emailId).ToList().FirstOrDefault();
                meetingList = meetingList.Where(x => x.ClientId == clientDetails.ClientId /*&& x.ClientHrContact != string.Empty*/).ToList();
                var finalList = meetingList.Select(x => x.IRFId).ToList();
                finalList = finalList.Distinct().ToList();
                return finalList;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        public bool AddMeeting(MeetingVM meeting)
        {
            try
            {
                if (_meetingRespository.AddMeeting(meeting).Result)
                {
                    var interviwers = _irfService.GetListOfInterviwerBasedOnIRFId(meeting.IRFId);
                    if (interviwers != null && string.IsNullOrEmpty(meeting.interviewer))
                    {
                        _notificationHelper.SendIRFBasedInterviewerMail(interviwers, meeting.interviewDate.ToString());
                        return true;
                    }
                    else if (!string.IsNullOrEmpty(meeting.interviewer))
                    {
                        _notificationHelper.SendInterviewerMailOnAssinment(meeting.interviewer, meeting.interviewDate.ToString());
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

        public MeetingVM GetMeeting(int id)
        {
            try
            {
                var meeting = _meetingRespository.GetInterviewMeetingById(id);
                if (meeting == null)
                    return null;
                else
                    return meeting.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public SelectList GetIRFList()
        {
            try
            {
                return new SelectList(_meetingRespository.GetIRFList(), "IRFId", "IRFId");
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<TransClientUser> GetClientsByRoles(string IRF)
        {
            try
            {

                return _meetingRespository.GetClientsByRoles(IRF);

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public bool PostApproval(string imid, string value, string comment)
        {
            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(imid))
            {
                var val = Convert.ToInt32(value);
                var result = _meetingRespository.PostApproval(Convert.ToInt32(imid), val, comment);
                return result;
            }
            return false;
        }

        public bool SaveClientFeedback(string imid, string isApproved, string approvalComment, string isSelected, string selectionComment, string joiningStatus, string joiningComment, string offerStatus, string offerComment)
        {
            if (!string.IsNullOrEmpty(isApproved) && !string.IsNullOrEmpty(isSelected) && !string.IsNullOrEmpty(offerStatus) && !string.IsNullOrEmpty(joiningStatus) && !string.IsNullOrEmpty(imid))
            {
                //var val = Convert.ToInt32(value);
                var result = _meetingRespository.SaveClientFeedback(Convert.ToInt32(imid), Convert.ToInt32(isApproved), approvalComment, Convert.ToInt32(isSelected), selectionComment, Convert.ToInt32(joiningStatus), joiningComment, Convert.ToInt32(offerStatus), offerComment);
                return result;
            }
            return false;
        }

        public bool SaveQCFeedback(string imid, string qcStatus, string comment, bool FraudLipSync, bool QuestionsRelevance, bool HandsOnCoding, bool BiasAndCandidateHandling, bool TechnicalCommunicationSkills, bool ChapteringCompleted, bool QuestionsFilled, bool TechSkillsMetric, bool SelectabilityMetric, bool JoinabilityMetric, bool GeneralSkillMetric, bool CommentsFilled, bool MeaningfulAssessments, bool CorrectEnglishGrammar, bool ProctoringReportFilled)
        {
            if (!string.IsNullOrEmpty(qcStatus) && !string.IsNullOrEmpty(imid))
            {
                //var val = Convert.ToInt32(value);
                var result = _meetingRespository.SaveQCFeedback(Convert.ToInt32(imid), Convert.ToInt32(qcStatus), comment, FraudLipSync, QuestionsRelevance, HandsOnCoding, BiasAndCandidateHandling, TechnicalCommunicationSkills, ChapteringCompleted, QuestionsFilled, TechSkillsMetric, SelectabilityMetric, JoinabilityMetric, GeneralSkillMetric, CommentsFilled, MeaningfulAssessments, CorrectEnglishGrammar, ProctoringReportFilled);
                return result;
            }
            return false;
        }
        public bool PostSelectionStatus(string imid, string value, string comment)
        {
            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(imid))
            {
                var val = Convert.ToInt32(value);
                var result = _meetingRespository.PostSelectionStatus(Convert.ToInt32(imid), val, comment);
                return result;
            }
            return false;
        }

        public bool UpdateMeeting(MeetingVM meetingToUpdate)
        {
            try
            {
                return _meetingRespository.UpdateMeeting(meetingToUpdate).Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public List<Meeting> GetAllOpenInterviews(string emailId)
        {
            try
            {
                var openMeeting = new List<Meeting>();
                openMeeting = _meetingRespository.GetAllOpenInterviews(emailId).Result.ToList();//.OrderByDescending(x => x.interviewDate).ToList();
                var emergencyMeetings = _meetingRespository.GetAllEmergencyOpenInterviews(emailId).Result.ToList();
                if (emergencyMeetings != null)
                {
                    foreach (var item in emergencyMeetings)
                    {
                        openMeeting.Add(item);
                    }
                }
                openMeeting = openMeeting.Distinct().ToList();
                openMeeting = openMeeting.OrderByDescending(x => x.interviewDate).ToList();
                return openMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<Meeting> GetQCOpenInterviews(string emailId)
        {
            try
            {
                var openMeeting = _meetingRespository.GetQCOpenInterviews(emailId).Result.OrderByDescending(x => x.interviewDate).ToList().Where(x => x.QCInterviewer == null).ToList();
                return openMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<Meeting> GetAllQCInterviews(string emailId)
        {
            try
            {
                var qcMeeting = _meetingRespository.GetAllQCInterviews(emailId).Result.OrderByDescending(x => x.interviewDate).ToList();
                return qcMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<MeetingVM> GetAllClientInterviews(string emailId)
        {
            try
            {
                var clientMeeting = _meetingRespository.GetAllClientInterviews(emailId).Result.OrderByDescending(x => x.dtCreatedOn).ToList();
                return clientMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<Meeting> GetAllAssignedInterviews(string emailID)
        {
            try
            {               
                var assignedMeeting = _meetingRespository.GetAllAssignedInterview(emailID).Result.OrderByDescending(x => x.interviewDate).ToList();
                
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<Meeting> GetCandidateInterviews(string emailID, string status)
        {
            try
            {
                ///*.Where(x=>/*x.interviewDate.Date>DateTime.Now.Date &&*/ x.MeetingStatus==2)*/
                var assignedMeeting = _meetingRespository.GetCandidateInterviews(emailID, status).Result.OrderByDescending(x => x.interviewDate).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        public List<Meeting> GetQualityControlInterviews(string emailID)
        {
            try
            {
                //var finalList = new List<Meeting>();
                var assignedMeeting = _meetingRespository.GetQualityControlInterview(emailID).Result.OrderByDescending(x => x.interviewDate).ToList();
                //foreach (var item in assignedMeeting)
                //{
                //    if(_meetingRespository.CheckCompletedInterviews(item.candidateEmail,item.ClientId, item.irfIntId).Result.ToList().Count()==0)
                //    {
                //        finalList.Add(item);
                //    }
                //}
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }


        /// <summary>
        /// Assign Yourself
        /// </summary>
        /// <param name="meetingid"></param>
        /// <param name="interviewerMail"></param>
        /// <param name="interviewDate"></param>
        /// <returns></returns>
        public string AssignYourself(int meetingid, string interviewerMail, DateTime interviewDate)
        {
            try
            {
                var assignedMeeting = _meetingRespository.GetAllAssignedInterview(interviewerMail).Result.OrderByDescending(x => x.interviewDate).ToList();
                var checkDateMeeting = assignedMeeting.Where(x => x.interviewDate == interviewDate);
                if (checkDateMeeting.Count() > 0)
                {
                    return "Failed! You already have an interview scheduled for this Date. Please contact Risebird HR.";
                }
                return _meetingRespository.AssignYourself(meetingid, interviewerMail);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }
            //try
            //{
            //    var newArr = InterviewDate.Split(" ")[0].Split("/");
            //    var date = newArr[1] + "-" + newArr[0] + "-" + newArr[2] +" "+ InterviewDate.Split(" ")[1];
            //    var assignedMeeting = _meetingRespository.GetAllAssignedInterview(interviewerMail).Result.OrderByDescending(x => x.interviewDate).ToList();
            //    var checkDateMeeting = assignedMeeting.Where(x => x.interviewDate == DateTime.Parse(date));
            //    if (checkDateMeeting.Count() > 0)
            //    {
            //        return "Failed! You already have an interview scheduled for this Date. Please contact Risebird HR.";
            //    }
            //    return _meetingRespository.AssignYourself(meetingid, interviewerMail);

            //}
            //catch (Exception ex)
            //{
            //    _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
            //    return "Failed to assigned interview, Please contact Risebird HR.";
            //}
        }

        public string QCAssignYourself(int meetingid, string interviewerMail)
        {
            try
            {

                return _meetingRespository.QCAssignYourself(meetingid, interviewerMail);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }

        }

        public bool UnassignYourself(int meetingid, string email, bool isPenalty)
        {
            try
            {
                var meeting = _meetingRespository.GetInterviewMeetingById(meetingid);

                if (meeting != null && meeting.Result != null)
                {
                    _meetingRespository.UnassignYourself(meetingid);
                    //TimeSpan timeSpan = meeting.Result.interviewDate - DateTime.Now;
                    var interviwers = new List<string>();
                    if (isPenalty)
                    {
                        //update flag for Emergency Interview
                        _irfRepository.UpdateEmergencyFlag(meetingid, email, meeting.Result.interviewDate);
                        interviwers = _irfRepository.GetEmergencyInterviwerBasedOnIRFId(meeting.Result.irfIntId).Result.ToList();
                    }
                    else
                    {
                        interviwers = _irfService.GetListOfInterviwerBasedOnIRFId(meeting.Result.IRFId);
                    }

                    if (interviwers != null)
                    {
                        interviwers.Remove(email);
                        _notificationHelper.SendIRFBasedInterviewerMail(interviwers, meeting.Result.interviewDate.ToString());

                    }
                    var hrs = _userService.GetAllHrsEmailld()?.ToList();
                    if (hrs != null)
                    {
                        var candidateEmail = meeting.Result.candidateEmail;
                        _notificationHelper.SendAlertHRForUnassignedInterview(hrs, meeting.Result.interviewDate.ToString(), candidateEmail, email);

                    }
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

        public bool QCUnassignYourself(int meetingid, string email)
        {
            try
            {
                return _meetingRespository.QCUnassignYourself(meetingid).Result;

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public bool CancelInterview(int meetingid, string interviewDate, string candidateEmail)
        {
            try
            {
                if (_meetingRespository.CancelInterview(meetingid))
                {
                    //send mail to the HRs
                    var interviewers = _meetingRespository.ListInterviewersMail(candidateEmail, interviewDate);

                    _notificationHelper.SendInterviewCancelledMail(interviewers, interviewDate);
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



        public InterviewComments GetInterviewComments(int interviewId)
        {

            try
            {
                var comments = _meetingRespository.GetInterviewComments(interviewId);

                if (comments == null)
                    return null;
                else
                    return comments.Result;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public IEnumerable<SkillComment> GetTechSkillComments(int IRFid)
        {

            try
            {
                var comments = _meetingRespository.GetTechSkillComments(IRFid);

                if (comments == null)
                    return null;
                else
                    return comments;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public bool UpdateComments(InterviewComments comments)
        {
            try
            {
                return _meetingRespository.UpdateComments(comments).Result;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<InterviewReport> GetInteviewReport(int interviewId, int irfId)
        {
            try
            {
                var reportModel = new InterviewReport();
                var irfDetails = _meetingRespository.GetIRFDetails(irfId);
                var levels = _meetingRespository.GetLevels();
                //check whether an IRF exists for the given IRFId
                var check = _meetingRespository.GetIRFbyId(irfId);
                var interviewDetails = _meetingRespository.GetInterviewMeetingById(interviewId).Result;

                if (!check)
                {
                    return null;
                }
                // check whether a feedback for the interviewId has already been submitted
                var interviewReport = _meetingRespository.GetInterviewReportById(interviewId, irfId);
                if (interviewReport != null && interviewReport.Count() > 0 /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    //call Edit Feedbackform method
                    reportModel = interviewReport.FirstOrDefault();
                    EditInterviewReport(reportModel);
                }
                else
                {
                    var model = interviewReport.Count() > 0 ? interviewReport.FirstOrDefault() : new InterviewReport();
                    model.IsEdited = model.InterviewId > 0;
                    //get all values from database
                    var levelTech = _meetingRespository.GetLevelTech();
                    var skills = _meetingRespository.GetSkills();
                    //var questionDetails = _meetingRespository.GetTechSkillComments(irfId);
                    var techSkillsDetails = _meetingRespository.GetSkillsbyIRFid(irfId);
                    var skillGeneralDetails = _meetingRespository.GetSkillsGeneralbyIRFid(irfId);
                    //var joinabilityDetails = _meetingRespository.GetTransJoinability(irfId);
                    //var selectabilityDetails = _meetingRespository.GetTransSelectability(irfId);
                    var techSkillList = new List<TechSkillReport>();


                    //assign values to model
                    foreach (var item in techSkillsDetails)
                    {
                        TechSkillReport obj = new TechSkillReport();
                        obj.InterviewId = interviewId;
                        obj.RequiredLevel = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.SkillName = skills.Where(x => x.id == item.SkillId).FirstOrDefault().Name;
                        obj.SkillId = skills.Where(x => x.id == item.SkillId).FirstOrDefault().id;
                        obj.WeightageRequired = item.Weightage;

                        techSkillList.Add(obj);
                    }
                    model.TechSkillReportCollection = (IList<TechSkillReport>)techSkillList;

                    var genSkillList = new List<SkillGeneralReport>();

                    foreach (var item in skillGeneralDetails)
                    {
                        SkillGeneralReport obj = new SkillGeneralReport();
                        obj.InterviewId = interviewId;
                        obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.Type = item.Type;
                        obj.Name = item.Name;
                        obj.Value = item.Value;
                        obj.WeightageRequired = item.Weightage;
                        genSkillList.Add(obj);
                    }

                    model.SkillGeneralReportCollection = (IList<SkillGeneralReport>)genSkillList;

                    //var joinabilityList = new List<JoinabilityReport>();


                    //foreach (var item in joinabilityDetails)
                    //{
                    //    JoinabilityReport obj = new JoinabilityReport();
                    //    obj.InterviewId = interviewId;
                    //    obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                    //    obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                    //    obj.Name = item.Name;
                    //    obj.Type = item.Type;
                    //    obj.Value = item.Value;
                    //    obj.WeightageRequired = item.Weightage;
                    //    joinabilityList.Add(obj);
                    //}
                    //model.JoinabilityReportCollection = (IList<JoinabilityReport>)joinabilityList;

                    //var selectabilityList = new List<SelectabilityReport>();
                    //foreach (var item in selectabilityDetails)
                    //{
                    //    SelectabilityReport obj = new SelectabilityReport();
                    //    obj.InterviewId = interviewId;
                    //    obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                    //    obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                    //    obj.Type = item.Type;
                    //    obj.Name = item.Name;
                    //    obj.Value = item.Value;
                    //    obj.WeightageRequired = item.Weightage;
                    //    selectabilityList.Add(obj);
                    //}

                    //model.SelectabilityReportCollection = (IList<SelectabilityReport>)selectabilityList;

                    model.InterviewId = interviewId;
                    model.CandidateName = interviewDetails.candidateName;
                    model.interviewDate = interviewDetails.interviewDate.ToString();
                    model.resumeLink = interviewDetails.resumeLink;
                    model.IRFId = irfId;
                   
                    model.JobLevelName = Enum.GetName(typeof(JobLevels), interviewDetails.JobLevel);
                    model.currentEmployerName = interviewDetails.currentEmployerName;
                    model.expectedPerHourRate = interviewDetails.expectedPerHourRate;
                    model.PreferredLocation = interviewDetails.preferredLocation;
                    model.IsEdited = false;
                    model.IsApproved = interviewDetails.IsApproved;
                    model.IsSelected = interviewDetails.IsSelected;
                    model.FinalComments = interviewDetails.FinalComments;
                    model.ApprovalComments = interviewDetails.ApprovalComments;
                    model.IsBiometricEnabled = interviewDetails.IsBiometricEnabled;
                    model.JoiningBiometryScore = interviewDetails.JoiningBiometryScore;
                    model.InterviewBiometryScore = interviewDetails.InterviewBiometryScore;
                    reportModel = model;
                }
                // joinability and Selectability

                //get candidateId
                reportModel.CandidateId = _meetingRespository.GetCandidateId(interviewId, irfDetails.IRFId);
                reportModel.CandidateName = interviewDetails.candidateName;
                reportModel.SelectabilityComments = _evaluatorRespository.GetSelectabilityComments(irfId, interviewDetails.candidateEmail);
                reportModel.CandidateEmail = interviewDetails.candidateEmail;
                reportModel.CandidateVisa = interviewDetails.CandidateVisa;
                reportModel.CandidateMobileNumber = interviewDetails.MobileNumber;
                var selectabilityReport = _evaluatorRespository.GetSelectabilityReport(reportModel.CandidateId, irfId);
                if (selectabilityReport != null && selectabilityReport.Count() > 0 /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    reportModel.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityReport;

                }
                else
                {
                    var selectabilityDetails = _evaluatorRespository.GetTransSelectability(irfId);
                    var selectabilityList = new List<SelectabilityEvaluatorReport>();
                    foreach (var item in selectabilityDetails)
                    {
                        SelectabilityEvaluatorReport obj = new SelectabilityEvaluatorReport();
                        obj.CandidateId = reportModel.CandidateId;
                        obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.Type = item.Type;
                        obj.Name = item.Name;
                        obj.Value = item.Value;
                        obj.WeightageRequired = item.Weightage;
                        selectabilityList.Add(obj);
                    }

                    reportModel.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityList;
                }

                reportModel.JoinabilityComments = _evaluatorRespository.GetJoinabilityComments(irfId, interviewDetails.candidateEmail);
                var joinabilityReport = _evaluatorRespository.GetJoinabilityReport(reportModel.CandidateId, irfId);
                if (joinabilityReport != null && joinabilityReport.Count() > 0 /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    reportModel.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityReport;

                }
                else
                {
                    var joinabilityDetails = _meetingRespository.GetTransJoinability(irfId);
                    var joinabilityList = new List<JoinabilityHRReport>();
                    foreach (var item in joinabilityDetails)
                    {
                        JoinabilityHRReport obj = new JoinabilityHRReport();
                        obj.CandidateId = reportModel.CandidateId;
                        obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.Type = item.Type;
                        obj.Name = item.Name;
                        obj.Value = item.Value;
                        obj.WeightageRequired = item.Weightage;
                        joinabilityList.Add(obj);
                    }

                    reportModel.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityList;
                }

                reportModel.IRFName = irfDetails.IRFId;
                reportModel.PrimarySkill = irfDetails.JDTiltle.Split("|")[0];
                return reportModel;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        private InterviewReport EditInterviewReport(InterviewReport model)
        {
            var techReportCollection = (IList<TechSkillReport>)(_meetingRespository.GetTechSkillReport(model.InterviewId));
            //model.TechSkillReportCollection = (IList<TechSkillReport>)(_meetingRespository.GetTechSkillReport(model.InterviewId));

            if (techReportCollection.Count() == 0)
            {
                var techSkillsDetails = _meetingRespository.GetSkillsbyIRFid(model.IRFId);
                var techSkillList = new List<TechSkillReport>();
                var levelTech = _meetingRespository.GetLevelTech();
                var skills = _meetingRespository.GetSkills();

                //assign values to model
                foreach (var item in techSkillsDetails)
                {
                    TechSkillReport obj = new TechSkillReport();
                    obj.InterviewId = model.InterviewId;
                    obj.RequiredLevel = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                    obj.RequiredLevelId = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                    obj.SkillName = skills.Where(x => x.id == item.SkillId).FirstOrDefault().Name;
                    obj.SkillId = skills.Where(x => x.id == item.SkillId).FirstOrDefault().id;
                    obj.WeightageRequired = item.Weightage;

                    techSkillList.Add(obj);
                }
                model.TechSkillReportCollection = (IList<TechSkillReport>)techSkillList;
            }
            else
            {
                model.TechSkillReportCollection = techReportCollection;
            }

            //model.JoinabilityReportCollection = (IList<JoinabilityReport>)(_meetingRespository.GetJoinabilityReport(model.InterviewId));
            //model.SelectabilityReportCollection = (IList<SelectabilityReport>)(_meetingRespository.GetSelectabilityReport(model.InterviewId));
            model.QuestionsCollection = (IList<QuestionsDiscussed>)(_meetingRespository.GetQuestionsDiscussed(model.InterviewId));
            //model.TechSkillReportCollection = (IList<TechSkillReport>)(_meetingRespository.GetTechSkillReport(model.InterviewId));
            model.SkillGeneralReportCollection = (IList<SkillGeneralReport>)(_meetingRespository.GetSkillGeneralReport(model.InterviewId));
            model.Proctoring = _meetingRespository.GetProctoringReport(model.InterviewId);
            model.IsEdited = true;
            model.JobLevelName = Enum.GetName(typeof(JobLevels), model.JobLevel);
            var interviewMeetingsDetails = _meetingRespository.GetInterviewMeetingById(model.InterviewId).Result;
            model.PreferredLocation = interviewMeetingsDetails.preferredLocation;
            model.resumeLink = interviewMeetingsDetails.resumeLink;
            model.currentEmployerName = model.currentEmployerName;
            model.expectedPerHourRate = interviewMeetingsDetails.expectedPerHourRate;
            //model.IsApproved = interviewMeetingsDetails.IsApproved;
            //model.IsSelected = interviewMeetingsDetails.IsSelected;
            //model.FinalComments = interviewMeetingsDetails.FinalComments;
            //model.ApprovalComments = interviewMeetingsDetails.ApprovalComments;
            // model.MeetingStatus = Enum.GetName(typeof(MeetingStatus), model.MeetingStatusList);

            return model;
        }

        public async Task<bool> AddInterviewReport(InterviewReport reportModel)
        {
            try
            {
                
                var result = await _meetingRespository.AddInterviewReport(reportModel);
                if (reportModel.InterviewStatus.HasValue && !reportModel.IsEdited)
                {
                    if (reportModel.InterviewStatus.Value == 2 || reportModel.InterviewStatus.Value == 4)
                    {

                        var irfQualityControllers = _irfRepository.GetTransQualityController(reportModel.IRFId);
                        var mailIds = new List<string>();
                        foreach (var item in irfQualityControllers)
                        {
                            if (item.EmailId != null)
                            {
                                mailIds.Add(item.EmailId);
                            }
                        }
                        _notificationHelper.SendQCInterviewerMail(mailIds, reportModel.interviewDate);

                        if(reportModel.InterviewStatus.Value == 2)
                        {
                            var namemodel = String.Empty;

                            if (reportModel.CandidateName != null && reportModel.CandidateName.Length >= 2)
                                namemodel = namemodel + reportModel.CandidateName?.ToCharArray()[0] + reportModel.CandidateName?.ToCharArray()[1];
                            else if (reportModel.CandidateName != null && reportModel.CandidateName.Length == 1)
                                namemodel = namemodel + reportModel.CandidateName?.ToCharArray()[0];
                            //get curated users
                            var curatedMailIds = _irfRepository.GetCuratedUserMailId(reportModel.IRFId);
                            _notificationHelper.SendCuratedUserMail(curatedMailIds.ToList(), reportModel.interviewDate, reportModel.IRFName, reportModel.PrimarySkill, namemodel);
                        }
                        
                    }
                }

                return result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public VerifyCandidateAudio GetCandidateAudios(int candidateId, int irfId)
        {
            try
            {
                var model = new VerifyCandidateAudio();
                model.CandidateAudios = (IList<CandidateAudio>)_evaluatorRespository.GetCandidateAudios(candidateId, irfId).Where(x => x.IsVoiceBiometric == true).ToList();
                model.CandidateId = candidateId;
                if (model.CandidateAudios.Count() > 0)
                {
                    model.CandidateName = model.CandidateAudios.First().CandidateName;
                    model.IRFId = irfId;
                    model.IRFName = model.CandidateAudios.First().IRFName;
                    model.VoiceItUserId = model.CandidateAudios.First().VoiceItUserId;
                    model.PhraseDetail = model.CandidateAudios.First().PhraseDetail;
                }
                return model;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public string VerifyCandidateVoice(string userId, string fileUrl, string phrase, int meetingId, string type)
        {
            try
            {
                string finalStatus = string.Empty;

                // verify the audio
                var verificationResult = _voiceItRepository.VerifyUser(userId, fileUrl, phrase);

                if (verificationResult.status == "200")
                {
                    if (verificationResult.responseCode == "SUCC")
                    {
                        finalStatus = verificationResult.message + " with confidence " + verificationResult.confidence;
                        if (type == "interview")
                        {
                            _meetingRespository.UpdateInterviewVerificationScore(meetingId, verificationResult.confidence);
                        }
                        if (type == "joining")
                        {
                            _meetingRespository.UpdateJoiningVerificationScore(meetingId, verificationResult.confidence);

                        }
                    }
                    else
                    {
                        finalStatus = verificationResult.message;//"User could not be verified!";
                    }
                }
                else
                {
                    finalStatus = "Verification operation failed!";
                }
                //update verification score in the meeting table


                return finalStatus;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


        public MeetingVM GetMeetingbyid(int id, string Email)
        {
            try
            {
                var meeting = _meetingRespository.GetMeetingbyid(id, Email);
                if (meeting == null)
                    return null;
                else
                    return meeting.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public MeetingVM VedioLike(string value, string vedioid, string email)
        {
            try
            {
                var meeting = _meetingRespository.VedioLikeAsync(value, vedioid, email);
                return meeting.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return null;
            }

        }
        public string AddTotalViewCount(int id,string emailaddress)
        {
            try
            {
                var meeting = _meetingRespository.AddTotalViewCount(id, emailaddress);
                return "";

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return "";
            }
        }
        public string VedioShare(string value, string vedioid)
        {
            try
            {
                var meeting = _meetingRespository.VedioShare(value, vedioid);
                return "";

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return "";
            }

        }
        public string VedioShared(string value, string vedioid,string Userid)
        {
            try
            {
                var meeting = _meetingRespository.VedioShared(value, vedioid, Userid);
                return meeting.Result;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return "";
            }

        }
        public string VedioComment(string value, string vedioid, string email)
        {
            try
            {
                var meeting = _meetingRespository.VedioComment(value, vedioid, email);
                return "";

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return "";
            }

        }
        public List<VedioComments> GetvedioComments(int id)
        {
            try
            {
                var meeting = _meetingRespository.GetvedioComments(id);
                return (List<VedioComments>)meeting;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public EmailDetails GetvedioShortList(string vedioid, string email)
        {
            try
            {
                var meeting = _meetingRespository.GetvedioShortList(vedioid, email);
                return meeting;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public string CandidateSuitable(int id, string email, string Comment)
        {
            try
            {
                var meeting = _meetingRespository.CandidateSuitable(id, email, Comment);
                return meeting;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }

        }
        public VedioComments Intrestreaction(string vedioid, string Status)
        {
            try
            {
                var assignedMeeting = _meetingRespository.Intrestreaction(vedioid, Status);
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string ShortListreaction(string vedioid, string Status)
        {
            try
            {
                var assignedMeeting = _meetingRespository.ShortListreaction(vedioid, Status);
                return "";
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<Meeting> ShortListedClient(string emailID, string IRFID)
        {
            try
            {
                ///*.Where(x=>/*x.interviewDate.Date>DateTime.Now.Date &&*/ x.MeetingStatus==2)*/
                var assignedMeeting = _meetingRespository.ShortListedClient(emailID, IRFID).Result.OrderByDescending(x => x.interviewDate).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<Meeting> InterestListClient(string emailID, string IRFID)
        {
            try
            {
                ///*.Where(x=>/*x.interviewDate.Date>DateTime.Now.Date &&*/ x.MeetingStatus==2)*/
                var assignedMeeting = _meetingRespository.InterestListClient(emailID, IRFID).Result.OrderByDescending(x => x.interviewDate).ToList();
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public string Fetchusername(string Emailid)
        {
            try
            {
                var assignedMeeting = _meetingRespository.Fetchusername(Emailid);
                return assignedMeeting;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public List<VideoCountdetail> GetTotalViewCount(int id)
        {
            try
            {
                var meeting = _meetingRespository.GetTotalViewCount(id);
                return meeting;

            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);

                return null;
            }
        }
        public bool SaveFeedbackAudio(int interviewId, string feedbackAudioLink)
        {
            try
            {
                return _meetingRespository.SaveFeedbackAudio(interviewId, feedbackAudioLink);
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<string> Broadcast(BroadcastModel model)
        {
            try
            {
                //check the receiver and get all their emails from the db
                var receiverEmails = _meetingRespository.GetEmailsforBroadcast(model.BroadcastReciever).Result;
                var supportEmails = _supportRespository.GetAllSupportHREmails();
                var result = _notificationHelper.SendBroadcastMail(receiverEmails.ToList(), supportEmails.ToList(), model);
                return result;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return "Broadcast failed.";
            }

        }

    }

}


