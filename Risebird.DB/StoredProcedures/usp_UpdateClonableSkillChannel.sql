IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateClonableSkillChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateClonableSkillChannel
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_UpdateClonableSkillChannel]              
    
--@Name varchar(200),  
@id int,                        
@ModifiedOn date,     
@ModifiedBy varchar(50), 
@MeetingPlatform varchar(25),
@JoinabilityBulkData AS [dbo].[TransJoinability_TableType] READONLY ,  
@SelectabilityBulkData AS [dbo].[TransSelectability_TableType] READONLY,  
@SkillGeneralBulkData AS [dbo].[TransSkillGeneral_TableType] READONLY,  
@TechSkillBulkData AS [dbo].[TransTechSkill_TableType] READONLY,  
@InterviewerBulkData AS [dbo].[TransInterviewer_TableType] READONLY,  
@QuestionsBulkData AS [dbo].[ClonableTemplateQuestions_TableType] READONLY,  
@RecommendationStatus bit,  
@RecommendationAssessmentGroupId int = null,  
@RecommendationCategoryId int = null,  
--@RecommendationCompetencyLevelId int = null,  
@RecommendationSeniorityLevelId varchar(50) = null,  
@RecommendationCountryType int = null,  
@RecommendationCompanyType int = null,  
@RecommendationModifiedOn datetime,  
@RecommendationDifficultyLevel varchar(50) = null,  
@RecommendationEmploymentType varchar(20) = null,  
@RecommendationPerHourRate int = null,  
@RecommendationPrimarySkills varchar(max) = null,  
@RecommendationClients varchar(max)=null,
@IsAadhaarVerificationRequired bit
              
AS              
Begin    
  
begin try    
  begin tran    
  
  if exists (select 1 from ClonableSkillChannel where id=@id)       
  Begin     
  update ClonableSkillChannel set ModifiedOn = @ModifiedOn,
  ModifiedBy = @ModifiedBy,
  IsAadhaarVerificationRequired=@IsAadhaarVerificationRequired,
  MeetingPlatform = @MeetingPlatform
  where id =@id  
  End  
  
  if(@id != 0)  
  Begin  
  
 --joinability  
  
 if exists(select 1 from @JoinabilityBulkData)  
 Begin  
  delete from TransClonableIRFJoinability where ClonableIRFId= @id  
  
  INSERT INTO [dbo].[TransClonableIRFJoinability](    
  [ClonableIRFId],    
  [LevelId],    
  [Type],    
  [Value],    
  [Weightage])    
    
  SELECT @id, jbd.LevelId, jbd.[Type], jbd.[Value], jbd.[Weightage]  FROM @JoinabilityBulkData jbd  
  
 End  
   
  
 --selectability  
 if exists(select 1 from @SelectabilityBulkData)  
 Begin  
  delete from TransClonableIRFSelectability where ClonableIRFId= @id  
  
  INSERT INTO [dbo].[TransClonableIRFSelectability](    
  [ClonableIRFId] ,    
  [LevelId],    
  [Type],    
  [Value],    
  [Weightage])    
    
  SELECT @id, sbd.LevelId, sbd.[Type], sbd.[Value], sbd.[Weightage]  FROM @SelectabilityBulkData sbd  
 End  
   
  
 --general skills  
 if exists(select 1 from @SkillGeneralBulkData)  
 Begin  
  delete from TransClonableIRFSkillGeneral where ClonableIRFId= @id  
  
  INSERT INTO [dbo].[TransClonableIRFSkillGeneral](    
  [ClonableIRFId] ,    
  [LevelId],    
  [Type],    
  [Value],    
  [Weightage])    
    
  SELECT @id, sgbd.LevelId, sgbd.[Type], sgbd.[Value], sgbd.[Weightage]  FROM @SkillGeneralBulkData sgbd  
 End  
   
  
 --tech skills  
 delete from TransClonableIRFTechSkill where ClonableIRFId= @id  
  
 INSERT INTO [dbo].[TransClonableIRFTechSkill](      
 [ClonableIRFId] ,      
 [LevelId],      
 [SkillId],      
 [Weightage],[Concepts],[Natureofquestion],[Timelimit])      
      
 SELECT @id, tsbd.LevelId, tsbd.[SkillId], tsbd.[Weightage], tsbd.[Concepts], tsbd.[Natureofquestion], tsbd.[Timelimit]  FROM @TechSkillBulkData tsbd  
  
 --interviewers  
 delete from TransClonableIRFInterviewer where ClonableIRFId= @id  
  
 INSERT INTO [dbo].[TransClonableIRFInterviewer](    
 [ClonableIRFId] ,    
 [UserId],    
 [PerSuccessRate],   
 [IsActive],  
 [IsAvailable],  
 [InterviewerId])    
      
 SELECT @id, ibd.UserId, ibd.PerSuccessRate, ibd.IsActive, ibd.IsAvailable, InterviewerProfile.id   
 as InterviewerId FROM @InterviewerBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId    
  
  
 --questions  
 delete from ClonableTemplateQuestions where ClonableIRFId= @id  
  
 INSERT INTO [dbo].[ClonableTemplateQuestions](    
 [ClonableIRFId] ,    
 [UserId],    
 [Skill],    
 [Question])    
    
 SELECT @id, qbd.UserId, qbd.[Skill], qbd.[Question] FROM @QuestionsBulkData qbd  
  
   
 IF EXISTS(Select 1 from IRFRecommendations where CloneIRFId = @id)       
 BEGIN  
  
  update IRFRecommendations set  
  [Status]=@RecommendationStatus,  
  --CompetencyLevelId = @RecommendationCompetencyLevelId,  
  SeniorityLevelId = @RecommendationSeniorityLevelId,  
  CountryType = @RecommendationCountryType,  
  CompanyType = @RecommendationCompanyType,  
  ModifiedOn = @RecommendationModifiedOn,  
  DifficultyLevel = @RecommendationDifficultyLevel,  
  EmploymentType = @RecommendationEmploymentType,  
  PerHourRate = @RecommendationPerHourRate,  
  PrimarySkills = @RecommendationPrimarySkills,  
  Clients = @RecommendationClients  
  Where CloneIRFId = @id  
   
 End  
  
 END   
   
  
    
  commit tran    
 end try    
 begin catch    
    
  rollback transaction    
    
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),    
   @Severity int = ERROR_SEVERITY(),    
   @State smallint = ERROR_STATE()    
     
  RAISERROR (@Message, @Severity, @State)    
     
 end catch  
  
End            
   
GO



