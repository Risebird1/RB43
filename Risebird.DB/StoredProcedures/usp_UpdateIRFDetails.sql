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
 --@SkillChannelSME int = null,  
 @ModifiedBy varchar(100) = null,  
 @timeNow datetime,  
 --@isSMEChanged bit output,  
 @IsLocked bit,  
 @IsAIInterview bit,
 @showChatBox bit,
 @generateTranscriptSummary bit,
 @AIInterviewType int,
 --@InterviewDuration int,  
 --@IsFixedInterviewduration bit,  
 @JoinabilityBulkData AS [dbo].[TransJoinability_TableType] READONLY ,  
 @SelectabilityBulkData AS [dbo].[TransSelectability_TableType] READONLY,  
 @SkillGeneralBulkData AS [dbo].[TransSkillGeneral_TableType] READONLY,  
 @TechSkillBulkData AS [dbo].[TransTechSkill_TableTypeV5] READONLY,--TransTechSkill2_TableType, TransTechSkill_TableTypeV3, TransTechSkill_TableTypeV4  
 @InterviewerBulkData AS [dbo].[TransInterviewer_TableTypeV3] READONLY,--[dbo].[TransInterviewer_TableType]  
 @InterviewerSurchargeBulkData AS [dbo].[TransInterviewerSurcharge_TableTypeV3] READONLY, --[dbo].[TransInterviewerSurcharge_TableType]  
 @EmergencyInterviewerBulkData AS [dbo].[TransEmergencyInterviewer_TableType] READONLY,  
 @ClientUserBulkData AS [dbo].[TransClientUser_TableType] READONLY,  
 --@QCBulkData AS [dbo].[TransQualityController_TableType] READONLY,  
 @HRBulkData AS [dbo].[TransHR_TableType] READONLY,  
 @SMEBulkData AS [dbo].[CommonId_TableType] READONLY,  
 @IsNewSkillChannel bit = null,  
 @TechRubricType varchar(20) = null,  
 @OpenAIAssistantId varchar(60) = null,
 @OpenAIPromptId varchar(100) = null,
 @OpenAIEvaluatorPromptId varchar(100) = null,
 @IsVideoRequired bit,
 @IsTechQC bit,
 @IsProctoring bit = null
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
 ,[IsAIInterview]=@IsAIInterview
 ,[showChatBox]=@showChatBox
 ,[generateTranscriptSummary]=@generateTranscriptSummary
 ,[AIInterviewType]=@AIInterviewType
 ,[OpenAIAssistantId] = @OpenAIAssistantId
 ,[OpenAIPromptId] = @OpenAIPromptId
 ,[OpenAIEvaluatorPromptId] = @OpenAIEvaluatorPromptId
 ,[IsTechQC]=@IsTechQC
 ,[IsProctoring]=ISNULL(@IsProctoring,[IsProctoring])
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

 -- TransTechSkill is the editable score-rubric TEMPLATE for the channel. Completed interview
 -- evaluations are snapshotted separately into TechSkillReport (which keeps its own SkillName,
 -- SkillSet, WeightageRequired/WeightageActual and level columns), so refreshing the rubric here
 -- never rewrites historical reports. A previous revision wrapped this refresh in an
 -- "only if no TechSkillReport exists yet" guard; that silently discarded rubric edits on any
 -- channel that already had a filled report while the overall save still committed (the user saw a
 -- "saved" confirmation but the rubric never changed). Always refresh the rubric, matching the DM
 -- save path (usp_TechSkillBulkInsert), which has no such guard.
 delete from TransTechSkill where IRFId=@id    
   
	INSERT INTO [dbo].[TransTechSkill](        
	[IRFId] ,        
	[LevelId],        
	[SkillId],        
	[Weightage],[Concepts],[Natureofquestion],[Timelimit],[isEliminationskill],[SkillSet], [SkillName])        
        
	SELECT * FROM @TechSkillBulkData   
  
 --interviewers  
  
 --Start :Upsert interviewers
 -- NOTE: This section used to DELETE + re-INSERT all MAG interviewers on every save. That approach
 -- (a) failed once TransInterviewerAuditLog started referencing TransInterviewer.id via
 --     FK_TransInterviewerAuditLog_TransInterviewer (the delete was blocked and the whole save
 --     rolled back), and
 -- (b) discarded audit history + any in-place edits to existing interviewers.
 -- It is now an upsert that NEVER hard-deletes a TransInterviewer row: existing rows are updated in
 -- place (id preserved, audit log intact), genuinely-new interviewers are inserted, and interviewers
 -- removed from the panel are SOFT-removed (IsActive = 0). We must never physically delete a row,
 -- because interview history and several open-interview / Auto-QC / payout / reporting queries
 -- INNER JOIN TransInterviewer; deleting a row would make that interviewer's past interviews for this
 -- channel disappear from those fetches.
 Declare @Assessmentid int    
  Select @Assessmentid=AssessmentGroupid From IRF where id=@id   
  Declare @temptable Table(Userid int)    
  Insert into @temptable Select Userid from InterviewerProfile cross apply string_split(AssessmentGroupid,',') where value=@Assessmentid    

 -- Interviewers submitted with this save (from whichever bulk TVP is populated for this channel type)
 Declare @incomingInterviewers Table(UserId int)
 if(@IsNewSkillChannel is not null and @IsNewSkillChannel = 1)
  Insert into @incomingInterviewers(UserId) Select UserId from @InterviewerSurchargeBulkData
 else
  Insert into @incomingInterviewers(UserId) Select UserId from @InterviewerBulkData

 -- Interviewers currently attached to this IRF but NOT resubmitted => removed from the panel in the
 -- UI. Soft-remove them (deactivate) instead of deleting. Guarded so an empty/absent bulk TVP (e.g.
 -- when the app skips building it) can never mass-deactivate an existing panel.
 IF EXISTS (SELECT 1 FROM @incomingInterviewers)
 BEGIN
  UPDATE b SET b.IsActive = 0, b.IsAvailable = 0, b.ModifiedBy = @ModifiedBy, b.ModifiedAt = @timeNow
  FROM TransInterviewer b
  WHERE b.IRFId=@id
    AND ISNULL(b.IsActive, 0) = 1
    AND NOT EXISTS (SELECT 1 FROM @incomingInterviewers i WHERE i.UserId = b.UserId)
 END
 --END: Soft-remove interviewers  
  
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
      
 SELECT distinct ibd.IRFId, ibd.UserId, ibd.PerSuccessRate, ibd.Surcharge,
 -- An interviewer may only be ACTIVE on a channel while they belong to the channel's MAG. If the
 -- interviewer is not (or no longer) in the MAG, force IsActive = 0 regardless of what was submitted.
 CASE WHEN EXISTS (SELECT 1 FROM @temptable t WHERE t.Userid = ibd.UserId) THEN ibd.IsActive ELSE 0 END,
 ibd.IsAvailable, InterviewerProfile.id, ibd.[Source],  
 @ModifiedBy, NULL as IsSurchargeApprove, ibd.IsCampaignActivity, ibd.CampaignId, ibd.OpenAIScreeningResponse, ibd.AIVerdict  
 FROM @InterviewerSurchargeBulkData ibd join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId    
 WHERE   
 NOT EXISTS (  
  SELECT 1  
  FROM [dbo].[TransInterviewer] ti  
  WHERE ti.UserId = ibd.UserId AND ti.IRFId = ibd.IRFId  
 );  

 -- Update interviewers that already exist for this IRF (preserves id -> keeps audit log FK valid).
 -- IsActive is forced to 0 when the interviewer no longer belongs to the channel's MAG, so an
 -- interviewer removed from the MAG cannot be (re)activated on this channel.
 UPDATE ti SET
    ti.PerSuccessRate = ibd.PerSuccessRate,
    ti.Surcharge = ibd.Surcharge,
    ti.IsActive = CASE WHEN EXISTS (SELECT 1 FROM @temptable t WHERE t.Userid = ti.UserId) THEN ibd.IsActive ELSE 0 END,
    ti.IsAvailable = ibd.IsAvailable,
    ti.[Source] = ibd.[Source],
    ti.ModifiedBy = @ModifiedBy,
    ti.IsCampaignActivity = ibd.IsCampaignActivity,
    ti.CampaignId = ibd.CampaignId,
    ti.OpenAIScreeningResponse = ibd.OpenAIScreeningResponse,
    ti.AIVerdict = ibd.AIVerdict
 FROM [dbo].[TransInterviewer] ti
 JOIN @InterviewerSurchargeBulkData ibd ON ibd.UserId = ti.UserId AND ibd.IRFId = ti.IRFId;
  
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
      
 SELECT distinct ibd.IRFId, ibd.UserId, ibd.PerSuccessRate,
 -- An interviewer may only be ACTIVE on a channel while they belong to the channel's MAG. If the
 -- interviewer is not (or no longer) in the MAG, force IsActive = 0 regardless of what was submitted.
 CASE WHEN EXISTS (SELECT 1 FROM @temptable t WHERE t.Userid = ibd.UserId) THEN ibd.IsActive ELSE 0 END,
 ibd.IsAvailable, InterviewerProfile.id as InterviewerId,  
 ibd.[Source], @ModifiedBy, ibd.IsCampaignActivity, ibd.CampaignId, ibd.OpenAIScreeningResponse, ibd.AIVerdict, ibd.IsSurchargeApprove  
 FROM @InterviewerBulkData ibd join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId    
 WHERE   
 NOT EXISTS (  
  SELECT 1  
  FROM [dbo].[TransInterviewer] ti  
  WHERE ti.UserId = ibd.UserId AND ti.IRFId = ibd.IRFId  
 );  

 -- Update interviewers that already exist for this IRF (preserves id -> keeps audit log FK valid).
 -- IsActive is forced to 0 when the interviewer no longer belongs to the channel's MAG, so an
 -- interviewer removed from the MAG cannot be (re)activated on this channel.
 UPDATE ti SET
    ti.PerSuccessRate = ibd.PerSuccessRate,
    ti.IsActive = CASE WHEN EXISTS (SELECT 1 FROM @temptable t WHERE t.Userid = ti.UserId) THEN ibd.IsActive ELSE 0 END,
    ti.IsAvailable = ibd.IsAvailable,
    ti.[Source] = ibd.[Source],
    ti.ModifiedBy = @ModifiedBy,
    ti.IsCampaignActivity = ibd.IsCampaignActivity,
    ti.CampaignId = ibd.CampaignId,
    ti.OpenAIScreeningResponse = ibd.OpenAIScreeningResponse,
    ti.AIVerdict = ibd.AIVerdict,
    ti.IsSurchargeApprove = ibd.IsSurchargeApprove
 FROM [dbo].[TransInterviewer] ti
 JOIN @InterviewerBulkData ibd ON ibd.UserId = ti.UserId AND ibd.IRFId = ti.IRFId;
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
   
     
 --set @isSMEChanged = 0  
 delete from SkillChannelSMEs where IRFId =@id  
  
 insert into SkillChannelSMEs (IRFId, InterviewerProfileId, AddedBy, PerformedAt)   
 select @id, id, @ModifiedBy, @timeNow from @SMEBulkData  
 --if(@SkillChannelSME is not null and @SkillChannelSME > 0)  
 --Begin  
 -- if not exists(select top 1 id from SkillChannelSMEs where IRFId = @id)  
 -- Begin  
 --  insert into SkillChannelSMEs (IRFId, InterviewerProfileId, AddedBy, PerformedAt)   
 --  values (@id, @SkillChannelSME, @ModifiedBy, @timeNow)  
  
 --  set @isSMEChanged = 1  
  
 -- End  
 -- Else  
 -- Begin  
  
 --  update SkillChannelSMEs set IsDeleted = 1, DeletedBy = @ModifiedBy, @isSMEChanged = 1 where IRFId = @id and InterviewerProfileId != @SkillChannelSME  
  
  
 --  if exists (select top 1 id from SkillChannelSMEs where irfid = @id and InterviewerProfileId = @SkillChannelSME and (IsDeleted is null or IsDeleted = 0))  
 --  Begin  
 --   set @isSMEChanged = 0  
  
 --  End  
 --  else  
 --  Begin  
  
 --   insert into SkillChannelSMEs (IRFId, InterviewerProfileId, AddedBy, PerformedAt)   
 --   values (@id, @SkillChannelSME, @ModifiedBy, @timeNow)  
  
 --  End  
  
 -- End  
    
   
   
 --End  
  
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


