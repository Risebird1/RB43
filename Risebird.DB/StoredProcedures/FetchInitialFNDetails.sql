IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.FetchInitialFNDetails'))
BEGIN
    DROP PROCEDURE dbo.FetchInitialFNDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure FetchInitialFNDetails
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

;With CTE as(Select Distinct ul.EmailId as Comment,ul.id,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
Case When ti.Surcharge>0 and IRF.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and IRF.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
--Case When cr.Question2=5 and IRF.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
Case When (clr.StarRating=5 or clrs.Rating=5) and IRF.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints,
(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward
from InterviewPoints ip join UserList ul on ul.id=ip.userid 
left join InterviewMeetings im on im.id=ip.Interviewid
left join IRF on IRF.IRFId=im.IRFId
--left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
left join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id
left join InterviewRatings clr on clr.Interviewid=im.id 
left join ClientCommentsForReport clrs on clrs.Interviewid=im.id  
left join transInterviewer ti on ti.IRFId=IRF.id and ip.Userid=ti.Userid
where ip.Status='Submitted')
Insert into @SubmitedPoints Select Comment,id,Sum(Points+Isnull(Surcharge,0)+ISNULL(ClientRatingAmount,0)+ISNULL(QCPoints,0)+ISNULL(VideoReward,0)-ISNULL(PaymentSLA,0)) as Points From CTE
group by Comment,id

Insert into @AdvancedPoints
Select Distinct ul.EmailId as Comment,ul.id,Sum(ip.Points) as Points 
from InterviewPoints ip join UserList ul on ul.id=ip.userid
where ip.Status='Advance' group by ul.EmailId,ul.id

Select sp.id,(Isnull(sp.Points,0)-Isnull(ap.Points,0)) as Points,sp.Comment From @SubmitedPoints sp left join @AdvancedPoints ap on sp.id=ap.id order by id
END
GO