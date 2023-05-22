IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchFNAllGeneratedInvoiceDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchFNAllGeneratedInvoiceDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_FetchFNAllGeneratedInvoiceDetails    
@st datetime,    
@se datetime    
AS    
BEGIN    
;with CTE as(
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,(ul.FirstName+ul.LastName) as FirstName,ip.Status,ip.Userid,    
ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,    
irf.JobCountry as  PreferredLocation,ul.EmailId as InterviewerEmailid,IRF.ClientName,ic.VideoLink, ic.VideoDuration,uli.Emailid as ChannelOwner,    
CASE WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=0 THEN IsNull(ti.PerSuccessRate,0) WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=1 THEN IsNull(tei.PerSuccessRate,ti.PerSuccessRate)     
ELSE '' END as Rate,IRF.PrimarySkillInvite,im.IRFId,im.IsEmergency,IRF.InterviewDuration as MaxVideoDuration,IRF.IsFixedInterviewduration,
Case When ti.Surcharge>0 and IRF.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and IRF.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
Case When cr.Question2=5 and IRF.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
Case When clr.StarRating=5 and IRF.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join InterviewMeetings im on im.id = ip.InterviewId     
left join InterviewComments ic on ic.InterviewId=im.id join IRF on IRF.IRFID=im.IRFId join Client cl on cl.id=IRF.ClientId   
left join Userlist uli on uli.id=irf.risebirdhr left join TransInterviewer ti on ti.IRFId=irf.id and ti.UserId=ip.Userid     
left join TransEmergencyInterviewer tei on tei.IRFId=irf.id and tei.UserId=ip.Userid     
left join TransQualityController tqi on tqi.IRFId=irf.id and tqi.UserId=ip.Userid   
left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
left join InterviewRatings clr on clr.Interviewid=im.id 
where ip.Interviewid!=0  and (ip.Status='Submitted' or ip.Status='Pending')  and (isNull(im.interviewdate,ip.Date)>=@st     
and isNull(im.interviewdate,ip.Date)<=@se)     
Union       
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,(ul.FirstName+ul.LastName) as FirstName,ip.Status,ip.Userid,    
ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points, im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,    
'' as PreferredLocation,ul.EmailId as InterviewerEmailid ,'Interviewer' as ClientName,ic.VideoLink, null as VideoDuration,    
uli.Emailid as ChannelOwner,IsNull(ti.PerSuccessRate,0) as Rate,'' as PrimarySkillInvite,im.ISCId as IRFId,0 as IsEmergency,null as MaxVideoDuration,0 as IsFixedInterviewduration,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId     
left join InterviewerComments ic on ic.InterviewId=im.id join InterviewerSkillsChannel  irf on IRF.InterviewerSkillId=im.ISCId     
left join Userlist uli on uli.id=irf.risebirdhr left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=irf.id and ti.UserId=ip.Userid      
where ip.Interviewerid!=0  and (ip.Status='Submitted' or ip.Status='Pending') and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se)      
union     
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,(ul.FirstName+ul.LastName) as FirstName,ip.Status,ip.Userid,    
ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,
im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,    
'' as PreferredLocation,ul.EmailId as InterviewerEmailid ,'Interviewer' as ClientName,ic.VideoLink, null as VideoDuration,    
uli.Emailid as ChannelOwner,0 as Rate,'' as PrimarySkillInvite,'' as IRFId,0 as IsEmergency,null as MaxVideoDuration,0 as IsFixedInterviewduration,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip join UserList ul on ul.id=ip.userid   left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId       
left join InterviewerComments ic on ic.InterviewId=im.id   left join InterviewerSkillsChannel  irf on IRF.InterviewerSkillId=im.ISCId     
left join Userlist uli on uli.id=irf.risebirdhr     
where ip.Interviewerid=0 and ip.Interviewid=0 and (ip.Status='Submitted' or ip.Status='Pending')     
and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se)   
union
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,ul.FirstName,ip.Status,ip.Userid,-ip.Points as PrePoints,0 as Penaltyamount,(-ip.Points) as Points,
im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,
'' as  PreferredLocation,ul.EmailId as InterviewerEmailid,IRF.ClientName,ic.VideoLink, ic.VideoDuration,
'' as ChannelOwner,0 as Rate,'' as PrimarySkillInvite,'' as IRFId,0 as IsEmergency,null as MaxVideoDuration,0 as IsFixedInterviewduration,
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join InterviewMeetings im on im.id = ip.InterviewId 
left join InterviewComments ic on ic.InterviewId=im.id left join IRF on IRF.IRFID=im.IRFId 
where ip.Status='Advance' )
Select Distinct id,InterviewId,Type,Comment,FirstName,Status,Userid,PrePoints,Penaltyamount,
(Points+Isnull(Surcharge,0)+ISNULL(CandidateRatingAmount,0)+ISNULL(ClientRatingAmount,0)+ISNULL(QCPoints,0)) as Points,
candidateEmail,candidateName,MeetingStatus,InterviewStatus,Date,PreferredLocation,InterviewerEmailid,ClientName,VideoLink,VideoDuration,
ChannelOwner,Rate,PrimarySkillInvite,IRFId,IsEmergency,MaxVideoDuration,IsFixedInterviewduration,Surcharge,CandidateRatingAmount,ClientRatingAmount,QCPoints 
From CTE
END
GO