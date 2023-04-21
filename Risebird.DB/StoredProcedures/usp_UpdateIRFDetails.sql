IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateIRFDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_UpdateIRFDetails]      
(      
 @IRFId nvarchar(200),          
 @id int,          
 @ClientName varchar(200),          
 @JDTiltle nvarchar(200),          
 @CompanyType varchar(200),          
 @ClientId int,          
 @JobCountry varchar(50),          
 @StartDate datetime,          
 @EndDate datetime,          
 @JDLink varchar(1000)=null,          
 @NoOfPosition int,          
 @NoOfIntterview int,          
 @RisebirdHr int,          
 @JobLevel int,          
 @PerMintuteRate real,          
 @CalendlyLink varchar(500),          
 @TotalBudget int,          
 @IsBiometricEnabled bit,          
 @PriorityLevel int,          
 @CompanyDetails varchar(1000),          
 @Type varchar(5),       
 @Heuristicsscore int,      
 @PrimarySkillInvite varchar(100),    
 @BillingOwner int,    
 @AssessmentGroupId varchar(100),  
 @MeetingPlatform varchar(100),  
 --@IsQCRequired bit,  
 @IsJoinabilitySelectabilityRequired bit,  
 @IsGeneralSkillRequired bit,  
 @IsAadhaarVerificationRequired bit,  
 @IsCandidateavailability bit,  
 @IsMeetingAutoCancelled bit,  
 @IsMeetingNotificationSend bit,  
 @SkillChannelSME int = null,  
 @ModifiedBy varchar(100) = null,  
 @timeNow datetime,  
 @isSMEChanged bit output,  
 @IsLocked bit,  
 @InterviewDuration int,  
 @IsFixedInterviewduration bit,  
 @JoinabilityBulkData AS [dbo].[TransJoinability_TableType] READONLY ,  
 @SelectabilityBulkData AS [dbo].[TransSelectability_TableType] READONLY,  
 @SkillGeneralBulkData AS [dbo].[TransSkillGeneral_TableType] READONLY,  
 @TechSkillBulkData AS [dbo].[TransTechSkill_TableType] READONLY,  
 @InterviewerBulkData AS [dbo].[TransInterviewer_TableType] READONLY,  
 @EmergencyInterviewerBulkData AS [dbo].[TransEmergencyInterviewer_TableType] READONLY,  
 @ClientUserBulkData AS [dbo].[TransClientUser_TableType] READONLY,  
 --@QCBulkData AS [dbo].[TransQualityController_TableType] READONLY,  
 @HRBulkData AS [dbo].[TransHR_TableType] READONLY  
 --@idRecommendation int,  
 --@RecommendationStatus bit,  
 --@RecommendationCategoryId int,  
 --@RecommendationCompetencyLevelId int,  
 --@RecommendationSeniorityLevelId int,  
 --@RecommendationCountryType int,  
 --@RecommendationModifiedOn datetime  
  
)      
AS      
BEGIN      
    
    
<<<<<<< HEAD
	SELECT * FROM @TechSkillBulkData 

	--interviewers
	delete from TransInterviewer where IRFId=@id and IsActive=1

	INSERT INTO [dbo].[TransInterviewer](  
	[IRFId] ,  
	[UserId],  
	[PerSuccessRate], 
	[IsActive],
	[IsAvailable],
	[InterviewerId])  
    
	SELECT ibd.*, InterviewerProfile.id as InterviewerId FROM @InterviewerBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId  
  
    --emergency interviewers
    delete from TransEmergencyInterviewer where IRFId=@id and IsActive=1

	INSERT INTO [dbo].[TransEmergencyInterviewer](  
	[IRFId] ,  
	[UserId],  
	[PerSuccessRate], 
	[IsActive],
	[InterviewerId])  
   
  	SELECT eibd.*, InterviewerProfile.id as InterviewerId FROM @EmergencyInterviewerBulkData eibd left join InterviewerProfile on eibd.UserId = InterviewerProfile.UserId  
  
	--client users
	delete from TransClientUser where IRFId=@id 

	INSERT INTO [dbo].[TransClientUser](  
	[IRFId] ,  
	[ClientUserId],  
	[ClientRoleId])  
   
  
	SELECT cubd.*, ClientUser.RoleId as ClientRoleId FROM @ClientUserBulkData cubd left join ClientUser on cubd.ClientUserId= ClientUser.UserId  
 
    --quality controllers
 --	delete from TransQualityController where IRFId=@id        

	--INSERT INTO [dbo].[TransQualityController](  
	--[IRFId] ,  
	--[UserId],  
	--[PerSuccessRate],
	--[IsActive],
	--[InterviewerId])  
