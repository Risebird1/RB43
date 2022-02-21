IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchFNGenerateInvoiceDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchFNGenerateInvoiceDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_FetchFNGenerateInvoiceDetails
@st datetime,
@se datetime
AS
BEGIN
;with CTE(paymentid,Comment,id, Points) as 
(Select ip.id as paymentid,ul.EmailId as Comment,ul.id,ip.Points 
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
left join InterviewMeetings im on im.id = ip.InterviewId 
where (ip.Status='Submitted' or ip.Status='Pending') and interviewid!=0 and (isNull(im.interviewdate,ip.Date)>=@st 
and isNull(im.interviewdate,ip.Date)<=@se)
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,ip.Points 
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId 
where (ip.Status='Submitted' or ip.Status='Pending') and interviewerid!=0 and (isNull(im.interviewdate,ip.Date)>=@st 
and isNull(im.interviewdate,ip.Date)<=@se) 
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,ip.Points 
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
where (ip.Status='Submitted' or ip.Status='Pending') and interviewerid=0 and Interviewid=0 and (ip.Date>=@st and ip.Date<=@se))
Select Comment,id,Sum(Points) as Points from CTE group by Comment,id
END
GO