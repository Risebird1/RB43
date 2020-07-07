/****** Object:  StoredProcedure [dbo].[usp_UpdateInterviewerSkillsChannel]    Script Date: 25-05-2020 10.57.14 AM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateInterviewerSkillsChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerSkillsChannel
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_UpdateInterviewerSkillsChannel]

@InterviewerSkillId nvarchar(200),
@id int,
@JDTiltle nvarchar(200),
@StartDate datetime,
@EndDate datetime,
@RisebirdHr int,
@PerMintuteRate real,
@CalendlyLink varchar(500),
@PrimarySkill int

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

Where id =@id

delete from TransInterviewerSkillsLeadInterviewer where InterviewerSkillId=@id
delete from TransInterviewerSkillsGeneralSkills where InterviewerSkillId=@id
delete from TransInterviewerSkillsTechSkills where InterviewerSkillId=@id
delete from TransInterviewerSkillsSelectablity where InterviewerSkillId=@id
End

GO


