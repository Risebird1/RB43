IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchAllFNDetail'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchAllFNDetail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_FetchAllFNDetail
AS
BEGIN
;with CTE as(
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,(ul.FirstName+ul.LastName) as FirstName,ip.Status,ip.Userid,
ip.Points as PrePoints,Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,
Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,
irf.JobCountry as  PreferredLocation,ul.EmailId as InterviewerEmailid,IRF.ClientName,ic.VideoLink, ic.VideoDuration,uli.Emailid as ChannelOwner,
CASE WHEN ip.Type='Interview' and im.IsEmergency=0 THEN IsNull(ti.PerSuccessRate,ip.Points) 
WHEN ip.Type='Interview' and im.IsEmergency=1 THEN IsNull(tei.PerSuccessRate,ip.Points) 
WHEN ip.Type='Quality Controller' THEN IsNull(tqi.PerSuccessRate,ip.Points) ELSE '' END as Rate,IRF.PrimarySkillInvite,im.IRFId,im.IsEmergency,Case When IRF.IsNewSkillChannel=1 then pm.DurationSelectedMin else IRF.interviewDuration end as MaxVideoDuration,
Case When ti.Surcharge>0 and IRF.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and IRF.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
--Case When cr.Question2=5 and IRF.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
Case When cfd.Rating=5 and IRF.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
Case When (clr.StarRating=5 or clrs.Rating=5) and IRF.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints,
(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join InterviewMeetings im on im.id = ip.InterviewId 
left join InterviewComments ic on ic.InterviewId=im.id join IRF on IRF.IRFID=im.IRFId left join Client cl on cl.id=IRF.ClientId left join Userlist uli on uli.id=irf.risebirdhr 
left join TransInterviewer ti on ti.IRFId=irf.id and ti.UserId=ip.Userid left join TransEmergencyInterviewer tei on tei.IRFId=irf.id and tei.UserId=ip.Userid 
left join TransQualityController tqi on tqi.IRFId=irf.id and tqi.UserId=ip.Userid 
--left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
left join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id
left join InterviewRatings clr on clr.Interviewid=im.id 
left join ClientCommentsForReport clrs on clrs.Interviewid=im.id  
left join PlanTypeChargesMaster pm on pm.CountryType=IRF.JobCountry and pm.CompanyType=IRF.CompanyType and pm.PlanTypeEnumValue=IRF.Heuristicsscore
where ip.Status='Submitted' and ip.Interviewid!=0 
Union 
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,(ul.FirstName+ul.LastName) as FirstName,ip.Status,ip.Userid,
ip.Points as PrePoints,Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,
Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,
'' as PreferredLocation,ul.EmailId as InterviewerEmailid ,'Interviewer' as ClientName,ic.VideoLink, null as VideoDuration,
uli.Emailid as ChannelOwner,IsNull(ti.PerSuccessRate,ip.Points) as Rate,'' as PrimarySkillInvite,im.ISCId as IRFId,0 as IsEmergency,null as MaxVideoDuration,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,
(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId 
left join InterviewerComments ic on ic.InterviewId=im.id join InterviewerSkillsChannel  irf on IRF.InterviewerSkillId=im.ISCId 
left join Userlist uli on uli.id=irf.risebirdhr left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=irf.id and ti.UserId=ip.Userid 
where ip.Status='Submitted' and ip.Interviewerid!=0 
Union 
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,(ul.FirstName+ul.LastName) as FirstName,ip.Status,ip.Userid,
ip.Points as PrePoints,Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,
Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,
'' as PreferredLocation,ul.EmailId as InterviewerEmailid ,'Interviewer' as ClientName,ic.VideoLink, null as VideoDuration,
uli.Emailid as ChannelOwner,0 as Rate,'' as PrimarySkillInvite,'' as IRFId,0 as IsEmergency,null as MaxVideoDuration,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,
(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId 
left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel  irf on IRF.InterviewerSkillId=im.ISCId 
left join Userlist uli on uli.id=irf.risebirdhr where ip.Status='Submitted' and ip.Interviewerid=0 and ip.Interviewid=0)
Select Distinct id,InterviewId,Type,Comment,FirstName,Status,Userid,PrePoints,Penaltyamount,
(Points+Isnull(Surcharge,0)+ISNULL(CandidateRatingAmount,0)+ISNULL(ClientRatingAmount,0)+ISNULL(QCPoints,0)+ISNULL(VideoReward,0)-ISNULL(PaymentSLA,0)) as Points,
candidateEmail,candidateName,MeetingStatus,InterviewStatus,Date,PreferredLocation,InterviewerEmailid,ClientName,VideoLink,VideoDuration,
ChannelOwner,Rate,PrimarySkillInvite,IRFId,IsEmergency,MaxVideoDuration,Surcharge,CandidateRatingAmount,ClientRatingAmount,QCPoints,PaymentSLA,VideoReward  
From CTE
END

GO