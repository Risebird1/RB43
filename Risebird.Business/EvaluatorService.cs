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
using System.Threading.Tasks;

namespace Risebird.Business
{
    public class EvaluatorService : IEvaluatorService
    {
        private readonly IEvaluatorRepository _evaluatorRespository;
        private readonly IVoiceItRepository _voiceItRepository;
        private readonly IApplicationErrorLogger _applicationErrorLogger;
        private readonly IMeetingRespository _meetingRespository;

        public EvaluatorService(IEvaluatorRepository evaluatorRespository, IApplicationErrorLogger applicationErrorLogger, IMeetingRespository meetingRespository, IVoiceItRepository voiceItRepository)
        {
            _evaluatorRespository = evaluatorRespository;
            _applicationErrorLogger = applicationErrorLogger;
            _meetingRespository = meetingRespository;
            _voiceItRepository = voiceItRepository;
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
                var assignedEvaluations = _evaluatorRespository.GetAssignedSelectabilityEvaluations(emailID).Result.ToList();
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
                var selectabilityReport = await _evaluatorRespository.GetSelectabilityReport(candidateId, irfId);

                var model = new EvaluatorReport();
                var levels = await _meetingRespository.GetLevels();

                if (selectabilityReport != null && selectabilityReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    model.IsEdited = true;
                    model.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityReport;

                }
                else
                {
                    model.IsEdited = false;

                    var selectabilityDetails = await _evaluatorRespository.GetTransSelectability(irfId);
                    var selectabilityList = new List<SelectabilityEvaluatorReport>();
                    foreach (var item in selectabilityDetails)
                    {
                        SelectabilityEvaluatorReport obj = new SelectabilityEvaluatorReport
                        {
                            CandidateId = candidateId,
                            RequiredLevel = levels.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levels.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        selectabilityList.Add(obj);
                    }

                    model.SelectabilityEvaluatorReportCollection = (IList<SelectabilityEvaluatorReport>)selectabilityList;
                }

                //get all values from database


                var candidateDetails = await _evaluatorRespository.GetCandidate(candidateId);
                var irfDetails = await _meetingRespository.GetIRFNameAndJoinability(irfId);

                model.CandidateName = candidateDetails.Name;
                model.ResumeLink = candidateDetails.ResumeLink;
                model.IRFId = irfId;
                //model.IRFName = (await _meetingRespository.GetIRFName(irfId)).FirstOrDefault();
                model.IRFName = irfDetails.IRFId;
                model.IsJoinabilitySelectabilityRequired = irfDetails.IsJoinabilitySelectabilityRequired;
                model.currentEmployerName = candidateDetails.CurrentEmployer;
                model.PreferredLocation = candidateDetails.Location;
                model.CandidateEmailId = candidateDetails.EmailId;
                model.SelectabilityComments = await _evaluatorRespository.GetSelectabilityComments(irfId, candidateDetails.EmailId);
                model.TechSkillComments = await _evaluatorRespository.GetTechComments(irfId, candidateId);
                model.SkillGeneralComments = await _evaluatorRespository.GetGeneralSkillComments(irfId, candidateId);
                model.Level = new SelectList(levels, "id", "Name");
                model.LevelTech = new SelectList(await _evaluatorRespository.GetLevelTech(), "id", "Name");
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
                var techReport = (IList<TechSkillEvaluatorReport>)(await _evaluatorRespository.GetTechReport(candidateId, irfId));

                if (techReport == null || !techReport.Any())
                {
                    var techSkillsDetails = await _meetingRespository.GetSkillsbyIRFid(irfId);
                    var techSkillList = new List<TechSkillEvaluatorReport>();
                    var levelTech = await _meetingRespository.GetLevelTech();
                    var skills = await _meetingRespository.GetSkills();

                    //assign values to model
                    foreach (var item in techSkillsDetails)
                    {
                        TechSkillEvaluatorReport obj = new TechSkillEvaluatorReport
                        {
                            CandidateId = candidateId,
                            IRFId = irfId,
                            RequiredLevel = levelTech.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levelTech.FirstOrDefault(x => x.id == item.LevelId).id,
                            SkillName = skills.FirstOrDefault(x => x.id == item.SkillId)?.Name,
                            SkillId = skills.FirstOrDefault(x => x.id == item.SkillId).id,
                            WeightageRequired = item.Weightage
                        };

                        techSkillList.Add(obj);
                    }
                    techReport = (IList<TechSkillEvaluatorReport>)techSkillList;
                }

                return techReport;


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
                var skillGeneralReport = (IList<SkillGeneralEvaluatorReport>)(await _evaluatorRespository.GetSkillGeneralReport(candidateId, irfId));

                if (skillGeneralReport == null || !skillGeneralReport.Any())
                {
                    var genSkillsDetails = await _meetingRespository.GetSkillsGeneralbyIRFid(irfId);
                    var genSkillList = new List<SkillGeneralEvaluatorReport>();
                    var levelTech = await _meetingRespository.GetLevelTech();

                    //assign values to model
                    foreach (var item in genSkillsDetails)
                    {
                        SkillGeneralEvaluatorReport obj = new SkillGeneralEvaluatorReport
                        {
                            CandidateId = candidateId,
                            IRFId = irfId,
                            RequiredLevel = levelTech.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levelTech.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        genSkillList.Add(obj);
                    }
                    skillGeneralReport = (IList<SkillGeneralEvaluatorReport>)genSkillList;
                }

                return skillGeneralReport;


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
                var joinabilityReport = await _evaluatorRespository.GetJoinabilityReport(candidateId, irfId);

                var model = new HRReport();
                var levels = await _meetingRespository.GetLevels();

                if (joinabilityReport != null && joinabilityReport.Any() /*&& interviewReport.FirstOrDefault().Rating > 0*/)
                {
                    model.IsEdited = true;
                    model.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityReport;

                }
                else
                {
                    model.IsEdited = false;

                    var joinabilityDetails = await _meetingRespository.GetTransJoinability(irfId);
                    var joinabilityList = new List<JoinabilityHRReport>();
                    foreach (var item in joinabilityDetails)
                    {
                        JoinabilityHRReport obj = new JoinabilityHRReport
                        {
                            CandidateId = candidateId,
                            RequiredLevel = levels.FirstOrDefault(x => x.id == item.LevelId)?.Name,
                            RequiredLevelId = levels.FirstOrDefault(x => x.id == item.LevelId).id,
                            Type = item.Type,
                            Name = item.Name,
                            Value = item.Value,
                            WeightageRequired = item.Weightage
                        };
                        joinabilityList.Add(obj);
                    }

                    model.JoinabilityHRReportCollection = (IList<JoinabilityHRReport>)joinabilityList;
                }

                //get all values from database

                var candidateDetails = await _evaluatorRespository.GetCandidate(candidateId);

                model.CandidateName = candidateDetails.Name;
                model.ResumeLink = candidateDetails.ResumeLink;
                model.IRFId = irfId;
                model.IRFName = (await _meetingRespository.GetIRFName(irfId)).FirstOrDefault();
                model.currentEmployerName = candidateDetails.CurrentEmployer;
                model.PreferredLocation = candidateDetails.Location;
                model.CandidateEmailId = candidateDetails.EmailId;
                model.JoinabilityComments = await _evaluatorRespository.GetJoinabilityComments(irfId, candidateDetails.EmailId);
                model.TechSkillComments = await _evaluatorRespository.GetTechComments(irfId, candidateId);
                model.SkillGeneralComments = await _evaluatorRespository.GetGeneralSkillComments(irfId, candidateId);
                model.Level = new SelectList(levels, "id", "Name");
                model.LevelTech = new SelectList(await _evaluatorRespository.GetLevelTech(), "id", "Name");

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
                var joinResult = _evaluatorRespository.AddJoinabilityReport(reportModel).Result;
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

        public List<CandidateRecruiter> GetRecruiterCandidates(string emailId, string profile)
        {
            try
            {
                var userId = _evaluatorRespository.GetUserId(emailId, profile);
                return _evaluatorRespository.GetRecruiterCandidates(userId).Result.ToList();
            }
            catch (Exception ex)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<CandidateAudio> GetCandidate(int id, int irfId)
        {
            try
            {
                return await _evaluatorRespository.GetCandidate(id, irfId);
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }
        public async Task<IEnumerable<Phrase>> GetPhrases()
        {
            try
            {
                return await _evaluatorRespository.GetPhrases();
            }
            catch (Exception ex)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, ex);
                return null;
            }
        }

        public async Task<bool> AddCandidateAudio(CandidateAudio obj, string emailId, string profile)
        {
            try
            {
                if (obj.IsVoiceBiometric)
                {
                    obj.Description = obj.DropDescription;
                }
                obj.UploadedByUserId = _evaluatorRespository.GetUserId(emailId, profile);
                return _evaluatorRespository.AddCandidateAudio(obj).Result;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return false;
            }
        }

        public async Task<IList<CandidateAudio>> GetCandidateAudios(int candidateId, int irfId)
        {
            try
            {
                var candidateAudios = (IList<CandidateAudio>)(await _evaluatorRespository.GetCandidateAudios(candidateId, irfId));
                return candidateAudios;
            }

            catch (Exception e)
            {
                await _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }

        public string EnrollCandidate(IList<CandidateAudio> candidateList)
        {
            try
            {
                string voiceItUserId = string.Empty;
                string finalStatus = "Successful!";
                //check if the candidate is a  VoiceIt user already
                if (candidateList != null && string.IsNullOrEmpty(candidateList.First().VoiceItUserId))
                {
                    //create user
                    var result = _voiceItRepository.CreateUser();
                    if (result.responseCode.Contains("SUCC"))
                    {
                        //save userID to database
                        voiceItUserId = result.userId;
                        _evaluatorRespository.UpdateCandidateVoiceUserId(result.userId, candidateList.First().CandidateId, candidateList.First().IRFId);
                    }
                    else
                    {
                        return "Failed!";
                    }
                }
                else
                {
                    voiceItUserId = candidateList.First().VoiceItUserId;
                }
                foreach (var item in candidateList)
                {
                    if (!item.IsEnrolled)
                    {
                        var enrollmentResult = _voiceItRepository.EnrollUser(voiceItUserId, item.AudioFileLink, item.PhraseDetail);

                        if (enrollmentResult.Contains("Success"))
                        {
                            item.IsEnrolled = true;
                        }
                        else
                        {
                            finalStatus = "Failed";

                        }
                        //update database
                        _evaluatorRespository.UpdateEnrollmentStatus(item.id, item.IsEnrolled, enrollmentResult);


                    }

                }

                return finalStatus;
            }

            catch (Exception e)
            {
                _applicationErrorLogger.LogMessage(LogLevelInfo.Exception, e);
                return null;
            }
        }


    }
}
