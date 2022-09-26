IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewCountClientMapped'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewCountClientMapped
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetInterviewCountClientMapped
@startDate datetime,
@endDate datetime
AS
BEGIN
Select uli.EmailId,COUNT(im.id) as TotalCompletedInterview,uli.FirstName as ClientName from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId join InterviewComments ic on ic.InterviewId=im.id
join Client cl on cl.id=IRF.ClientId
join TransClientUser tc on tc.IRFId=IRF.id join UserList uli on uli.id=tc.ClientUserId  
where interviewDate>=@startDate and interviewDate<=@endDate and 
(ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7 or ic.MeetingStatus=8) 
group by uli.EmailId,uli.FirstName
END
Go
