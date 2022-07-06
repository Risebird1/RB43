IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetQCReportDetailsByInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_GetQCReportDetailsByInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetQCReportDetailsByInterviewer
@Emailid varchar(200)
AS
BEGIN
Select im.id as imid,im.IRFId,im.InterviewDate,im.candidateName,im.candidateEmail,IRF.JDTiltle,cl.Name as ClientName,ul.EmailId as QCFormedBy,ic.MeetingStatus,ic.InterviewStatus,ic.VideoLink,ic.VideoDuration,ISNULL(tq.Redflags,0) Redflags 
from Trans_QCFormReport tq join InterviewMeetings im on im.id=tq.Interviewid 
join InterviewComments ic on ic.InterviewId=im.id join UserList ul on ul.id=tq.Userid 
join IRF on IRF.IRFId=im.IRFId join Client cl on cl.id=IRF.ClientId 
where im.interviewer=@Emailid    
END
Go