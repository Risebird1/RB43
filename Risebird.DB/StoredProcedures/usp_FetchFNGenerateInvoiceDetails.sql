IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchFNGenerateInvoiceDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchFNGenerateInvoiceDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FetchFNGenerateInvoiceDetails
@st datetime,
@se datetime
AS
BEGIN
Declare @SubmitedPoints Table(
Comment varchar(200),
id int,
Points int
)
Declare @AdvancedPoints Table(
Comment varchar(200),
id int,
Points int
)

;with CTE as 
(Select ip.id as paymentid,ul.EmailId as Comment,ul.id,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
Case When ti.Surcharge>0 and ir.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and ir.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
Case When cr.Question2=5 and ir.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
Case When (clr.StarRating=5 or clrs.Rating=5) and ir.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints,
(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
left join InterviewMeetings im on im.id = ip.InterviewId 
left join IRF ir on ir.IRFId=im.IRFId
left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
left join InterviewRatings clr on clr.Interviewid=im.id 
left join ClientCommentsForReport clrs on clrs.Interviewid=im.id  
left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=ip.userid
where (ip.Status='Submitted' or ip.Status='Pending') and ip.interviewid!=0 and (isNull(im.interviewdate,ip.Date)>=@st 
and isNull(im.interviewdate,ip.Date)<=@se)
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,
(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId 
where (ip.Status='Submitted' or ip.Status='Pending') and interviewerid!=0 and (isNull(im.interviewdate,ip.Date)>=@st 
and isNull(im.interviewdate,ip.Date)<=@se) 
Union 
Select ip.id as paymentid,ul.EmailId as Comment,ul.id,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,
(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
where (ip.Status='Submitted' or ip.Status='Pending') and interviewerid=0 and Interviewid=0 and (ip.Date>=@st and ip.Date<=@se))
Insert into @SubmitedPoints
Select Comment,id,Sum(Points+Isnull(Surcharge,0)+ISNULL(CandidateRatingAmount,0)+ISNULL(ClientRatingAmount,0)+ISNULL(QCPoints,0)+ISNULL(VideoReward,0)-ISNULL(PaymentSLA,0)) as Points from CTE group by Comment,id

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
Insert into @AdvancedPoints
Select Comment,id,Sum(Points) as Points from CTE group by Comment,id

Select sp.id,(Isnull(sp.Points,0)-Isnull(ap.Points,0)) as Points,sp.Comment From @SubmitedPoints sp left join @AdvancedPoints ap on sp.id=ap.id
END
GO