IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FetchInterviewerPayoutDetailsAdvance'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchInterviewerPayoutDetailsAdvance
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchInterviewerPayoutDetailsAdvance  
AS  
BEGIN  
;with CTE as(Select  Distinct ip.id, (ul.FirstName+ul.LastName) as FirstName,ip.Userid,ul.EmailId as InterviewerEmailid,  
(ip.Points) as Points  
from InterviewPoints ip join UserList ul on ul.id=ip.userid   
left join InterviewMeetings im on im.id = ip.InterviewId   
where ip.Interviewid!=0  and ip.Status='Advance'  
Union   
Select  Distinct ip.id,(ul.FirstName+ul.LastName) as FirstName,ip.Userid,ul.EmailId as InterviewerEmailid,  
(ip.Points) as Points  
from InterviewPoints ip join UserList ul on ul.id=ip.userid   
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId  
where ip.Interviewerid!=0  and ip.Status='Advance'   
Union  
Select  Distinct ip.id, (ul.FirstName+ul.LastName) as FirstName,ip.Userid,ul.EmailId as InterviewerEmailid,  
(ip.Points) as Points  
from InterviewPoints ip join UserList ul on ul.id=ip.userid  
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId   
where ip.Interviewerid=0 and ip.Interviewid=0 and ip.Status='Advance')  
Select distinct FirstName,Userid,InterviewerEmailid,sum(Points) as Points from CTE  
Group by FirstName,Userid,InterviewerEmailid  
END
GO