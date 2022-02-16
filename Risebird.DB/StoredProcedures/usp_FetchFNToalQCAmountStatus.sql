IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchFNToalQCAmountStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchFNToalQCAmountStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_FetchFNToalQCAmountStatus
@st datetime,
@se datetime
AS
BEGIN
;with CTE(paymentid,Comment,id, Points,MeetingStatus) as
(Select ip.id as paymentid,ul.EmailId as Comment,ul.id,ip.Points,ic.MeetingStatus 
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join InterviewMeetings im on im.id = ip.InterviewId
left join InterviewComments ic on ic.Interviewid=im.id  where (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Paid')
and ip.Type='Quality Controller' and ip.interviewid!=0 and (isNull(im.interviewdate,ip.Date)>=@st 
and isNull(im.interviewdate,ip.Date)<=@se) 
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,ip.Points,ic.MeetingStatus 
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.Interviewid=im.id
where (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Paid') and ip.Type='Quality Controller' 
and ip.interviewerid!=0 and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se)) 
Select Comment,id,MeetingStatus,Sum(Points) as Points from CTE group by Comment,id,MeetingStatus
END

GO
