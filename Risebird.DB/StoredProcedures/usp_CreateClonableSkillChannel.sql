IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CreateClonableSkillChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_CreateClonableSkillChannel
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

 Create PROCEDURE [dbo].[usp_CreateClonableSkillChannel]            
  
@Name varchar(200),
@CreatedFromIRFId int,                        
@CreatedOn date,  
@CreatedBy varchar(50),
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
@RecommendationCreatedOn datetime,
@RecommendationDifficultyLevel varchar(50) = null,
@RecommendationEmploymentType varchar(20) = null,
@RecommendationPerHourRate int = null
            
AS            
Begin  

begin try  
  begin tran  
  
  declare @idClonableIrf int
  set @idClonableIrf = 0

  if not exists (select 1 from ClonableSkillChannel where [Name] = @Name)     
  Begin   

		INSERT INTO [dbo].[ClonableSkillChannel]            
		   ([Name]            
		   ,[CreatedOn]            
		   ,[CreatedFromIRFId]
		   ,[CreatedBy]) 
		   
		VALUES            
		  (@Name,            
		   @CreatedOn,            
		   @CreatedFromIRFId, 
		   @CreatedBy)      
       
		select @idClonableIrf = SCOPE_IDENTITY()    
  End

  if(@idClonableIrf != 0)
  Begin
	--joinability
	delete from TransClonableIRFJoinability where ClonableIRFId= @idClonableIrf

	INSERT INTO [dbo].[TransClonableIRFJoinability](  
	[ClonableIRFId],  
	[LevelId],  
	[Type],  
	[Value],  
	[Weightage])  
  
	SELECT @idClonableIrf, jbd.LevelId, jbd.[Type], jbd.[Value], jbd.[Weightage]  FROM @JoinabilityBulkData jbd

	--selectability
	delete from TransClonableIRFSelectability where ClonableIRFId= @idClonableIrf

	INSERT INTO [dbo].[TransClonableIRFSelectability](  
	[ClonableIRFId] ,  
	[LevelId],  
	[Type],  
	[Value],  
	[Weightage])  
  
	SELECT @idClonableIrf, sbd.LevelId, sbd.[Type], sbd.[Value], sbd.[Weightage]  FROM @SelectabilityBulkData sbd

	--general skills
	delete from TransClonableIRFSkillGeneral where ClonableIRFId= @idClonableIrf

	INSERT INTO [dbo].[TransClonableIRFSkillGeneral](  
	[ClonableIRFId] ,  
	[LevelId],  
	[Type],  
	[Value],  
	[Weightage])  
  
	SELECT @idClonableIrf, sgbd.LevelId, sgbd.[Type], sgbd.[Value], sgbd.[Weightage]  FROM @SkillGeneralBulkData sgbd

	--tech skills
	delete from TransClonableIRFTechSkill where ClonableIRFId= @idClonableIrf

	INSERT INTO [dbo].[TransClonableIRFTechSkill](    
	[ClonableIRFId] ,    
	[LevelId],    
	[SkillId],    
	[Weightage],[Concepts],[Natureofquestion],[Timelimit])    
    
	SELECT @idClonableIrf, tsbd.LevelId, tsbd.[SkillId], tsbd.[Weightage], tsbd.[Concepts], tsbd.[Natureofquestion], tsbd.[Timelimit]  FROM @TechSkillBulkData tsbd

	--interviewers
	delete from TransClonableIRFInterviewer where ClonableIRFId= @idClonableIrf

	INSERT INTO [dbo].[TransClonableIRFInterviewer](  
	[ClonableIRFId] ,  
	[UserId],  
	[PerSuccessRate], 
	[IsActive],
	[IsAvailable],
	[InterviewerId])  
    
	SELECT @idClonableIrf, ibd.UserId, ibd.PerSuccessRate, ibd.IsActive, ibd.IsAvailable, InterviewerProfile.id 
	as InterviewerId FROM @InterviewerBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId  


	--questions
	delete from ClonableTemplateQuestions where ClonableIRFId= @idClonableIrf

	INSERT INTO [dbo].[ClonableTemplateQuestions](  
	[ClonableIRFId] ,  
	[UserId],  
	[Skill],  
	[Question])  
  
	SELECT @idClonableIrf, qbd.UserId, qbd.[Skill], qbd.[Question] FROM @QuestionsBulkData qbd

	
	IF Not EXISTS(Select 1 from IRFRecommendations where CloneIRFId = @idClonableIrf)     
	BEGIN
		insert into IRFRecommendations 
		(
			[CloneIRFId],
			[Status],
			[AssessmentGroupId],
			CategoryId,
			--CompetencyLevelId,
			SeniorityLevelId,
			CountryType,
			CompanyType,
			DifficultyLevel,
			EmploymentType,
			PerHourRate,
			CreatedOn
			
		)

		values
		(
			@idClonableIrf,
			@RecommendationStatus,
			@RecommendationAssessmentGroupId,
			@RecommendationCategoryId,
			--@RecommendationCompetencyLevelId,
			@RecommendationSeniorityLevelId,
			@RecommendationCountryType,
			@RecommendationCompanyType,
			@RecommendationDifficultyLevel,
			@RecommendationEmploymentType,
			@RecommendationPerHourRate,
			@RecommendationCreatedOn
		)


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



