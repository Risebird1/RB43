IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchFNToalAmountStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchFNToalAmountStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_FetchFNToalAmountStatus
@st datetime,
@se datetime
AS
BEGIN
with CTE as 
(Select ip.id as paymentid,ul.EmailId as Comment,ul.id,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) as Points,ic.MeetingStatus,
Case When ti.Surcharge>0 and ir.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and ir.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
Case When cr.Question2=5 and ir.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
Case When (clr.StarRating=5 or clrs.Rating=5) and ir.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join InterviewMeetings im on im.id = ip.InterviewId 
left join InterviewComments ic on ic.Interviewid=im.id  
left join IRF ir on ir.IRFId=im.IRFId
left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
left join InterviewRatings clr on clr.Interviewid=im.id 
left join ClientCommentsForReport clrs on clrs.Interviewid=im.id  
left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=ip.userid
where (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Paid') and ip.Type='Interview' and ip.interviewid!=0 and 
(isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se)
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) as Points,ic.MeetingStatus,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.Interviewid=im.id
where (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Paid') and ip.Type='Interview' and ip.interviewerid!=0 
and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se))
Select Comment,id,MeetingStatus,Sum(Points+Isnull(Surcharge,0)+ISNULL(CandidateRatingAmount,0)+ISNULL(ClientRatingAmount,0)+ISNULL(QCPoints,0)) as Points from CTE 
where MeetingStatus=1 or MeetingStatus=3 or MeetingStatus=4 
group by Comment,id,MeetingStatus
END
GO