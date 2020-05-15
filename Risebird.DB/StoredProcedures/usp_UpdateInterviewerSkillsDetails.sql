IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewerSkillsDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerSkillsDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_UpdateInterviewerSkillsDetails]

@InterviewerSkillId nvarchar(200),
@id int,
@JDTiltle nvarchar(200),
@StartDate datetime,
@EndDate datetime,
@RisebirdHr int,
@PerMintuteRate real,
@CalendlyLink varchar(500),
@PrimarySkill int,
@UserId int
AS
Begin

update InterviewerSkillsChannel 
set
[InterviewerSkillId]=@InterviewerSkillId,
           [JDTiltle]=@JDTiltle
           ,[StartDate]=@StartDate
           ,[EndDate]=@EndDate           
           ,[RisebirdHr]=@RisebirdHr
           ,[PerMintuteRate]=@PerMintuteRate
		   ,[CalendlyLink] =@CalendlyLink
		   ,[PrimarySkill]=@PrimarySkill
		   ,[UserId]=@UserId

Where id =@id

delete from TransInterviewerSkillsLeadInterviewer where InterviewerSkillId=@id
delete from TransInterviewerSkillsGeneralSkills where InterviewerSkillId=@id
delete from TransInterviewerSkillsTechSkills where InterviewerSkillId=@id
delete from TransInterviewerSkillsSelectablity where InterviewerSkillId=@id
End
Go


