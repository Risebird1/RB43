/****** Object:  StoredProcedure [dbo].[usp_AddIRFDetails]    Script Date: 26-09-2019 5.05.16 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_AddIRFDetails
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AddIRFDetails]              
@IRFId nvarchar(200),              
--@clientName varchar(200),              
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
@CreatedOn datetime,              
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
--@IsQCRequired bit,  
@IsJoinabilitySelectabilityRequired bit,  
@IsGeneralSkillRequired bit,  
@IsAadhaarVerificationRequired bit,  
@IsCandidateavailability bit,  
@IsMeetingAutoCancelled bit,  
@IsMeetingNotificationSend bit,  
@SkillChannelSME int = null,  
@CreatedBy varchar(100) = null,  
--@InterviewDuration int,  
--@IsFixedInterviewduration bit,  
@JoinabilityBulkData AS [dbo].[TransJoinability_TableType] READONLY ,  
@SelectabilityBulkData AS [dbo].[TransSelectability_TableType] READONLY,  
@SkillGeneralBulkData AS [dbo].[TransSkillGeneral_TableType] READONLY,  
@TechSkillBulkData AS [dbo].[TransTechSkill_TableTypeV5] READONLY,--TransTechSkill2_TableType, TransTechSkill_TableTypeV3, TransTechSkill_TableTypeV4  
@InterviewerBulkData AS [dbo].[TransInterviewerSurcharge_TableType] READONLY,  
@EmergencyInterviewerBulkData AS [dbo].[TransEmergencyInterviewer_TableType] READONLY,  
@ClientUserBulkData AS [dbo].[TransClientUser_TableType] READONLY,  
--@QCBulkData AS [dbo].[TransQualityController_TableType] READONLY,  
@HRBulkData AS [dbo].[TransHR_TableType] READONLY,  
--@FixedAmount int = null,  
@IsNewSkillChannel bit = null,  
@FlatRate int,  
@IsVideoRequired bit, 
@IsTechQC bit,
@TechRubricType varchar(20) = null,  
@IsAIInterview bit,
@AIInterviewType int,
@OpenAIPromptId varchar(100) = null,
@OpenAIEvaluatorPromptId varchar(100) = null,
@MeetingPlatform varchar(25),
@showChatBox bit = null,  
@generateTranscriptSummary bit = null,
@IsProctoring bit = null,
-- MCQ assessment section (screen.risebird.io). Defaulted so an older caller still inserts.
@HasMcqAssessmentSection bit = null,
@McqAssessmentSections varchar(500) = null,
@McqAssessmentMinutes int = null,
-- Advanced Resume Screening opt-in, and the ideal candidate profile it reads (Add had no profile).
@IsAdvancedResumeScreening bit = null,
@IdealCandidateProfile varchar(max) = null,
-- Report level display and candidate reschedule limits. Edit IRF in clone mode saves through this
-- proc, so without these a cloned channel would silently lose what the form showed. NULL = today.
@LevelDisplayMode int = null,
@MaxCandidateReschedules int = null,
@InterviewCompleteByDays int = null,
@idIrf int output
              
AS              
Begin  
 Begin Try  
  Begin tran  
  
   if not exists (select 1 id from IRF where IRFId =@IRFId)       
    Begin      
     INSERT INTO [dbo].[IRF]              
       ([IRFId]              
       --,[clientName]              
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
       --,[IsQCRequired]  
       ,[IsJoinabilitySelectabilityRequired]  
       ,[IsGeneralSkillRequired]  
       ,[IsAadhaarVerificationRequired]  
       ,[IsCandidateavailability]  
       ,[IsMeetingAutoCancelled]  
       ,[IsMeetingNotificationSend]  
       ,[IsNewSkillChannel]  
       ,[FlatRate]  
       ,[SkillSetsevaluate]  
       ,[JDTitleTwo]  
       ,[IsVideoRequired]  
       ,[TechRubricType]
	   ,[IsAIInterview]
	   ,[AIInterviewType]
	   ,[OpenAIPromptId]
	   ,[OpenAIEvaluatorPromptId]
	   ,[MeetingPlatform]
	   ,[IsTechQC]
	   ,[IsProctoring]
	   ,[HasMcqAssessmentSection]
	   ,[McqAssessmentSections]
	   ,[McqAssessmentMinutes]
	   ,[IsAdvancedResumeScreening]
	   ,[IdealCandidateProfile]
	   -- @generateTranscriptSummary was accepted but never stored, so a clone lost the Q&A log flag.
	   ,[generateTranscriptSummary]
	   ,[LevelDisplayMode]
	   ,[MaxCandidateReschedules]
	   ,[InterviewCompleteByDays])
       --,[InterviewDuration]  
       --,[IsFixedInterviewduration]  
       --,[FixedAmount])              
        VALUES              
         (@IRFId,              
       --@clientName,              
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
       --@IsQCRequired,  
       @IsJoinabilitySelectabilityRequired,  
       @IsGeneralSkillRequired,  
       @IsAadhaarVerificationRequired,  
       @IsCandidateavailability,  
       @IsMeetingAutoCancelled,  
       @IsMeetingNotificationSend,  
       @IsNewSkillChannel,  
       @FlatRate,  
       @SkillSetsevaluate,  
       @JDTitleTwo,  
       @IsVideoRequired,  
       @TechRubricType,
	   @IsAIInterview,
	   @AIInterviewType,
	   @OpenAIPromptId,
	   @OpenAIEvaluatorPromptId,
	   @MeetingPlatform,
	   @IsTechQC,
	   @IsProctoring,
	   ISNULL(@HasMcqAssessmentSection, 0),
	   NULLIF(LTRIM(RTRIM(@McqAssessmentSections)), ''),
	   NULLIF(@McqAssessmentMinutes, 0),
	   ISNULL(@IsAdvancedResumeScreening, 0),
	   NULLIF(LTRIM(RTRIM(@IdealCandidateProfile)), ''),
	   NULLIF(@generateTranscriptSummary, 0),
	   NULLIF(@LevelDisplayMode, 0),
	   @MaxCandidateReschedules,
	   NULLIF(@InterviewCompleteByDays, 0))
       --@InterviewDuration,  
       --@IsFixedInterviewduration,  
       --@FixedAmount)        
         
     select @idIrf = SCOPE_IDENTITY()    
      
     INSERT INTO [dbo].[TransJoinability](    
     [IRFId] ,    
     [LevelId],    
     [Type],    
     [Value],    
     [Weightage])    
    
     SELECT @idIrf, jbd.LevelId, jbd.[Type], jbd.[Value], jbd.Weightage FROM @JoinabilityBulkData jbd  
  
     --selectability  
  
     INSERT INTO [dbo].[TransSelectability](    
     [IRFId] ,    
     [LevelId],    
     [Type],    
     [Value],    
     [Weightage])    
    
     SELECT @idIrf, sbd.LevelId, sbd.[Type], sbd.[Value], sbd.Weightage FROM @SelectabilityBulkData sbd  
  
  
     --general skills  
  
     INSERT INTO [dbo].[TransSkillGeneral](    
     [IRFId] ,    
     [LevelId],    
     [Type],    
     [Value],    
     [Weightage])    
    
     SELECT @idIrf, sgbd.LevelId, sgbd.[Type], sgbd.[Value], sgbd.Weightage FROM @SkillGeneralBulkData sgbd  
     --tech skills  
  
     INSERT INTO [dbo].[TransTechSkill](      
     [IRFId] ,      
     [LevelId],      
     [SkillId],      
     [Weightage],[Concepts],[Natureofquestion],[Timelimit],[isEliminationskill],[SkillSet], [SkillName])      
      
     SELECT @idIrf, tsbd.LevelId, tsbd.SkillId, tsbd.Weightage, tsbd.Concepts, tsbd.Natureofquestion,   
     tsbd.Timelimit,tsbd.isEliminationskill,tsbd.SkillSet, tsbd.SkillName FROM @TechSkillBulkData tsbd  
       
       
     --interviewers  
  
     INSERT INTO [dbo].[TransInterviewer](    
     [IRFId] ,    
     [UserId],    
     [PerSuccessRate],   
     [Surcharge],  
     [IsActive],  
     [IsAvailable],  
     [InterviewerId],  
     [IsSurchargeApprove])    
      
     SELECT distinct @idIrf, ibd.UserId, ibd.PerSuccessRate, ibd.Surcharge, ibd.IsActive, ibd.IsAvailable,   
     InterviewerProfile.id as InterviewerId, NULL as IsSurchargeApprove FROM @InterviewerBulkData ibd   
     join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId   
     WHERE   
     NOT EXISTS (  
      SELECT 1  
      FROM [dbo].[TransInterviewer] ti  
      WHERE ti.UserId = ibd.UserId AND ti.IRFId = ibd.IRFId  
     );  
  
     Insert into SurchargeActivityTracker(InterviewId,Surcharge,SurchargeComment,RequestBy,RequestAt,RequestType,IRFId,UserId)  
 SELECT   
    0 AS InterviewId,   
    ibd.Surcharge,  
    NULL AS SurchargeComment,    
    @CreatedBy,  
    @CreatedOn,   
    'Raised' AS RequestType,  
    ibd.IRFId AS IRFId,  
 ibd.UserId  
FROM   
  
    @InterviewerBulkData ibd where ISNULL(ibd.Surcharge,0)<>0;  
     INSERT INTO [dbo].[TransEmergencyInterviewer](    
     [IRFId] ,    
     [UserId],    
     [PerSuccessRate],   
     [IsActive],  
     [InterviewerId])    
     
         
     SELECT @idIrf, eibd.UserId, eibd.PerSuccessRate, eibd.IsActive, InterviewerProfile.id as InterviewerId FROM @EmergencyInterviewerBulkData eibd left join InterviewerProfile on eibd.UserId = InterviewerProfile.UserId    
  
     --client users  
  
     INSERT INTO [dbo].[TransClientUser](    
     [IRFId] ,    
     [ClientUserId],    
     [ClientRoleId])    
     
     SELECT @idIrf, cubd.ClientUserId, ClientUser.RoleId as ClientRoleId FROM @ClientUserBulkData cubd   
     left join ClientUser on cubd.ClientUserId= ClientUser.UserId   
   
       
   
     INSERT INTO [dbo].[TransHR](    
     [IRFId] ,    
     [HRId])    
     
     SELECT @idIrf, HR.id as HRId FROM @HRBulkData hrbd left join HR on hrbd.UserId = HR.UserId  
  
     if(@SkillChannelSME is not null and @SkillChannelSME != 0)  
     Begin  
      insert into SkillChannelSMEs (IRFId, InterviewerProfileId, AddedBy, PerformedAt)   
      values (@idIrf, @SkillChannelSME, Isnull(@CreatedBy, ''), @CreatedOn)  
     End  
  
     if exists(select top 1 id from MasterAssessmentGroup where AssessmentGroupId = @AssessmentGroupId)  
     Begin  
      if not exists(select top 1 csc.id from ClonableSkillChannel csc join IRF on irf.id= csc.CreatedFromIRFId join AssessmentGroup ag on ag.id = irf.AssessmentGroupId where ag.id = @AssessmentGroupId and (csc.IsDeleted is null or csc.IsDeleted =0))  
      Begin  
       declare @idClonableIrf int, @templateName varchar(100)  
       set @idClonableIrf = 0  
       set @templateName = (select AssessmentGroup from AssessmentGroup where id = @AssessmentGroupId) + ' Template'  
    
       if not exists (select 1 from ClonableSkillChannel where [Name] = @templateName)         
       Begin       
    
       INSERT INTO [dbo].[ClonableSkillChannel]                
        ([Name]                
        ,[CreatedOn]                
        ,[CreatedFromIRFId]    
        ,[CreatedBy]     
        ,[MeetingPlatform]  
        ,[IsAadhaarVerificationRequired])    
       VALUES                
       (@templateName,                
        @CreatedOn,                
        @idIrf,     
        @CreatedBy,    
        'Zoho',  
        @IsAadhaarVerificationRequired)          
           
       select @idClonableIrf = SCOPE_IDENTITY()   
  
       if(@idClonableIrf != 0)  
       Begin  
  
  
        delete from TransClonableIRFTechSkill where ClonableIRFId= @idClonableIrf    
    
         INSERT INTO [dbo].[TransClonableIRFTechSkill](        
         [ClonableIRFId] ,        
         [LevelId],        
         [SkillId],        
         [Weightage],[Concepts],[Natureofquestion],[Timelimit],[isEliminationskill], [SkillName])        
        
         SELECT @idClonableIrf, tsbd.LevelId, tsbd.[SkillId], tsbd.[Weightage], tsbd.[Concepts], tsbd.[Natureofquestion], tsbd.[Timelimit], tsbd.isEliminationskill, tsbd.SkillName  FROM @TechSkillBulkData tsbd  
  
  
          delete from TransClonableIRFInterviewer where ClonableIRFId= @idClonableIrf    
    
          
        -- The MAG's own group plus its secondary groups, resolved once. Filtering the split with
        -- "value = @AssessmentGroupId OR value IN (subquery)" took ~42s on prod - past the app's
        -- command timeout - so the first channel on every new MAG failed and rolled back.
        declare @magAssessmentGroups table (AGId varchar(20) primary key)
        insert into @magAssessmentGroups (AGId)
        select cast(@AssessmentGroupId as varchar(20))
        union
        select cast(sag.AssessmentGroupId as varchar(20)) from SecondaryAssessmentGroups sag
        join MasterAssessmentGroup mag on mag.id = sag.MAGId
        where mag.AssessmentGroupId = @AssessmentGroupId and sag.AssessmentGroupId is not null

        ;With CTE AS( Select id, AssessmentGroupId, UserId, value from InterviewerProfile cross Apply string_split(AssessmentGroupId,','))

        INSERT INTO [dbo].[TransClonableIRFInterviewer](
                [ClonableIRFId] ,
                [UserId],
                [PerSuccessRate],
                [IsActive],
                --[IsAvailable],
                [InterviewerId])
        Select Distinct @idClonableIrf, CTE.UserId, @FlatRate, 1, CTE.id
        From CTE join @magAssessmentGroups mags on CTE.value = mags.AGId
        join Userlist ul on CTE.UserId=ul.id
        where ul.IsActive=1
        group by CTE.UserId,CTE.AssessmentGroupId, CTE.id
  
  
  
        --questions   
        declare @createdByUserId int = (select id from UserList where EmailId = @CreatedBy)  
         
        delete from ClonableTemplateQuestions where ClonableIRFId= @idClonableIrf    
    
         INSERT INTO [dbo].[ClonableTemplateQuestions](      
         [ClonableIRFId] ,      
         [UserId],      
         [Skill],      
         [Question], [SkillName])      
      
        Select distinct @idClonableIrf, @createdByUserId, 0, ts.Comment as Question, SkillName From TechSkillComments ts   
        join IRF on IRF.id=ts.IRFId --join Skill sk on sk.id=ts.Skill   
        where AssessmentGroupId = @assessmentGroupId   
  
       End  
      End  
     End  
     END  
            
         
  
    End  
   else  
    BEGIN  
    select @idIrf = 0              
    END  
  
  
  commit tran  
  
 End Try  
 Begin Catch   
  rollback transaction  
  DECLARE @Message varchar(MAX) = ERROR_MESSAGE(),    
    @Severity int = ERROR_SEVERITY(),    
    @State smallint = ERROR_STATE()    
     
  RAISERROR (@Message, @Severity, @State)  
 End Catch  
  
End                       
GO