=======
 begin try    
  begin tran    
>>>>>>> qaRelease
     
   declare @oldCalendlyLink varchar(500), @isCalendlyChanged bit   
 set @isCalendlyChanged=0  
 select @oldCalendlyLink = CalendlyLink from IRF where id =@id  
  
 if(@oldCalendlyLink != @CalendlyLink)  
 begin  
  set @isCalendlyChanged = 1  
 end  
  
 update IRF           
 set          
 [ClientName]=@ClientName          
 ,[JDTiltle]=@JDTiltle          
 ,[CompanyType]=@CompanyType               
 ,[JobCountry]=@JobCountry          
 ,[StartDate]=@StartDate          
 ,[EndDate]=@EndDate                     
 ,[JDLink]=@JDLink          
 ,[NoOfPosition]=@NoOfPosition          
 ,[NoOfIntterview]=@NoOfIntterview          
 ,[RisebirdHr]=@RisebirdHr          
 ,[JobLevel]=@JobLevel          
 ,[PerMintuteRate]=@PerMintuteRate          
 ,[CalendlyLink] =@CalendlyLink          
 ,[TotalBudget]=@TotalBudget          
 ,[IsBiometricEnabled]=@IsBiometricEnabled          
 ,[PriorityLevel]=@PriorityLevel          
 ,[CompanyDetails]=@CompanyDetails      
 ,[BussinessType]=@Type       
 ,[Heuristicsscore]=@Heuristicsscore      
 ,[PrimarySkillInvite]=@PrimarySkillInvite      
 ,[BillingOwner]=@BillingOwner  
 ,[AssessmentGroupId]=@AssessmentGroupId  
 ,[MeetingPlatform]=@MeetingPlatform  
 --,[IsQCRequired] = @IsQCRequired  
 ,[IsJoinabilitySelectabilityRequired] = @IsJoinabilitySelectabilityRequired  
 ,[IsGeneralSkillRequired] = @IsGeneralSkillRequired  
 ,[IsAadhaarVerificationRequired]=@IsAadhaarVerificationRequired  
 ,[IsCandidateavailability]=@IsCandidateavailability  
 ,[IsMeetingAutoCancelled]=@IsMeetingAutoCancelled  
 ,[IsMeetingNotificationSend]=@IsMeetingNotificationSend  
 ,[IsLocked]=@IsLocked  
 ,[InterviewDuration]=@InterviewDuration  
 ,[IsFixedInterviewduration]=@IsFixedInterviewduration  
 Where id =@id          
      
   
 if(@isCalendlyChanged =1)  
 begin  
  if not exists(select id from TransCalendlyLinkAddedUpdated where IRFId =@id)  
  begin  
   insert into TransCalendlyLinkAddedUpdated values (@id, GETDATE())  
  end  
 end  
  
 --joinability  
 delete from TransJoinability where IRFId=@id  
  
 INSERT INTO [dbo].[TransJoinability](    
 [IRFId] ,    
 [LevelId],    
 [Type],    
 [Value],    
 [Weightage])    
    
 SELECT * FROM @JoinabilityBulkData   
  
 --selectability  
 delete from TransSelectability where IRFId=@id  
  
 INSERT INTO [dbo].[TransSelectability](    
 [IRFId] ,    
 [LevelId],    
 [Type],    
 [Value],    
 [Weightage])    
    
 SELECT * FROM @SelectabilityBulkData    
  
 --general skills  
 delete from TransSkillGeneral where IRFId=@id   
  
 INSERT INTO [dbo].[TransSkillGeneral](    
 [IRFId] ,    
 [LevelId],    
 [Type],    
 [Value],    
 [Weightage])    
    
 SELECT * FROM @SkillGeneralBulkData  
  
 --tech skills  
 delete from TransTechSkill where IRFId=@id  
  
 INSERT INTO [dbo].[TransTechSkill](      
 [IRFId] ,      
 [LevelId],      
 [SkillId],      
 [Weightage],[Concepts],[Natureofquestion],[Timelimit])      
      
 SELECT * FROM @TechSkillBulkData   
  
 --interviewers  
 --Start :Delete interviewers
 Declare @Assessmentid int  
