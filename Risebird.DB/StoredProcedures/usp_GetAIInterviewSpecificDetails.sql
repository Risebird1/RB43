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

-- InterviewSessions is joined loosely on purpose. The very first adaptive turn resolves this
-- skill channel's prompt ids before any session row exists, so an inner join would return
-- nothing there and the turn would silently fall back to the global configured prompt.
select im.id as imid, im.AssistantId, ise.ThreadId, irf.IsAIInterview, AIInterviewType, irf.id as irfIntId,
irf.OpenAIPromptId, irf.OpenAIEvaluatorPromptId
from InterviewMeetings im 
left join InterviewSessions ise on ise.SessionId =  im.SessionId
join irf on irf.IRFId = im.irfId
where im.SessionId = @SessionId 

END
GO
