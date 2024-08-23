IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateIRFDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_UpdateIRFDetails]    
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
	@SkillSetsevaluate int,
	@JDTitleTwo varchar(150),
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
	--@InterviewDuration int,
	--@IsFixedInterviewduration bit,
	@JoinabilityBulkData AS [dbo].[TransJoinability_TableType] READONLY ,
	@SelectabilityBulkData AS [dbo].[TransSelectability_TableType] READONLY,
	@SkillGeneralBulkData AS [dbo].[TransSkillGeneral_TableType] READONLY,
	@TechSkillBulkData AS [dbo].[TransTechSkill_TableTypeV3] READONLY,--TransTechSkill2_TableType
	@InterviewerBulkData AS [dbo].[TransInterviewer_TableTypeV3] READONLY,--[dbo].[TransInterviewer_TableType]
	@InterviewerSurchargeBulkData AS [dbo].[TransInterviewerSurcharge_TableTypeV3] READONLY, --[dbo].[TransInterviewerSurcharge_TableType]
	@EmergencyInterviewerBulkData AS [dbo].[TransEmergencyInterviewer_TableType] READONLY,
	@ClientUserBulkData AS [dbo].[TransClientUser_TableType] READONLY,
	--@QCBulkData AS [dbo].[TransQualityController_TableType] READONLY,
	@HRBulkData AS [dbo].[TransHR_TableType] READONLY,
	@IsNewSkillChannel bit = null,
	@TechRubricType varchar(20) = null,
	@IsVideoRequired bit
	--@FixedAmount int = null
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
  
  
 begin try  
  begin tran  
   
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
	,[JDTitleTwo]=@JDTitleTwo
	,[IsVideoRequired]=@IsVideoRequired
	,[TechRubricType] =@TechRubricType
	--,[InterviewDuration]=@InterviewDuration
	--,[IsFixedInterviewduration]=@IsFixedInterviewduration
	--,[FixedAmount] = @FixedAmount
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
	[Weightage],[Concepts],[Natureofquestion],[Timelimit],[isEliminationskill],[SkillSet])    
    
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

 if(@IsNewSkillChannel is not null and @IsNewSkillChannel = 1)
 Begin
	INSERT INTO [dbo].[TransInterviewer](  
	[IRFId] ,  
	[UserId],  
	[PerSuccessRate], 
	[Surcharge],
	[IsActive],
	[IsAvailable],
	[InterviewerId],
	[Source],
	[ModifiedBy],
	[IsSurchargeApprove],
	[IsCampaignActivity],
	[CampaignId],
	[OpenAIScreeningResponse],
	[AIVerdict])  
    
	SELECT ibd.IRFId, ibd.UserId, ibd.PerSuccessRate, ibd.Surcharge, ibd.IsActive, ibd.IsAvailable, InterviewerProfile.id, ibd.[Source],
	@ModifiedBy, NULL as IsSurchargeApprove, ibd.IsCampaignActivity, ibd.CampaignId, ibd.OpenAIScreeningResponse, ibd.AIVerdict
	FROM @InterviewerSurchargeBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId  
	Insert into SurchargeActivityTracker(InterviewId,Surcharge,SurchargeComment,RequestBy,RequestAt,RequestType,IRFId,UserId)
	SELECT 
    0 AS InterviewId,
    ibd.Surcharge,
    NULL AS SurchargeComment,
    @ModifiedBy,
    @timeNow,
    'Raised' AS RequestType,
    ibd.IRFId AS IRFId,
	ibd.UserId
FROM 
    @InterviewerSurchargeBulkData ibd where ISNULL(ibd.Surcharge,0)<>0;

					--emergency interviewers
 End
 else
 Begin
	INSERT INTO [dbo].[TransInterviewer](  
	[IRFId] ,  
	[UserId],  
	[PerSuccessRate], 
	[IsActive],
	[IsAvailable],
	[InterviewerId],
	[Source],
	[ModifiedBy],
	[IsCampaignActivity],
	[CampaignId],
	[OpenAIScreeningResponse],
	[AIVerdict],
	[IsSurchargeApprove])  
    
	SELECT ibd.IRFId, ibd.UserId, ibd.PerSuccessRate, ibd.IsActive, ibd.IsAvailable, InterviewerProfile.id as InterviewerId,
	ibd.[Source], @ModifiedBy, ibd.IsCampaignActivity, ibd.CampaignId, ibd.OpenAIScreeningResponse, ibd.AIVerdict, ibd.IsSurchargeApprove
	FROM @InterviewerBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId  
  
 End
	
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
 
   
    --HRs
	delete from TransHR where IRFId=@id 
	
	INSERT INTO [dbo].[TransHR](  
	[IRFId] ,  
	[HRId])  
   
	SELECT hrbd.IRFId, HR.id as HRId FROM @HRBulkData hrbd left join HR on hrbd.UserId = HR.UserId  
	
	  
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

	Declare @imid TABLE  
(  
   Id int 
) 
Insert into @imid 
					select distinct im.id from TransInterviewer ti
join UserList ul on ti.UserId=ul.id
join IRF on ti.IRFId=IRF.id
join InterviewMeetings im on ul.EmailId=im.interviewer and IRF.IRFId=im.IRFId
where ti.IsActive=0 and ti.IRFId=@id and interviewDate>@timeNow
					IF EXISTS( select * from @imid)
					begin 
					update InterviewMeetings set interviewer=null where id in(select id from @imid)
					end
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