Select @Assessmentid=AssessmentGroupid From IRF where id=@id 
Declare @temptable Table(Userid int)  
Insert into @temptable Select Userid from InterviewerProfile cross apply string_split(AssessmentGroupid,',') where value=@Assessmentid  
 --delete from TransInterviewer where IRFId=@id 
 Delete b FROM TransInterviewer b LEFT JOIN @temptable f ON f.Userid = b.Userid WHERE f.Userid IS NOT NULL and b.IRFId=@id
 --END: Delete interviewers
  
 INSERT INTO [dbo].[TransInterviewer](    
 [IRFId] ,    
 [UserId],    
 [PerSuccessRate],   
 [IsActive],  
 [IsAvailable],  
 [InterviewerId])    
      
 SELECT ibd.*, InterviewerProfile.id as InterviewerId FROM @InterviewerBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId    
    
    --emergency interviewers  
    delete from TransEmergencyInterviewer where IRFId=@id 
  
 INSERT INTO [dbo].[TransEmergencyInterviewer](    
 [IRFId] ,    
 [UserId],    
 [PerSuccessRate],   
 [IsActive],  
 [InterviewerId])    
     
   SELECT eibd.*, InterviewerProfile.id as InterviewerId FROM @EmergencyInterviewerBulkData eibd left join InterviewerProfile on eibd.UserId = InterviewerProfile.UserId    
    
 --client users  
 delete from TransClientUser where IRFId=@id   
  
 INSERT INTO [dbo].[TransClientUser](    
 [IRFId] ,    
 [ClientUserId],    
 [ClientRoleId])    
     
    
 SELECT cubd.*, ClientUser.RoleId as ClientRoleId FROM @ClientUserBulkData cubd left join ClientUser on cubd.ClientUserId= ClientUser.UserId    
   
    --quality controllers  
 -- delete from TransQualityController where IRFId=@id          
  
 --INSERT INTO [dbo].[TransQualityController](    
 --[IRFId] ,    
 --[UserId],    
 --[PerSuccessRate],  
 --[IsActive],  
 --[InterviewerId])    
       
 --SELECT qcbd.*, InterviewerProfile.id as InterviewerId FROM @QCBulkData qcbd left join InterviewerProfile on qcbd.UserId = InterviewerProfile.UserId    
   
    --HRs  
 delete from TransHR where IRFId=@id   
   
 INSERT INTO [dbo].[TransHR](    
 [IRFId] ,    
 [HRId])    
     
 SELECT hrbd.IRFId, HR.id as HRId FROM @HRBulkData hrbd left join HR on hrbd.UserId = HR.UserId    
   
 --IF EXISTS(Select 1 from IRFRecommendations where id = @idRecommendation)       
 --BEGIN  
 -- update IRFRecommendations set   
 -- [Status] = @RecommendationStatus, CategoryId = @RecommendationCategoryId, CompetencyLevelId = @RecommendationCompetencyLevelId,   
 -- SeniorityLevelId = @RecommendationSeniorityLevelId,  
 -- CountryType = @RecommendationCountryType, ModifiedOn = @RecommendationModifiedOn where id = @idRecommendation  
       
 --END     
 set @isSMEChanged = 0  
  
 if(@SkillChannelSME is not null and @SkillChannelSME > 0)  
 Begin  
  if not exists(select top 1 id from SkillChannelSMEs where IRFId = @id)  
  Begin  
   insert into SkillChannelSMEs (IRFId, InterviewerProfileId, AddedBy, PerformedAt)   
   values (@id, @SkillChannelSME, @ModifiedBy, @timeNow)  
  
   set @isSMEChanged = 1  
  
  End  
  Else  
  Begin  
  
   update SkillChannelSMEs set IsDeleted = 1, DeletedBy = @ModifiedBy, @isSMEChanged = 1 where IRFId = @id and InterviewerProfileId != @SkillChannelSME  
  
  
   if exists (select top 1 id from SkillChannelSMEs where irfid = @id and InterviewerProfileId = @SkillChannelSME and (IsDeleted is null or IsDeleted = 0))  
   Begin  
    set @isSMEChanged = 0  
  
   End  
   else  
   Begin  
  
    insert into SkillChannelSMEs (IRFId, InterviewerProfileId, AddedBy, PerformedAt)   
    values (@id, @SkillChannelSME, @ModifiedBy, @timeNow)  
  
   End  
  
  End  
    
   
   
 End  
  
  commit tran    
 end try    
 begin catch    
    
  rollback transaction    
    
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),    
   @Severity int = ERROR_SEVERITY(),    
   @State smallint = ERROR_STATE()    
     
  RAISERROR (@Message, @Severity, @State)    
     
 end catch    
    
      
END       
   

GO


