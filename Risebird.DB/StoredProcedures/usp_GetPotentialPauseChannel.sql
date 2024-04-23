IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetPotentialPauseChannel'))
BEGIN
    DROP PROCEDURE dbo.usp_GetPotentialPauseChannel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetPotentialPauseChannel]   
AS    
Begin
;with CTE as(
SELECT i1.IRFId
FROM InterviewMeetings i1
WHERE NOT EXISTS (
    SELECT i2.IRFId
    FROM InterviewMeetings i2
    WHERE i2.IRFId = i1.IRFId
    AND i2.InterviewDate BETWEEN DATEADD(DAY, -7, CAST(GETDATE() AS DATE)) AND CAST(GETDATE() AS DATE) group by i2.IRFId
) group by i1.IRFId)
select CTE.IRFId,ul.EmailId as ChannelOwner,(select count(im.candidateEmail) from InterviewMeetings im where im.IRFId=CTE.IRFId group by im.IRFId) TotalCandidate
,(select top 1 im.interviewDate from InterviewMeetings im where im.IRFId=CTE.IRFId  order by interviewDate desc) interviewDate
,(select top 1 im.candidateEmail from InterviewMeetings im where im.IRFId=CTE.IRFId  order by interviewDate desc) candidateEmail
,IRF.JDTiltle
,(Select ul.EmailId From IRF join UserList ul on ul.id=IRF.BillingOwner where IRFId=CTE.IRFId) BillingOwner
,(select Count(ic.MeetingStatus) from InterviewMeetings im join [InterviewComments] ic on im.id= ic.InterviewId where im.IRFId=CTE.IRFId and ic.MeetingStatus in(3,6,7)) TotalCompletedInterview
,IRF.id as irfIntId,IRF.IsPaused
from CTE
Join IRF on CTE.IRFId=IRF.IRFId
Join UserList ul on ul.id  = irf.RisebirdHr
--join UserList ulc on ul.id=IRF.BillingOwner
End
Go