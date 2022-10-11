IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetUpcomingInterviewsCandidates'))
BEGIN
    DROP PROCEDURE dbo.usp_GetUpcomingInterviewsCandidates
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetUpcomingInterviewsCandidates
AS
BEGIN
Select ul.uniqueid,im.candidateEmail as EmailId,im.candidateName as Name,IRF.JobCountry from InterviewMeetings im join IRF on IRF.IRFId=im.IRFId 
join UserList ul on im.candidateEmail=ul.EmailId left join AadhaarDetails ad on im.candidateEmail=ad.email 
where interviewDate>=dateadd(minute,330,GetDate()) and interviewDate<dateadd(minute,390,GetDate()) 
and ad.id is null and IRF.IsAadhaarVerificationRequired=1

END
Go