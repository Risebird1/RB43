IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchAudioComment'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchAudioComment
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchAudioComment
@interviewId int
as 
begin
Select irf.AudioComment,irf.CompanyDetails,im.zoomLink,im.interviewDate,irf.JDLink,irf.JobLevel,im.candidateEmail,im.MobileNumber,im.id,im.candidateName,irf.IRFID,cf.FormURL as ConsentFormUrl,ca.PreReportLink,ca.PreReportUsername,ca.PreReportPassword,IRF.Heuristicsscore 
from InterviewMeetings im                   
 join IRF  irf on im.IRFId=irf.IRFId Left join ConsentForms cf on cf.IRFId=IRF.id
 join UserList ul on ul.EmailId=im.candidateEmail
 join CandidatesMaster cm on cm.Userid=ul.id
 join Candidate ca on ca.CandidateMasterId=cm.id and ca.IRFId=IRF.id
 where im.id=@interviewId
 End
GO