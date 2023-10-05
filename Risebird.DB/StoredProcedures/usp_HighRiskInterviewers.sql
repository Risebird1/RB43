IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_HighRiskInterviewers'))
BEGIN
    DROP PROCEDURE dbo.usp_HighRiskInterviewers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_HighRiskInterviewers
AS
Begin
Select im.id,im.IRFId as IRFId,im.interviewDate,im.interviewer,ip.RedMarker,im.candidateName,im.candidateEmail,ip.ResumeLink as ResumeLink 
from InterviewerProfile ip 
join UserList ul on ul.id=ip.UserId 
join InterviewMeetings im on im.interviewer=ul.EmailId 
 where im.interviewDate>GETDATE() and  ip.RedMarker>0
End
GO