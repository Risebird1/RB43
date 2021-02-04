/****** Object:  StoredProcedure [dbo].[usp_UpdateIRFDetails]    Script Date: 26-09-2019 5.07.18 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateIRFDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateIRFDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
 
Create PROCEDURE [dbo].[usp_UpdateIRFDetails]    
    
@IRFId nvarchar(200),    
@id int,    
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
@Tag varchar(200),
@idIrf int output    
    
AS    
Begin    
    
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
			,[Tag]=@Tag
Where id =@id    
    
delete from TransClientUser where IRFId=@id    
delete from TransInterviewer where IRFId=@id    
delete from TransJoinability where IRFId=@id    
    
delete from TransQualityController where IRFId=@id    
    
delete from TransSkillGeneral where IRFId=@id    
    
delete from TransTechSkill where IRFId=@id    
    
delete from TransSelectability where IRFId=@id    
delete from TransEmergencyInterviewer where IRFId=@id    
delete from TransResumeEvaluator where IRFId=@id    
delete from TransHR where IRFId=@id    
delete from TransRecruiter where IRFId=@id    
    
End    
 


GO

