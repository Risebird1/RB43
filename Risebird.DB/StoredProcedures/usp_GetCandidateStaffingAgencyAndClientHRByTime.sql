  
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateStaffingAgencyAndClientHRByTime'))
BEGIN
    DROP PROCEDURE [dbo].[usp_GetCandidateStaffingAgencyAndClientHRByTime]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_GetCandidateStaffingAgencyAndClientHRByTime]  
@stdate datetime =null,
@etdate datetime =null
AS  
Begin  
select distinct a.EmailId,a.Total,IRFId from (select ca.ProfileSource as EmailId,Count(ca.id) as Total,IRF.IRFId from InterviewMeetings im   
 join IRF on irf.IRFId =im.IRFId  
 join UserList ul on ul.EmailId = im.candidateEmail  
 join CandidatesMaster cm on cm.UserId = ul.id  
 join Candidate ca on ca.CandidateMasterId =cm.id and ca.IRFId =irf.id  
 where im.interviewDate>=@stdate and im.interviewDate<@etdate
 group by ca.ProfileSource,IRF.IRFId
 union  
 select ca.ClientHRAttemptContact as EmailId,Count(ca.id) as Total,IRF.IRFId from InterviewMeetings im   
 join IRF on irf.IRFId =im.IRFId  
 join UserList ul on ul.EmailId = im.candidateEmail  
 join CandidatesMaster cm on cm.UserId = ul.id  
 join Candidate ca on ca.CandidateMasterId =cm.id and ca.IRFId =irf.id  
 where im.interviewDate>=@stdate and im.interviewDate<@etdate group by ca.ClientHRAttemptContact,IRF.IRFId) a  
 where EmailId is not null 
  
End  
GO