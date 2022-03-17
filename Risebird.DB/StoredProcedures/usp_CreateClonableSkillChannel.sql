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
@clientName varchar(200),            
@JDTiltle nvarchar(200),            
@CompanyType varchar(200),            
@ClientId int,            
@JobCountry varchar(50),            
@StartDate datetime,            
@EndDate datetime,            
@JDLink varchar(1000),            
@NoOfPosition int,            
@NoOfIntterview int,            
@RisebirdHr int,            
@PerMintuteRate real,            
@JobLevel int,            
@CalendlyLink varchar(500),            
@TotalBudget int,            
@CreatedOn date,            
@IsBiometricEnabled bit,            
@PriorityLevel int,            
@CompanyDetails varchar(1000),           
@Type varchar(5),        
@Heuristicsscore int,        
@PrimarySkillInvite varchar(100),        
@BillingOwner int,     
@AssessmentGroupId int,
@IsQCRequired bit,
@IsJoinabilitySelectabilityRequired bit,
@IsGeneralSkillRequired bit,
@JoinabilityBulkData AS [dbo].[TransJoinability_TableType] READONLY ,
@SelectabilityBulkData AS [dbo].[TransSelectability_TableType] READONLY,
@SkillGeneralBulkData AS [dbo].[TransSkillGeneral_TableType] READONLY,
@TechSkillBulkData AS [dbo].[TransTechSkill_TableType] READONLY,
@InterviewerBulkData AS [dbo].[TransInterviewer_TableType] READONLY,
@EmergencyInterviewerBulkData AS [dbo].[TransEmergencyInterviewer_TableType] READONLY,
@ClientUserBulkData AS [dbo].[TransClientUser_TableType] READONLY,
@QCBulkData AS [dbo].[TransQualityController_TableType] READONLY,
@HRBulkData AS [dbo].[TransHR_TableType] READONLY,
@RecommendationStatus bit,
@RecommendationCategoryId int = null,
@RecommendationCompetencyLevelId int = null,
@RecommendationSeniorityLevelId int = null,
@RecommendationCountryType int = null,
@RecommendationCreatedOn datetime
            
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
		   ,[ClientName]            
		   ,[JDTiltle]            
		   ,[CompanyType]            
		   ,[ClientId]            
		   ,[JobCountry]            
		   ,[StartDate]            
		   ,[EndDate]                       
		   ,[JDLink]            
		   ,[NoOfPosition]            
		   ,[NoOfIntterview]            
		   ,[RisebirdHr]            
		   ,[PerMintuteRate]              
		   ,[JobLevel]            
		   ,[CalendlyLink]            
		   ,[CreatedOn]            
		   ,[IsBiometricEnabled]            
		   ,[TotalBudget]            
		   ,[PriorityLevel]            
		   ,[CompanyDetails]            
		   ,[BussinessType]        
		   ,[Heuristicsscore]        
		   ,[PrimarySkillInvite]      
		   ,[BillingOwner]    
		   ,[AssessmentGroupId]
		   ,[IsQCRequired]
		   ,[IsJoinabilitySelectabilityRequired]
		   ,[IsGeneralSkillRequired]
		   ,[CreatedFromIRFId])            
			   VALUES            
				   (@Name,            
		   @clientName,            
		   @JDTiltle,            
		   @CompanyType,            
		   @ClientId,            
		   @JobCountry,            
		   @StartDate,            
		   @EndDate,            
		   @JDLink,            
		   @NoOfPosition,            
		   @NoOfIntterview,            
		   @RisebirdHr,            
		   @PerMintuteRate,            
		   @JobLevel,            
		   @CalendlyLink,            
		   @CreatedOn,            
		   @IsBiometricEnabled,            
		   @TotalBudget,            
		   @PriorityLevel,            
		   @CompanyDetails,          
		   @Type,        
		   @Heuristicsscore,        
		   @PrimarySkillInvite,      
		   @BillingOwner,    
		   @AssessmentGroupId,
		   @IsQCRequired,
		   @IsJoinabilitySelectabilityRequired,
		   @IsGeneralSkillRequired,
		   @CreatedFromIRFId)      
       
    
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

  
    --emergency interviewers
    delete from TransClonableIRFEmergencyInterviewer where ClonableIRFId= @idClonableIrf

	INSERT INTO [dbo].[TransClonableIRFEmergencyInterviewer](  
	[ClonableIRFId] ,  
	[UserId],  
	[PerSuccessRate], 
	[IsActive],
	[InterviewerId])  
   
  	--SELECT eibd.*, InterviewerProfile.id as InterviewerId FROM @EmergencyInterviewerBulkData eibd left join InterviewerProfile on eibd.UserId = InterviewerProfile.UserId  
  
	SELECT @idClonableIrf, eibd.UserId, eibd.PerSuccessRate, eibd.IsActive, InterviewerProfile.id as InterviewerId FROM @EmergencyInterviewerBulkData eibd left join InterviewerProfile on eibd.UserId = InterviewerProfile.UserId  

	--client users
	delete from TransClonableIRFClientUser where ClonableIRFId= @idClonableIrf

	INSERT INTO [dbo].[TransClonableIRFClientUser](  
	[ClonableIRFId] ,  
	[ClientUserId],  
	[ClientRoleId])  
   
  
	--SELECT cubd.*, ClientUser.RoleId as ClientRoleId FROM @ClientUserBulkData cubd left join ClientUser on cubd.ClientUserId= ClientUser.UserId  
 
	SELECT @idClonableIrf, cubd.ClientUserId, ClientUser.RoleId as ClientRoleId FROM @ClientUserBulkData cubd left join ClientUser on cubd.ClientUserId= ClientUser.UserId  

    --quality controllers
 	delete from TransClonableIRFQualityController where ClonableIRFId= @idClonableIrf        

	INSERT INTO [dbo].[TransClonableIRFQualityController](  
	[ClonableIRFId] ,  
	[UserId],  
	[PerSuccessRate],
	[IsActive],
	[InterviewerId])  
     
	SELECT @idClonableIrf, qcbd.UserId, qcbd.PerSuccessRate, qcbd.IsActive, InterviewerProfile.id as InterviewerId FROM @QCBulkData qcbd left join InterviewerProfile on qcbd.UserId = InterviewerProfile.UserId  
 
    --HRs
	delete from TransClonableIRFHR where ClonableIRFId= @idClonableIrf 
	
	INSERT INTO [dbo].[TransClonableIRFHR](  
	[ClonableIRFId] ,  
	[HRId])  
   
	SELECT @idClonableIrf, HR.id as HRId FROM @HRBulkData hrbd left join HR on hrbd.UserId = HR.UserId  
	
	
	
	IF Not EXISTS(Select 1 from IRFRecommendations where CloneIRFId = @idClonableIrf)     
	BEGIN
		insert into IRFRecommendations 
		(
			[CloneIRFId],
			[Status],
			CategoryId,
			CompetencyLevelId,
			SeniorityLevelId,
			CountryType,
			CreatedOn
		)

		values
		(
			@idClonableIrf,
			@RecommendationStatus,
			@RecommendationCategoryId,
			@RecommendationCompetencyLevelId,
			@RecommendationSeniorityLevelId,
			@RecommendationCountryType,
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



