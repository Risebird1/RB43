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
@idIrf int output            
            
AS            
Begin            
if not exists (select 1 from IRF where IRFId =@IRFId)     
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
   ,[IsMeetingNotificationSend])            
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
   @IsMeetingNotificationSend)      
       
    
select @idIrf = SCOPE_IDENTITY()    
End
else
BEGIN
select @idIrf = 0            
END
End            
  
    
GO



