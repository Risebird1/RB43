/****** Object:  StoredProcedure [dbo].[usp_GetTransInterviewer]    Script Date: 12-11-2019 1.41.58 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetTransEmergencyInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTransEmergencyInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetTransEmergencyInterviewer]

@irfId int


AS
Begin

select TransEmergencyInterviewer.*, Interviewer.PrimarySkill from TransEmergencyInterviewer 
inner join 
Interviewer on TransEmergencyInterviewer.InterviewerId = Interviewer.id where IRFId=@irfId



End


GO


