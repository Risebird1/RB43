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
	 [Weightage],[Concepts],[Natureofquestion],[Timelimit])    
    
	SELECT @id, tsbd.LevelId, tsbd.SkillId, tsbd.Weightage, tsbd.Concepts, tsbd.Natureofquestion, tsbd.Timelimit FROM @TechSkillBulkData tsbd 
	--interviewers
	delete from TransInterviewer where IRFId = @id

	INSERT INTO [dbo].[TransInterviewer](  
	[IRFId] ,  
	[UserId],  
	[PerSuccessRate], 
	[IsActive],
	[IsAvailable],
	[InterviewerId])  
    
	SELECT @id, ibd.UserId, ibd.PerSuccessRate, ibd.IsActive, ibd.IsAvailable, InterviewerProfile.id 
	as InterviewerId FROM @InterviewerBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId  

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



