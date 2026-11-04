/****** Object:  StoredProcedure [dbo].[usp_AddOrEditInterviewerProfile]    Script Date: 5/1/2020 3:59:20 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddOrEditInterviewerProfile'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddOrEditInterviewerProfile]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AddOrEditInterviewerProfile]              
@UserId int,              
@FirstName varchar(150),              
@LastName nvarchar(150),              
@NickName varchar(100),              
@EmailId varchar(150),              
@AltEmailId varchar(150),              
@DOB DateTime,              
@Gender varchar(50),              
@AnnualSalary nvarchar(200),              
@Country varchar(200),              
@City varchar (200),              
@MobileNumber nvarchar(200),              
@AltMobileNumber nvarchar(200),              
@CurrentRole varchar(150),              
@Experience decimal(10,2),              
@PreferredDomain varchar(200),              
@GitHubLink nvarchar(max),              
@ResumeLink nvarchar(max),              
@CodedResumeLink nvarchar(max),              
@StackOverflowLink nvarchar(max),              
@LinkedinLink nvarchar(max),              
@Blog nvarchar(max),              
@IsLeadInterviewer bit,              
@IsActive bit,
@IsInterviewerAccess bit = null,
@IsApproved bit,              
@AssessmentGroupId varchar(100),          
@UserPassword nvarchar(60),       
@WhatsappNumber varchar(15),      
@Isinstaavailable bit,    
@InterviewerProfileId int output,    
--@IsServiceDomain bit,    
--@IsProductDomain bit,    
@IndiaServiceRate int,    
@IndiaProductRate int,    
@OutsideIndiaServiceRate int,    
@OutsideIndiaProductRate int,  
@isVerified bit,  
@CompanyName varchar(100),  
@IsEmployed bit,  
@PrimarySpecializationForAcknowledgement varchar(250)=null,
@loggedInUser varchar(50),  
@AssessmentGroupDifference varchar(100),  
@assessmentGroupList varchar(max) out,  
@RemovedAssessmentGroups varchar(max)=null,
@TargetIrfId int,
@outputUserId int out,
@uniqueid varchar(50) out,
@IsInsertOperation bit out,
@IsHoldPayment bit = null,
@HoldPaymentReason varchar(500) = null
AS              
Begin

SET XACT_ABORT ON;

begin try  
 begin tran 

 set @assessmentGroupList = ''  
  if(@AssessmentGroupDifference is not null and @AssessmentGroupDifference != '')  
  Begin  
    declare @temp table(assessmentGroupId varchar(5), TotalInterviewers int)  
    
   insert into @temp  
  
   select a.value, b.TotalInterviewers from string_split(@AssessmentGroupDifference,',') a  
   outer apply  
   (select count(ipr.Id) as TotalInterviewers from InterviewerProfile ipr join UserList ul on ul.id = ipr.UserId cross apply string_split(ipr.AssessmentGroupId, ',') where a.value = value and ul.IsActive=1 group by value) b  
  
  
   if exists(select top 1 assessmentGroupId from @temp where TotalInterviewers >=2000)  
   Begin  
  set @assessmentGroupList = (select STRING_AGG(asg.AssessmentGroup, ',') from @temp tp join AssessmentGroup asg on asg.id = tp.assessmentGroupId where tp.TotalInterviewers >= 2000)  
  rollback tran  
  return  
   End  
      
  End  
if(@assessmentGroupList is null or @assessmentGroupList = '')  
Begin  
 -- Only a real (positive) UserId is an EDIT. New profiles are sent with @UserId = 0; without the
 -- @UserId > 0 guard, any stray InterviewerProfile row with UserId = 0 would route every new
 -- interviewer into this UPDATE branch (overwriting that stray row and returning @outputUserId = 0).
 If (ISNULL(@UserId, 0) > 0 AND Exists (select 1 from [dbo].[InterviewerProfile] where userId = @UserId))
-- Edit in both the tables
 Begin              
  -- Set flag to indicate UPDATE operation
  SET @IsInsertOperation = 0;
              
 Declare @v_ProfileId int;

  -- Values before the edit, for InterviewerActionLog (QC Activity status changes).
  Declare @OldIsActive bit, @OldIsInterviewerAccess bit, @OldIsHoldPayment bit;
  select @OldIsActive = IsActive, @OldIsInterviewerAccess = IsInterviewerAccess from [dbo].[UserList] where Id = @UserId;
  select top 1 @OldIsHoldPayment = IsHoldPayment from [dbo].[InterviewerProfile] where UserId = @UserId order by Id desc;

   Update [dbo].[UserList]
   SET              
    [FirstName] = @FirstName,              
    [LastName] = @LastName,              
    [EmailId] = @EmailId,              
    [IsActive] = @IsActive,
    [IsInterviewerAccess] = ISNULL(@IsInterviewerAccess, [IsInterviewerAccess]),
    [MobileNumber] = @MobileNumber,               
  [WhatsappNumber]=@WhatsappNumber          
    where Id = @UserId;              
              
   Update [dbo].[InterviewerProfile]              
   Set              
    [NickName] = @NickName,               
    [AltEmailID] = @AltEmailId,              
    [DOB] = @DOB,              
    [Gender] = @Gender,              
    [AnnualSalary] = @AnnualSalary,              
    [Country] = @Country,              
    [City] = @City,              
    [AltMobileNumber] = @AltMobileNumber,              
    [CurrentRole] = @CurrentRole,              
    [Experience] = @Experience,              
    [PreferredDomain] = @PreferredDomain,              
    [ResumeLink] = @ResumeLink,              
    [CodedResumeLink] = @CodedResumeLink,              
    [GitHubLink] = @GitHubLink,              
    [StackOverflowLink] = @StackOverflowLink,              
    [LinkedinLink] = @LinkedinLink,              
    [Blog] = @Blog,              
    [IsLeadInterviewer] = @IsLeadInterviewer,              
    [IsApproved] = @IsApproved,              
    [AssessmentGroupId]=@AssessmentGroupId,    
    [Isinstaavailable]=@Isinstaavailable,  
    [IsHoldPayment] = ISNULL(@IsHoldPayment, [IsHoldPayment]),
    [HoldPaymentReason] = @HoldPaymentReason,
    [ModifiedBy] = @loggedInUser,  
    [ModifiedOn] = dateadd(MINUTE, 330, getdate())  
    --[IsServiceDomain] =@IsServiceDomain,    
    --[IsProductDomain] = @IsProductDomain,    
    --[IndiaServiceRate] = @IndiaServiceRate,    
    --[IndiaProductRate] = @IndiaProductRate,    
    --[OutsideIndiaServiceRate] = @OutsideIndiaServiceRate,    
    --[OutsideIndiaProductRate] = @OutsideIndiaProductRate    
  where userId = @UserId

  -- Call opt-out / MAG: an admin switching the interviewer off here (Active or Interviewer access) removes them
  -- from every MAG, like the dashboard and flags panel; switching them back on restores the groups removed by a
  -- manual inactivation (merged with any picked in this form). A call opt-out exclusion is left alone.
  Declare @MagWasOn bit = CASE WHEN ISNULL(@OldIsActive, 0) = 1 AND ISNULL(@OldIsInterviewerAccess, 1) = 1 THEN 1 ELSE 0 END;
  Declare @MagIsOn bit = CASE WHEN ISNULL(@IsActive, 0) = 1 AND ISNULL(ISNULL(@IsInterviewerAccess, @OldIsInterviewerAccess), 1) = 1 THEN 1 ELSE 0 END;
  Declare @MagNow datetime = DATEADD(MINUTE, 330, GETUTCDATE());
  Declare @MagBy varchar(200) = ISNULL(NULLIF(LTRIM(RTRIM(@loggedInUser)), ''), 'Admin');
  IF @MagWasOn = 1 AND @MagIsOn = 0
      EXEC usp_ExcludeInterviewerFromMAG @UserId, 'ManualInactive', @MagBy, @MagNow
  IF @MagWasOn = 0 AND @MagIsOn = 1
      EXEC usp_RestoreInterviewerToMAG @UserId, 'ManualInactive', @MagBy, @MagNow

  -- QC Activity: one InterviewerActionLog row per status that actually changed in this edit
  -- (Active, Interviewer access, Hold payment). Skipped while the log table is missing.
  IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
  BEGIN
   Declare @NewIsInterviewerAccess bit = ISNULL(@IsInterviewerAccess, @OldIsInterviewerAccess);
   Declare @NewIsHoldPayment bit = ISNULL(@IsHoldPayment, @OldIsHoldPayment);

   insert into dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
   select @UserId, @EmailId, a.ActionType, a.Reason, NULLIF(LTRIM(RTRIM(@loggedInUser)), ''), 'usp_AddOrEditInterviewerProfile'
   from (values
        (CASE WHEN ISNULL(@OldIsActive, 0) = 1 AND ISNULL(@IsActive, 0) = 0 THEN 'Inactivated'
              WHEN ISNULL(@OldIsActive, 0) = 0 AND ISNULL(@IsActive, 0) = 1 THEN 'Reactivated' END,
         CAST(NULL AS nvarchar(500))),
        (CASE WHEN ISNULL(@OldIsInterviewerAccess, 1) = 1 AND ISNULL(@NewIsInterviewerAccess, 1) = 0 THEN 'AccessRevoked'
              WHEN ISNULL(@OldIsInterviewerAccess, 1) = 0 AND ISNULL(@NewIsInterviewerAccess, 1) = 1 THEN 'AccessRestored' END,
         CAST(NULL AS nvarchar(500))),
        (CASE WHEN ISNULL(@OldIsHoldPayment, 0) = 0 AND ISNULL(@NewIsHoldPayment, 0) = 1 THEN 'PaymentHold'
              WHEN ISNULL(@OldIsHoldPayment, 0) = 1 AND ISNULL(@NewIsHoldPayment, 0) = 0 THEN 'PaymentHoldReleased' END,
         CASE WHEN ISNULL(@OldIsHoldPayment, 0) = 0 AND ISNULL(@NewIsHoldPayment, 0) = 1
              THEN CAST(NULLIF(LTRIM(RTRIM(@HoldPaymentReason)), '') AS nvarchar(500)) END)
   ) a (ActionType, Reason)
   where a.ActionType IS NOT NULL;
  END

   select @v_ProfileId = id from [dbo].[InterviewerProfile] where UserId = @UserId;
   
    select @outputUserId= @UserId 
	select @uniqueid = uniqueid from [dbo].[UserList] where id = @UserId;
   --delete from TransInterviewerSkill where InterviewerProfileId=@v_ProfileId              
              
   --delete from TransInterviewerEducation where InterviewerProfileId=@v_ProfileId              
              
   --delete from TransInterviewerProfessional where InterviewerProfileId=@v_ProfileId              
              
   --delete from TransInterviewerProjects where InterviewerProfileId=@v_ProfileId              
              
   --delete from TransInterviewerAvailability where InterviewerProfileId=@v_ProfileId              
              
   --delete from TransInterviewerAccDetails where InterviewerProfileId=@v_ProfileId              
              
   --delete from TransInterviewerCertificate where InterviewerProfileId=@v_ProfileId           
          
   select @InterviewerProfileId=@v_ProfileId     
      
   select * from InterviewerRecommendation    
    
   if exists(select id from InterviewerRecommendation where Userid = @UserId)    
   begin    
  update InterviewerRecommendation set     
  PerHourRateIndia = @IndiaServiceRate,    
  PerHourRateOutside =@OutsideIndiaServiceRate,    
  IndiaProductRate =@IndiaProductRate,    
  OutsideIndiaProductRate =@OutsideIndiaProductRate    
  where Userid =@UserId    
   end    
   else    
   begin    
  insert into InterviewerRecommendation (Userid, PerHourRateIndia, PerHourRateOutside, IndiaProductRate, OutsideIndiaProductRate)    
  values (@UserId, @IndiaServiceRate, @OutsideIndiaServiceRate, @IndiaProductRate, @OutsideIndiaProductRate)    
   end    
              
 End              
              
 Else              
--Insert new record for Add request              
 Begin              
  -- Set flag to indicate INSERT operation
  SET @IsInsertOperation = 1;

Declare @v_User_Id int;              
              
 INSERT INTO [dbo].[UserList]              
           ([idProfile],              
      [FirstName],              
   [LastName],              
   [EmailId],              
   [IsActive],              
   [IsInterviewerAccess],
   [MobileNumber],        
   [UserPassword],      
   [WhatsappNumber],    
   [uniqueid])              
                 
       VALUES              
           (3,              
      @FirstName,              
   @LastName,              
   @EmailId,              
   @IsActive,              
   1,
   @MobileNumber,        
   @UserPassword,      
   @WhatsappNumber,    
   NEWID())            
              
 -- Collect the user ID from UserList tbl              
 Select @v_User_Id = id from [dbo].[UserList] where EmailId = @EmailId;              
              
 INSERT INTO [dbo].[InterviewerProfile]              
           ([UserId],              
   [NickName],               
   [AltEmailID],              
   [DOB],              
   [Gender],              
   [AnnualSalary],              
   [Country],              
   [City],              
   [AltMobileNumber],              
   [CurrentRole],              
   [Experience],              
   [PreferredDomain],              
   [ResumeLink],              
   [CodedResumeLink],              
   [GitHubLink],              
   [StackOverflowLink],              
   [LinkedinLink],              
   [Blog],              
   [IsLeadInterviewer],              
   [IsApproved],              
   [AssessmentGroupId],    
   [Isinstaavailable],  
   [CreatedBy],  
   [CreatedOn],  
   [isVerified],  
   [CompanyName],  
   [IsEmployed],
   [PrimarySpecializationForAcknowledgement],
   [IsHoldPayment],
   [HoldPaymentReason])    
   --[IsServiceDomain],    
   --[IsProductDomain],    
   --[IndiaServiceRate],    
   --[IndiaProductRate],    
   --[OutsideIndiaServiceRate],    
   --[OutsideIndiaProductRate])              
       VALUES              
           (@v_User_Id,              
   @NickName,              
   @AltEmailId,              
   @DOB,              
   @Gender,              
   @AnnualSalary,              
   @Country,              
   @City,              
   @AltMobileNumber,              
   @CurrentRole,              
   @Experience,              
   @PreferredDomain,              
   @ResumeLink,              
   @CodedResumeLink,              
   @GitHubLink,              
   @StackOverflowLink,              
   @LinkedinLink,              
   @Blog,              
   @IsLeadInterviewer,              
   @IsApproved,              
   @AssessmentGroupId,    
   @Isinstaavailable,  
   @loggedInUser,  
   dateadd(MINUTE, 330, getdate()),  
   @isVerified,  
   @CompanyName,  
   @IsEmployed,
   @PrimarySpecializationForAcknowledgement,
   ISNULL(@IsHoldPayment, 0),
   @HoldPaymentReason)    
   --@IsServiceDomain,    
   --@IsProductDomain,    
   --@IndiaServiceRate,    
   --@IndiaProductRate,    
   --@OutsideIndiaServiceRate,    
   --@OutsideIndiaProductRate              
        
 select @InterviewerProfileId=(select id from InterviewerProfile where UserId=@v_User_Id)    

 declare @primaryAg varchar(7) = (select top 1 value from string_split(@AssessmentGroupId,','))
 declare @primaryAgSkill varchar(75) = (select top 1 Trim(value) from AssessmentGroup ag cross apply string_split(PrimarySkills,',')  where id = @primaryAg)
 declare @skillId int = (select top 1 id from Skill where [Name] like '%' + @primaryAgSkill + '%')
 
 select @outputUserId=@v_User_Id 
 select @uniqueid = uniqueid from [dbo].[UserList] where id = @v_User_Id;

   if(@TargetIrfId > 0)
 Begin
	declare @rate int
	set @rate =   (select Rate from PlanTypeChargesMaster where CountryType = (select JobCountry from IRF where id = @TargetIrfId) 
				and PlanTypeEnumValue = (select Heuristicsscore from IRF where id = @TargetIrfId)
				and CompanyType = (select CompanyType from IRF where id = @TargetIrfId))

        if not exists(select 1 from TransInterviewer where UserId = @v_User_Id and IRFId = @TargetIrfId)
        Begin
            -- Skill-channel activation follows the channel's Interviewer Lock (IRF.DmIvLockNew):
            --   Locked  → IsActive = 0 (pending manual RB vetting), IsSMEVerified = 0, ClientAction NULL, ActivatedAt NULL
            --   Unlocked→ IsActive = 1 (auto-activated), IsSMEVerified = 1, ClientAction 'Shortlisted', ActivatedAt now (IST)
            DECLARE @targetIsLocked bit = ISNULL((SELECT DmIvLockNew FROM IRF WHERE id = @TargetIrfId), 0)
            insert into TransInterviewer(IRFId, InterviewerId, UserId, IsAvailable, IsActive, IsSMEVerified, ClientAction, PerSuccessRate, ActivatedAt)
            values (@TargetIrfId, @InterviewerProfileId, @v_User_Id, 1,
                CASE WHEN @targetIsLocked = 1 THEN 0 ELSE 1 END,
                CASE WHEN @targetIsLocked = 1 THEN 0 ELSE 1 END,
                CASE WHEN @targetIsLocked = 1 THEN NULL ELSE 'Shortlisted' END,
                @rate,
                CASE WHEN @targetIsLocked = 1 THEN NULL ELSE DATEADD(MINUTE, 330, GETDATE()) END)
        End
	 
 End
 Else If(@AssessmentGroupId IS NOT NULL AND @AssessmentGroupId <> '' AND @AssessmentGroupId <> '0')
 Begin
    -- MAG-direct onboarding: add interviewer to ALL active skill channels under this AG
    DECLARE @agIdInt INT = TRY_CAST(@AssessmentGroupId AS INT)
    IF @agIdInt > 0
    BEGIN
        -- MAG-direct onboarding: each skill channel's activation follows its Interviewer Lock
        -- (IRF.DmIvLockNew). Locked channels stay Available-only (IsActive = 0) pending manual RB
        -- vetting; unlocked channels are auto-activated (IsActive = 1) right away.
        INSERT INTO TransInterviewer(IRFId, InterviewerId, UserId, IsAvailable, IsActive, IsSMEVerified, ClientAction, PerSuccessRate, AddedAt, ActivatedAt, Source)
        SELECT 
            irf.id,
            @InterviewerProfileId,
            @v_User_Id,
            1,  -- IsAvailable
            CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN 0 ELSE 1 END,  -- IsActive
            CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN 0 ELSE 1 END,  -- IsSMEVerified
            CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN NULL ELSE 'Shortlisted' END,  -- ClientAction
            (SELECT TOP 1 Rate FROM PlanTypeChargesMaster 
             WHERE CountryType = irf.JobCountry 
               AND PlanTypeEnumValue = irf.Heuristicsscore 
               AND CompanyType = irf.CompanyType),
            DATEADD(MINUTE, 330, GETDATE()),  -- AddedAt (IST)
            CASE WHEN ISNULL(irf.DmIvLockNew, 0) = 1 THEN NULL ELSE DATEADD(MINUTE, 330, GETDATE()) END,  -- ActivatedAt (IST when auto-activated)
            'MAG-Direct'
        FROM IRF irf
        WHERE irf.AssessmentGroupId = @agIdInt
          AND irf.IsPaused = 0
          AND ISNULL(irf.IsDeleted, 0) = 0
          AND NOT EXISTS (
              SELECT 1 FROM TransInterviewer ti 
              WHERE ti.UserId = @v_User_Id AND ti.IRFId = irf.id
          )
    END
 End


 -- INSERT INTO [dbo].[TransInterviewerSkill]
	--	(InterviewerProfileId, 
	--	SkillId,  
	--	Category, 
	--	Ip, 
	--	Level, 
	--	SkillExperience, 
	--	Nature)
 --Values 
	--	(@InterviewerProfileId, 
	--	 @skillId,
	--	 1, 
	--	 1, 
	--	 1,
	--	 1,
	--	 1);



 if exists(select id from InterviewerRecommendation where Userid = @v_User_Id)    
 begin    
  update InterviewerRecommendation set     
  PerHourRateIndia = @IndiaServiceRate,    
  PerHourRateOutside =@OutsideIndiaServiceRate,    
  IndiaProductRate =@IndiaProductRate,    
  OutsideIndiaProductRate =@OutsideIndiaProductRate    
  where Userid =@UserId    
 end    
 else    
 begin    
 insert into InterviewerRecommendation (Userid, PerHourRateIndia, PerHourRateOutside, IndiaProductRate, OutsideIndiaProductRate)    
 values (@v_User_Id, @IndiaServiceRate, @OutsideIndiaServiceRate, @IndiaProductRate, @OutsideIndiaProductRate)    
 end    
End    
End  
  
if(@AssessmentGroupDifference is not null and @AssessmentGroupDifference != '')  
  Begin  
    --insert into @temp  
   insert into AssessmentGroupInterviewers  
   select a.value, @InterviewerProfileId, 0, @loggedInUser, GETDATE(), null, null from string_split(@AssessmentGroupDifference,',') a  
  End  
     
if(@RemovedAssessmentGroups is not null and @RemovedAssessmentGroups != '')  
  Begin  
    declare @tempRemovedFromAssessmentGroups table(assessmentGroupId varchar(5))  
     insert into @tempRemovedFromAssessmentGroups  
      select value from string_split(@RemovedAssessmentGroups,',')  
     
   --delete from TransInterviewer where id in   
   --(select * from @tempRemovedFromAssessmentGroups tra join irf on tra.assessmentGroupId = irf.AssessmentGroupId  
   --join TransInterviewer ti on ti.IRFId = irf.id where ti.UserId = @UserId)  
     
   update TransInterviewer set IsActive = 0 where id in   
   (select ti.id from @tempRemovedFromAssessmentGroups tra join irf on tra.assessmentGroupId = irf.AssessmentGroupId  
   join TransInterviewer ti on ti.IRFId = irf.id where ti.UserId = @UserId)  
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
     
End  
GO

