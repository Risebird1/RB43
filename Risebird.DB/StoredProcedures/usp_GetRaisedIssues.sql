IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRaisedIssues'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRaisedIssues
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

  
Create Procedure usp_GetRaisedIssues  
@profileName varchar(50)  
AS  
BEGIN  
If(@profileName='TechSupport')  
Begin  
Select rs.*,ul.MobileNumber,ul.FirstName as Name,IRF.id as irfIntId,im.candidateEmail,im.Interviewer,
    CAST(CASE WHEN EXISTS (SELECT 1 FROM RaisedIssuesHistory rih WHERE rih.RaisedIssueId = rs.id AND rih.Action = 'Reopened') THEN 1 ELSE 0 END AS bit) AS IsReopened
from RaisedIssues rs left join UserList ul on ul.EmailId=rs.RequestedBy 
left join InterviewMeetings im  on im.Id=rs.interviewId
left join IRF on IRF.IRFID=im.IRFId
where rs.Requester not in('Interviewer','Client') order by RequestedOn  desc  
END  
Else If(@profileName='Admin')  
Begin  
Select rs.*,ul.MobileNumber,ul.FirstName as Name,IRF.id as irfIntId,im.candidateEmail,im.Interviewer,
    CAST(CASE WHEN EXISTS (SELECT 1 FROM RaisedIssuesHistory rih WHERE rih.RaisedIssueId = rs.id AND rih.Action = 'Reopened') THEN 1 ELSE 0 END AS bit) AS IsReopened
from RaisedIssues rs left join UserList ul on ul.EmailId=rs.RequestedBy 
left join InterviewMeetings im  on im.Id=rs.interviewId
left join IRF on IRF.IRFID=im.IRFId
order by RequestedOn  desc  
End  
Else  
Begin  
Select rs.*,ul.MobileNumber,ul.FirstName as Name,IRF.id as irfIntId,im.candidateEmail,im.Interviewer,
    CAST(CASE WHEN EXISTS (SELECT 1 FROM RaisedIssuesHistory rih WHERE rih.RaisedIssueId = rs.id AND rih.Action = 'Reopened') THEN 1 ELSE 0 END AS bit) AS IsReopened
from RaisedIssues rs left join UserList ul on ul.EmailId=rs.RequestedBy 
left join InterviewMeetings im  on im.Id=rs.interviewId
left join IRF on IRF.IRFID=im.IRFId
where rs.Requester in('Interviewer','Client') order by RequestedOn  desc  
END  
END   
GO
