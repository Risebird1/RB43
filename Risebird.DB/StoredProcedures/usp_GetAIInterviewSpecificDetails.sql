IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAIInterviewSpecificDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIInterviewSpecificDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetAIInterviewSpecificDetails
@SessionId varchar(200)
AS
BEGIN

select im.AssistantId, ThreadId  from InterviewMeetings im join InterviewSessions ise on ise.SessionId =  im.SessionId
where im.SessionId = @SessionId 

END
GO