IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAIInterviewDetailsBySessionId'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAIInterviewDetailsBySessionId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetAIInterviewDetailsBySessionId
@SessionId varchar(50)
AS
BEGIN

select im.id as InterviewId, im.interviewDate, im.AssistantId, im.candidateEmail, ulc.MobileNumber, im.interviewer,
im.TimeLeftInSeconds, im.TimeZonesValue, im.TimeZoneoffset, irf.JDTiltle, iss.ThreadId
from InterviewMeetings im
join IRF on IRF.irfId = im.irfid
join UserList ulc on ulc.EmailId = im.candidateEmail
outer apply (select ThreadId from InterviewSessions where SessionId = @SessionId) iss
where im.SessionId = @SessionId


END
GO