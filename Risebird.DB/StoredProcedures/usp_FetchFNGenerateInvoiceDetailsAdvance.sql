IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchFNGenerateInvoiceDetailsAdvance'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchFNGenerateInvoiceDetailsAdvance
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_FetchFNGenerateInvoiceDetailsAdvance
AS
BEGIN
;with CTE(paymentid,Comment,id, Points) as 
(Select ip.id as paymentid,ul.EmailId as Comment,ul.id,ip.Points 
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join InterviewMeetings im on im.id = ip.InterviewId 
where ip.Status='Advance' and interviewid!=0 
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,ip.Points 
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId 
where ip.Status='Advance' and interviewerid!=0  
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,ip.Points 
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
where ip.Status='Advance' and interviewerid=0 and Interviewid=0)
Select Comment,id,Sum(Points) as Points from CTE group by Comment,id
END

go