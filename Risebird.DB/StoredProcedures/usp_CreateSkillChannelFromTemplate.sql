IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CreateSkillChannelFromTemplate'))
BEGIN
    DROP PROCEDURE dbo.usp_CreateSkillChannelFromTemplate
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_CreateSkillChannelFromTemplate]              
    
@IRFId nvarchar(200),              
@ClientName varchar(200),              
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
@AssessmentGroupId varchar(100),  
@IsQCRequired bit,  
@IsJoinabilitySelectabilityRequired bit,  
@IsGeneralSkillRequired bit,      
@CreatedFromTemplateId int,  
@MeetingPlatform varchar(25),  
@IsAadhaarVerificationRequired bit,
@IsCandidateavailability bit,
@IsMeetingAutoCancelled bit,
@IsMeetingNotificationSend bit,
@IsNewSkillChannel bit = null,
@TechSkillBulkData AS [dbo].[TransTechSkill_TableType] READONLY,  
@InterviewerBulkData AS [dbo].[TransInterviewerSurcharge_TableType] READONLY,  
@ClientUserBulkData AS [dbo].[TransClientUser_TableType] READONLY  
            
AS              
Begin    
  
begin try    
  begin tran    
    
  declare @irfIntId int  
  set @irfIntId = 0  
  
  declare @flatRate int = 0
  set @flatRate = (select rate from PlanTypeChargesMaster where CountryType = @JobCountry and PlanTypeEnumValue =@Heuristicsscore and CompanyType = @CompanyType)



  if not exists (select 1 from IRF where [IRFId] = @IRFId)       
  Begin     
  
  INSERT INTO [dbo].[IRF]              
  ([IRFId]              
  ,[clientName]              
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
  ,[CreatedFromTemplateId]  
  ,[MeetingPlatform]
  ,[IsAadhaarVerificationRequired]
  ,[IsCandidateavailability]
  ,[IsMeetingAutoCancelled]
  ,[IsMeetingNotificationSend]
  ,[IsNewSkillChannel]
  ,[FlatRate])
  VALUES              
  (@IRFId,              
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
  @CreatedFromTemplateId,  
  @MeetingPlatform,
  @IsAadhaarVerificationRequired ,
  @IsCandidateavailability,
  @IsMeetingAutoCancelled,
  @IsMeetingNotificationSend,
  @IsNewSkillChannel,
  @flatRate)                                                                           
         
      
  select @irfIntId = SCOPE_IDENTITY()      
  End  
  
  if(@irfIntId != 0)  
  Begin  
   
 --tech skills  
 DELETE from TransTechSkill where IRFID in (Select IRFId From @TechSkillBulkData)    
 --This row performs bulk copy of object DataTable to Table in SQL      
 INSERT INTO [dbo].[TransTechSkill](      
  [IRFId] ,      
  [LevelId],      
  [SkillId],      
  [Weightage],[Concepts],[Natureofquestion],[Timelimit])      
      
 SELECT @irfIntId, tsbd.LevelId, tsbd.SkillId, tsbd.Weightage, tsbd.Concepts, tsbd.Natureofquestion, tsbd.Timelimit FROM @TechSkillBulkData tsbd   
 --interviewers  
 --Start :Delete interviewers
 Declare @Assessmentid int  
Select @Assessmentid=AssessmentGroupid From IRF where id=@irfIntId  
Declare @temptable Table(Userid int)  
Insert into @temptable Select Userid from InterviewerProfile cross apply string_split(AssessmentGroupid,',') where value=@Assessmentid  
 --delete from TransInterviewer where IRFId=@id 
 Delete b FROM TransInterviewer b LEFT JOIN @temptable f ON f.Userid = b.Userid WHERE f.Userid IS NOT NULL and b.IRFId=@irfIntId
 --END: Delete interviewers
  
 INSERT INTO [dbo].[TransInterviewer](    
 [IRFId] ,    
 [UserId],    
 [PerSuccessRate],  
 [Surcharge],
 [IsActive],  
 [IsAvailable],  
 [InterviewerId])    
      
 SELECT @irfIntId, ibd.UserId, @flatRate, ibd.Surcharge, ibd.IsActive, ibd.IsAvailable, InterviewerProfile.id   
 as InterviewerId FROM @InterviewerBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId    
  
  
 DELETE from TransClientUser where IRFID = @irfIntId  
 --This row performs bulk copy of object DataTable to Table in SQL    
 INSERT INTO [dbo].[TransClientUser](    
 [IRFId] ,    
 [ClientUserId],    
 [ClientRoleId])    
     
    
 SELECT @irfIntId, cubd.ClientUserId, ClientUser.RoleId as ClientRoleId FROM @ClientUserBulkData cubd left join ClientUser on cubd.ClientUserId= ClientUser.UserId    
    
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



