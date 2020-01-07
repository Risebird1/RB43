using Microsoft.AspNetCore.Mvc.Rendering;
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
    public class EvaluatorService : IEvaluatorService
    {
        private IEvaluatorRepository _evaluatorRespository;
        private IApplicationErrorLogger _applicationErrorLogger;
        private readonly INotificationHelper _notificationHelper;
        private IMeetingRespository _meetingRespository;

        public EvaluatorService(IEvaluatorRepository evaluatorRespository, INotificationHelper notificationHelper, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository)
        {
            _evaluatorRespository = evaluatorRespository;
            _applicationErrorLogger = applicationErrorLogger;
            _notificationHelper = notificationHelper;
            _meetingRespository = meetingRespository;
        }

        public List<CandidateResumeEvaluation> GetOpenEvaluations(string emailId)
        {
            try
            {
                return _evaluatorRespository.GetOpenEvaluations(emailId).Result.ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public string AssignSelectabilityYourself(int id, string mailId)
        {
            try
            {
                return _evaluatorRespository.AssignYourselfSelectability(id, mailId);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }

        }
        public string AssignJoinabilityYourself(int id, string mailId)
        {
            try
            {
                return _evaluatorRespository.AssignYourselfJoinability(id, mailId);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return "Failed to assigned interview, Please contact Risebird HR.";
            }

        }

        public List<CandidateResumeEvaluation> GetAssignedSelectabilityEvaluations(string emailID)
        {
            try
            {
                var assignedEvaluations =_evaluatorRespository.GetAssignedSelectabilityEvaluations(emailID).Result.ToList();                
                return assignedEvaluations;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public bool UnAssignSelectability(int id)
        {
            try
            {
                return _evaluatorRespository.UnAssignSelectability(id);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }
        public async Task<EvaluatorReport> GetEvaluatorReport(int candidateId, int irfId)
        {
            try
            {
                //check whether an IRF exists for the given IRFId
                //var check = _meetingRespository.GetIRFbyId(irfId);
                //if (!check)
                //{
                //    return null;
                //}
                // check whether a feedback for the interviewId has already been submitted
                var selectabilityReport = _evaluatorRespository.GetSelectabilityReport(candidateId, irfId);

                var model = new EvaluatorReport();
                var levels = _meetingRespository.GetLevels();

                if (selectabilityReport != null && selectabilityReport.Count() > 0 /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    model.IsEdited = true;
                    model.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityReport;

                }
                else
                {
                    model.IsEdited = false;

                    var selectabilityDetails = _evaluatorRespository.GetTransSelectability(irfId);
                    var selectabilityList = new List<SelectabilityEvaluatorReport>();
                    foreach (var item in selectabilityDetails)
                    {
                        SelectabilityEvaluatorReport obj = new SelectabilityEvaluatorReport();
                        obj.CandidateId = candidateId;
                        obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.Type = item.Type;
                        obj.Name = item.Name;
                        obj.Value = item.Value;
                        obj.WeightageRequired = item.Weightage;
                        selectabilityList.Add(obj);
                    }

                    model.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityList;
                }

                //get all values from database
               

                var candidateDetails = _evaluatorRespository.GetCandidate(candidateId);
                

                model.CandidateName = candidateDetails.Name;
                model.ResumeLink = candidateDetails.ResumeLink;
                model.IRFId = irfId;
                model.IRFName = _meetingRespository.GetIRFName(irfId).FirstOrDefault();
                model.currentEmployerName = candidateDetails.CurrentEmployer;
                model.PreferredLocation = candidateDetails.Location;
                model.CandidateEmailId = candidateDetails.EmailId;
                model.SelectabilityComments = _evaluatorRespository.GetSelectabilityComments(irfId, candidateDetails.EmailId);
                model.TechSkillComments = _evaluatorRespository.GetTechComments(irfId, candidateDetails.EmailId);
                model.SkillGeneralComments = _evaluatorRespository.GetGeneralSkillComments(irfId, candidateDetails.EmailId);
                model.Level = new SelectList(levels, "id", "Name");
                model.LevelTech = new SelectList(_evaluatorRespository.GetLevelTech(), "id", "Name");
                return model;
                
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<IList<TechSkillEvaluatorReport>> GetTechReport(int candidateId, int irfId)
        {
            try
            {
                
                // check whether a feedback for the interviewId has already been submitted
                var techReport = (IList<TechSkillEvaluatorReport>)_evaluatorRespository.GetTechReport(candidateId, irfId);

                if (techReport == null || techReport.Count() == 0)
                {
                    var techSkillsDetails = _meetingRespository.GetSkillsbyIRFid(irfId);
                    var techSkillList = new List<TechSkillEvaluatorReport>();
                    var levelTech = _meetingRespository.GetLevelTech();
                    var skills = _meetingRespository.GetSkills();

                    //assign values to model
                    foreach (var item in techSkillsDetails)
                    {
                        TechSkillEvaluatorReport obj = new TechSkillEvaluatorReport();
                        obj.CandidateId = candidateId;
                        obj.IRFId = irfId;
                        obj.RequiredLevel = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levelTech.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.SkillName = skills.Where(x => x.id == item.SkillId).FirstOrDefault().Name;
                        obj.SkillId = skills.Where(x => x.id == item.SkillId).FirstOrDefault().id;
                        obj.WeightageRequired = item.Weightage;

                        techSkillList.Add(obj);
                    }
                    techReport = (IList<TechSkillEvaluatorReport>)techSkillList;
                }

                return techReport;
                //get all values from database
                

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }
        public async Task<IList<SkillGeneralEvaluatorReport>> GetSkillGeneralReport(int candidateId, int irfId)
        {
            try
            {
                
                // check whether a feedback for the interviewId has already been submitted
                var skillGeneralReport = (IList<SkillGeneralEvaluatorReport>)_evaluatorRespository.GetSkillGeneralReport(candidateId, irfId);
                
                if (skillGeneralReport == null || skillGeneralReport.Count() == 0)
                {
                    var genSkillsDetails = _meetingRespository.GetSkillsGeneralbyIRFid(irfId);
                    var genSkillList = new List<SkillGeneralEvaluatorReport>();
                    var levels = _meetingRespository.GetLevels();

                    //assign values to model
                    foreach (var item in genSkillsDetails)
                    {
                        SkillGeneralEvaluatorReport obj = new SkillGeneralEvaluatorReport();
                        obj.CandidateId = candidateId;
                        obj.IRFId = irfId;
                        obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.Type = item.Type;
                        obj.Name = item.Name;
                        obj.Value = item.Value;
                        obj.WeightageRequired = item.Weightage;
                        genSkillList.Add(obj);
                    }
                    skillGeneralReport = (IList<SkillGeneralEvaluatorReport>)genSkillList;
                }

                return skillGeneralReport;
                //get all values from database


            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }



        public async Task<bool> AddSelectabilityReport(EvaluatorReport reportModel)
        {
            try
            {
                //bool joinResult = false;
                reportModel.HRReport.CandidateEmailId = reportModel.CandidateEmailId;
                reportModel.HRReport.CandidateId = reportModel.CandidateId;
                reportModel.HRReport.IRFId = reportModel.IRFId;
                
                var joinResult = _evaluatorRespository.AddJoinabilityReport(reportModel.HRReport).Result;
                
                var selecResult = _evaluatorRespository.AddSelectabilityReport(reportModel).Result;
                var techResult = _evaluatorRespository.AddTechReport(reportModel.TechSkillEvaluatorReportCollection, reportModel.CandidateId, reportModel.IRFId).Result;
                var genSkillResult = _evaluatorRespository.AddSkillGeneralReport(reportModel.SkillGeneralEvaluatorReportCollection, reportModel.CandidateId, reportModel.IRFId).Result;
                if (joinResult && selecResult && techResult && genSkillResult)
                {
                    return true;
                }
                else

                    return false; 
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public List<CandidateHREvaluation> GetEvaluationsHR(string emailId)
        {
            try
            {
                return _evaluatorRespository.GetEvaluationsHR(emailId).Result.ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public List<CandidateHREvaluation> GetAssignedJoinabilityEvaluations(string emailID)
        {
            try
            {
                var assignedEvaluations = _evaluatorRespository.GetAssignedJoinabilityEvaluations(emailID).Result.ToList();
                return assignedEvaluations;
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public bool UnAssignJoinability(int id)
        {
            try
            {
                return _evaluatorRespository.UnAssignJoinability(id);

            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return false;
            }
        }

        public async Task<HRReport> GetHRJoinabilityReport(int candidateId, int irfId)
        {
            try
            {
                //check whether an IRF exists for the given IRFId
                //var check = _meetingRespository.GetIRFbyId(irfId);
                //if (!check)
                //{
                //    return null;
                //}
                // check whether a feedback for the interviewId has already been submitted
                var joinabilityReport = _evaluatorRespository.GetJoinabilityReport(candidateId, irfId);

                var model = new HRReport();
                var levels = _meetingRespository.GetLevels();

                if (joinabilityReport != null && joinabilityReport.Count() > 0 /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    model.IsEdited = true;
                    model.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityReport;

                }
                else
                {
                    model.IsEdited = false;

                    var joinabilityDetails = _meetingRespository.GetTransJoinability(irfId);
                    var joinabilityList = new List<JoinabilityHRReport>();
                    foreach (var item in joinabilityDetails)
                    {
                        JoinabilityHRReport obj = new JoinabilityHRReport();
                        obj.CandidateId = candidateId;
                        obj.RequiredLevel = levels.Where(x => x.id == item.LevelId).FirstOrDefault().Name;
                        obj.RequiredLevelId = levels.Where(x => x.id == item.LevelId).FirstOrDefault().id;
                        obj.Type = item.Type;
                        obj.Name = item.Name;
                        obj.Value = item.Value;
                        obj.WeightageRequired = item.Weightage;
                        joinabilityList.Add(obj);
                    }

                    model.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityList;
                }

                //get all values from database
                
                var candidateDetails = _evaluatorRespository.GetCandidate(candidateId);
                
                model.CandidateName = candidateDetails.Name;
                model.ResumeLink = candidateDetails.ResumeLink;
                model.IRFId = irfId;
                model.IRFName = _meetingRespository.GetIRFName(irfId).FirstOrDefault();
                model.currentEmployerName = candidateDetails.CurrentEmployer;
                model.PreferredLocation = candidateDetails.Location;
                model.CandidateEmailId = candidateDetails.EmailId;
                model.JoinabilityComments = _evaluatorRespository.GetJoinabilityComments(irfId, candidateDetails.EmailId);
                model.TechSkillComments = _evaluatorRespository.GetTechComments(irfId, candidateDetails.EmailId);
                model.SkillGeneralComments = _evaluatorRespository.GetGeneralSkillComments(irfId, candidateDetails.EmailId);
                model.Level = new SelectList(levels, "id", "Name");
                model.LevelTech = new SelectList(_evaluatorRespository.GetLevelTech(), "id", "Name");

                return model;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public async Task<bool> AddJoinabilityReport(HRReport reportModel)
        {
            try
            {
                reportModel.EvaluatorReport.CandidateEmailId = reportModel.CandidateEmailId;
                reportModel.EvaluatorReport.CandidateId = reportModel.CandidateId;
                reportModel.EvaluatorReport.IRFId = reportModel.IRFId;
                reportModel.EvaluatorReport.TechSkillComments = reportModel.TechSkillComments;
                reportModel.EvaluatorReport.TechSkillScore = reportModel.TechSkillScore;
                reportModel.EvaluatorReport.SkillGeneralComments = reportModel.SkillGeneralComments;
                reportModel.EvaluatorReport.SkillGeneralScore = reportModel.SkillGeneralScore;
                var selecResult = _evaluatorRespository.AddSelectabilityReport(reportModel.EvaluatorReport).Result;
                var joinResult =  _evaluatorRespository.AddJoinabilityReport(reportModel).Result;
                var techResult = _evaluatorRespository.AddTechReport(reportModel.TechSkillEvaluatorReportCollection, reportModel.CandidateId, reportModel.IRFId).Result;
                var genSkillResult = _evaluatorRespository.AddSkillGeneralReport(reportModel.SkillGeneralEvaluatorReportCollection, reportModel.CandidateId, reportModel.IRFId).Result;
                if (joinResult && selecResult && techResult && genSkillResult)
                {
                    return true;
                }
                else
                    return false;

            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }


    }
}
