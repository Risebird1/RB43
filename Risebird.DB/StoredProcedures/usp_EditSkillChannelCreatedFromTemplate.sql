IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_EditSkillChannelCreatedFromTemplate'))
BEGIN
    DROP PROCEDURE dbo.usp_EditSkillChannelCreatedFromTemplate
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_EditSkillChannelCreatedFromTemplate]              
    
@id int,  
@ClientName varchar(200),              
@JDTiltle nvarchar(200),              
@CompanyType varchar(200),                     
@JobCountry varchar(50),                         
@JDLink varchar(1000),              
@NoOfPosition int,              
@NoOfIntterview int,              
@RisebirdHr int,              
@PerMintuteRate real,              
@JobLevel int,              
@CalendlyLink varchar(500),              
@TotalBudget int,              
@ModifiedOn datetime,              
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
@IsAadhaarVerificationRequired bit, 
@IsCandidateavailability bit,
@IsMeetingAutoCancelled bit,
@IsMeetingNotificationSend bit,
@IsVideoRequired bit,
@TechSkillBulkData AS [dbo].[TransTechSkill_TableType] READONLY,  
@InterviewerBulkData AS [dbo].[TransInterviewer_TableType] READONLY,  
@ClientUserBulkData AS [dbo].[TransClientUser_TableType] READONLY  
            
AS              
Begin    
  
begin try    
  begin tran    
    
    
  if exists (select 1 from IRF where id = @id)       
  Begin    
  
 declare @oldCalendlyLink varchar(500), @isCalendlyChanged bit   
 set @isCalendlyChanged=0  
 select @oldCalendlyLink = CalendlyLink from IRF where id = @id  
  
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
 ,[IsQCRequired] = @IsQCRequired  
 ,[IsJoinabilitySelectabilityRequired] = @IsJoinabilitySelectabilityRequired  
 ,[IsGeneralSkillRequired] = @IsGeneralSkillRequired  
 ,[ModifiedOn] = @ModifiedOn
 ,[IsAadhaarVerificationRequired]=@IsAadhaarVerificationRequired
 ,[IsCandidateavailability]=@IsCandidateavailability
,[IsMeetingAutoCancelled]=@IsMeetingAutoCancelled
,[IsMeetingNotificationSend]=@IsMeetingNotificationSend
,[IsVideoRequired]=@IsVideoRequired
 Where id =@id          
      
   
 if(@isCalendlyChanged =1)  
 begin  
  if not exists(select id from TransCalendlyLinkAddedUpdated where IRFId =@id)  
  begin  
   insert into TransCalendlyLinkAddedUpdated values (@id, GETDATE())  
  end  
 end  
  
  
  
  --tech skills  
 DELETE from TransTechSkill where IRFID = @id    
 --This row performs bulk copy of object DataTable to Table in SQL      
 INSERT INTO [dbo].[TransTechSkill](      
  [IRFId] ,      
  [LevelId],      
  [SkillId],      
  [Weightage],[Concepts],[Natureofquestion],[Timelimit],[isEliminationskill])      
      
 SELECT @id, tsbd.LevelId, tsbd.SkillId, tsbd.Weightage, tsbd.Concepts, tsbd.Natureofquestion, tsbd.Timelimit,tsbd.isEliminationskill FROM @TechSkillBulkData tsbd   
 --interviewers  
 --Start :Upsert interviewers
 -- NOTE: This section used to DELETE + re-INSERT interviewers on every save. That approach
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

 -- Interviewers currently active on this IRF but NOT resubmitted => removed from the panel in the UI.
 -- Soft-remove them (deactivate) instead of deleting. Guarded so an empty/absent bulk TVP can never
 -- mass-deactivate an existing panel.
 IF EXISTS (SELECT 1 FROM @InterviewerBulkData)
 BEGIN
  UPDATE b SET b.IsActive = 0, b.IsAvailable = 0, b.ModifiedAt = @ModifiedOn
  FROM TransInterviewer b
  WHERE b.IRFId=@id
    AND ISNULL(b.IsActive, 0) = 1
    AND NOT EXISTS (SELECT 1 FROM @InterviewerBulkData i WHERE i.UserId = b.UserId AND i.IRFId = b.IRFId)
 END
 --END: Soft-remove interviewers
  
 INSERT INTO [dbo].[TransInterviewer](    
 [IRFId] ,    
 [UserId],    
 [PerSuccessRate],   
 [IsActive],  
 [IsAvailable],  
 [InterviewerId])    
      
 SELECT distinct @id, ibd.UserId, ibd.PerSuccessRate,
 -- An interviewer may only be ACTIVE on a channel while they belong to the channel's MAG. If the
 -- interviewer is not (or no longer) in the MAG, force IsActive = 0 regardless of what was submitted.
 CASE WHEN EXISTS (SELECT 1 FROM @temptable t WHERE t.Userid = ibd.UserId) THEN ibd.IsActive ELSE 0 END,
 ibd.IsAvailable, InterviewerProfile.id   
 as InterviewerId FROM @InterviewerBulkData ibd join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId    
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
    ti.IsAvailable = ibd.IsAvailable
 FROM [dbo].[TransInterviewer] ti
 JOIN @InterviewerBulkData ibd ON ibd.UserId = ti.UserId AND ibd.IRFId = ti.IRFId;
 
 DELETE from TransClientUser where IRFID = @id  
 --This row performs bulk copy of object DataTable to Table in SQL    
 INSERT INTO [dbo].[TransClientUser](    
 [IRFId] ,    
 [ClientUserId],    
 [ClientRoleId])    
     
    
 SELECT @id, cubd.ClientUserId, ClientUser.RoleId as ClientRoleId FROM @ClientUserBulkData cubd left join ClientUser on cubd.ClientUserId= ClientUser.UserId    
       
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



