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
@CreatedOn datetime,            
@IsBiometricEnabled bit,            
@PriorityLevel int,            
@CompanyDetails varchar(1000),           
@Type varchar(5),        
@Heuristicsscore int,        
@PrimarySkillInvite varchar(100),        
@BillingOwner int,     
@AssessmentGroupId varchar(100),
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
@TechSkillBulkData AS [dbo].[TransTechSkill_TableType] READONLY,
@InterviewerBulkData AS [dbo].[TransInterviewerSurcharge_TableType] READONLY,
@EmergencyInterviewerBulkData AS [dbo].[TransEmergencyInterviewer_TableType] READONLY,
@ClientUserBulkData AS [dbo].[TransClientUser_TableType] READONLY,
--@QCBulkData AS [dbo].[TransQualityController_TableType] READONLY,
@HRBulkData AS [dbo].[TransHR_TableType] READONLY,
--@FixedAmount int = null,
@IsNewSkillChannel bit = null,
@FlatRate int,
@idIrf int output            
            
AS            
Begin
	Begin Try
		Begin tran

			if not exists (select 1 id from IRF where IRFId =@IRFId)     
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
				   --,[IsQCRequired]
				   ,[IsJoinabilitySelectabilityRequired]
				   ,[IsGeneralSkillRequired]
				   ,[IsAadhaarVerificationRequired]
				   ,[IsCandidateavailability]
				   ,[IsMeetingAutoCancelled]
				   ,[IsMeetingNotificationSend]
				   ,[IsNewSkillChannel]
				   ,[FlatRate]
				   )
				   --,[InterviewDuration]
				   --,[IsFixedInterviewduration]
				   --,[FixedAmount])            
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
				   --@IsQCRequired,
				   @IsJoinabilitySelectabilityRequired,
				   @IsGeneralSkillRequired,
				   @IsAadhaarVerificationRequired,
				   @IsCandidateavailability,
				   @IsMeetingAutoCancelled,
				   @IsMeetingNotificationSend,
				   @IsNewSkillChannel,
				   @FlatRate
				   )
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
					[Weightage],[Concepts],[Natureofquestion],[Timelimit])    
    
					SELECT @idIrf, tsbd.LevelId, tsbd.SkillId, tsbd.Weightage, tsbd.Concepts, tsbd.Natureofquestion, tsbd.Timelimit FROM @TechSkillBulkData tsbd
					--interviewers

					INSERT INTO [dbo].[TransInterviewer](  
					[IRFId] ,  
					[UserId],  
					[PerSuccessRate], 
					[Surcharge],
					[IsActive],
					[IsAvailable],
					[InterviewerId])  
    
					SELECT @idIrf, ibd.UserId, ibd.PerSuccessRate, ibd.Surcharge, ibd.IsActive, ibd.IsAvailable, InterviewerProfile.id as InterviewerId FROM @InterviewerBulkData ibd left join InterviewerProfile on ibd.UserId = InterviewerProfile.UserId  

					--emergency interviewers

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
 
					--quality controllers
				 --	delete from TransQualityController where IRFId=@id        

					--INSERT INTO [dbo].[TransQualityController](  
					--[IRFId] ,  
					--[UserId],  
					--[PerSuccessRate],
					--[IsActive],
					--[InterviewerId])  
     
					--SELECT qcbd.*, InterviewerProfile.id as InterviewerId FROM @QCBulkData qcbd left join InterviewerProfile on qcbd.UserId = InterviewerProfile.UserId  
 
					--HRs
	
					INSERT INTO [dbo].[TransHR](  
					[IRFId] ,  
					[HRId])  
   
					SELECT @idIrf, HR.id as HRId FROM @HRBulkData hrbd left join HR on hrbd.UserId = HR.UserId

					if(@SkillChannelSME is not null and @SkillChannelSME != 0)
					Begin
						insert into SkillChannelSMEs (IRFId, InterviewerProfileId, AddedBy, PerformedAt) 
						values (@idIrf, @SkillChannelSME, Isnull(@CreatedBy, ''), @CreatedOn)
					End

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



