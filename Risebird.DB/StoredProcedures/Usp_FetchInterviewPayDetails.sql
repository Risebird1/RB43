
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_FetchInterviewPayDetails'))
BEGIN
    DROP PROCEDURE dbo.Usp_FetchInterviewPayDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure Usp_FetchInterviewPayDetails    
@Emailid varchar(200)                      
As                      
BEGIN                      
Declare @Userid int                      
Select @Userid=id from UserList where LOWER(EmailId)=LOWER(@Emailid)
;with CTE as (Select distinct  ip.id,ip.InterviewId,ip.Type,im.candidateName as Comment,ip.Status,ip.Points as PrePoints,          
Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,          
Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,ir.JobCountry as  PreferredLocation,ic.VideoLink, ic.VideoDuration
,IsNull(ti.PerSuccessRate,ip.Points) as Rate,ip.Comment as CrossCheckvalue,            
Case When ti.Surcharge>0 and ir.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and ir.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,            
0 as CandidateRatingAmount,            
0 as ClientRatingAmount,ip.QCPoints,          
(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,  
ic.AITranscriptVerdict,ic.AudioTranscriptFileLink,ic.AIVerdict,ir.id as IRFinid,ISNULL(ir.InterviewerBillingCycle,0) as InterviewerBillingCycle,
ISNULL(ip.PaymentHistoryId,0) as PaymentHistoryId,ph.TransferUtr,COALESCE(ph.TransactionDate, ph.PaymentDate) as TransactionDate,ph.Comment as PaymentComment,ph.PayoutStatus
from InterviewPoints ip                       
Left join InterviewMeetings im on im.id=ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId                
left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=ip.userid
left join PaymentHistory ph on ph.id = ip.PaymentHistoryId
where ip.userid=@Userid and ip.Interviewid!=0 and ISNULL(ir.IsNewSkillChannel,0)=1                    
union            
Select distinct  ip.id,ip.InterviewId,ip.Type,im.candidateName as Comment,ip.Status,ip.Points as PrePoints,          
Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,          
Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,ir.JobCountry as  PreferredLocation,ic.VideoLink, ic.VideoDuration
,IsNull(ti.PerSuccessRate,ip.Points) as Rate,ip.Comment as CrossCheckvalue,            
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,  
ic.AITranscriptVerdict,ic.AudioTranscriptFileLink,ic.AIVerdict,ir.id as IRFinid,ISNULL(ir.InterviewerBillingCycle,0) as InterviewerBillingCycle,
ISNULL(ip.PaymentHistoryId,0) as PaymentHistoryId,ph.TransferUtr,COALESCE(ph.TransactionDate, ph.PaymentDate) as TransactionDate,ph.Comment as PaymentComment,ph.PayoutStatus
from InterviewPoints ip                       
Left join InterviewMeetings im on im.id=ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId                
left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=ip.userid
left join PaymentHistory ph on ph.id = ip.PaymentHistoryId
where ip.userid=@Userid and ip.Interviewid!=0 and ISNULL(ir.IsNewSkillChannel,0)=0                    
union                    
Select distinct ip.id,ip.InterviewId,ip.Type,im.candidateName as Comment,ip.Status,ip.Points as PrePoints,          
Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,          
Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,'' as PreferredLocation,ic.VideoLink, '' as VideoDuration,0 as Rate,ip.Comment as CrossCheckvalue,0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,  
'' as AITranscriptVerdict,'' as AudioTranscriptFileLink, '' as AIVerdict,0 as IRFinid,0 as InterviewerBillingCycle,
ISNULL(ip.PaymentHistoryId,0) as PaymentHistoryId,ph.TransferUtr,COALESCE(ph.TransactionDate, ph.PaymentDate) as TransactionDate,ph.Comment as PaymentComment,ph.PayoutStatus
from InterviewPoints ip                       
Left join UnapprovedInterviewerMeetings im on im.id=ip.InterviewId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId
left join PaymentHistory ph on ph.id = ip.PaymentHistoryId
where ip.userid=@Userid and ip.Interviewerid!=0                        
union                   
Select distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points as PrePoints,          
Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,          
Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,isNull(ip.Date,'') as Date,0 as MeetingStatus,0 as InterviewStatus,'' as PreferredLocation,'' as VideoLink, '' as VideoDuration,0 as Rate,ip.Comment as CrossCheckvalue,            
0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,  
'' as AITranscriptVerdict,'' as AudioTranscriptFileLink,'' as AIVerdict,0 as IRFinid,0 as InterviewerBillingCycle,
ISNULL(ip.PaymentHistoryId,0) as PaymentHistoryId,ph.TransferUtr,COALESCE(ph.TransactionDate, ph.PaymentDate) as TransactionDate,ph.Comment as PaymentComment,ph.PayoutStatus
from InterviewPoints ip
left join PaymentHistory ph on ph.id = ip.PaymentHistoryId
where ip.userid=@Userid and ip.Interviewerid=0 and ip.Interviewid=0)            
select id,InterviewId,Type,Comment,Status,(PrePoints+IsnUll(Surcharge,0)) as PrePoints,Penaltyamount,(Points+Isnull(Surcharge,0)+ISNULL(CandidateRatingAmount,0)+ISNULL(ClientRatingAmount,0)+ISNULL(QCPoints,0)+ISNULL(VideoReward,0)-ISNULL(PaymentSLA,0)) as Points,            
Date,MeetingStatus,InterviewStatus,PreferredLocation,VideoLink,VideoDuration,Rate,CrossCheckvalue,Surcharge,CandidateRatingAmount,ClientRatingAmount,QCPoints,PaymentSLA,VideoReward,AITranscriptVerdict,AudioTranscriptFileLink,AIVerdict,IRFinid,InterviewerBillingCycle,PaymentHistoryId,TransferUtr,TransactionDate,PaymentComment,PayoutStatus
From CTE            
END   
  GO
