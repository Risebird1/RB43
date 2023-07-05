IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchFNToalAmountPaid'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchFNToalAmountPaid
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_FetchFNToalAmountPaid
@st datetime,
@se datetime
AS
BEGIN
;with CTE as 
(Select ip.id as paymentid,ul.EmailId as Comment,ul.id,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) as Points,
Case When ti.Surcharge>0 and ir.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and ir.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
Case When cr.Question2=5 and ir.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
Case When (clr.StarRating=5 or clrs.Rating=5) and ir.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join InterviewMeetings im on im.id = ip.InterviewId
left join IRF ir on ir.IRFId=im.IRFId
left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
left join InterviewRatings clr on clr.Interviewid=im.id 
left join ClientCommentsForReport clrs on clrs.Interviewid=im.id  
left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=ip.userid
where ip.Status='Paid' and ip.interviewid!=0 and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se) 
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) as Points,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId 
where ip.Status='Paid' and (Type='Interview' or Type='Quality Controller') and interviewerid!=0 
and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se) 
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) as Points,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
where ip.Status='Paid' and (Type='Interview' or Type='Quality Controller') and interviewerid=0 and Interviewid=0 and 
(ip.Date>=@st and ip.Date<=@se)) 
Select paymentid,Comment,id,(Points+Isnull(Surcharge,0)+ISNULL(CandidateRatingAmount,0)+ISNULL(ClientRatingAmount,0)+ISNULL(QCPoints,0)) as Points  from CTE
END
GO