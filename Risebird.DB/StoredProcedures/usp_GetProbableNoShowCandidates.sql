/****** Object:  StoredProcedure [dbo].[usp_GetProbableNoShowCandidates]   ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetProbableNoShowCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetProbableNoShowCandidates
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
CREATE PROCEDURE [dbo].[usp_GetProbableNoShowCandidates]    

AS    
Begin   
  select ul.EmailId as candidateEmail, im.interviewDate, im.IRFId,ic.MeetingStatus,ic.InterviewStatus 
from ProbableNoShowCandidates pnc
join InterviewMeetings im on im.id = pnc.InterviewId
join UserList ul on ul.id = pnc.CandidateUserId
join InterviewComments ic on ic.Interviewid=im.id
order by im.interviewDate desc
End  
GO

