IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_FetchRBInterviewerDetails'))
BEGIN
    DROP PROCEDURE dbo.Usp_FetchRBInterviewerDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure Usp_FetchRBInterviewerDetails
@Userid int,                    
@flag int                    
AS                    
BEGIN                    
If (@flag=2)                    
 begin                    
  ;with CTE as(Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points as PrePoints,
  Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
  Case When ti.Surcharge>0 and ir.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and ir.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
  Case When cfd.Rating=5 and ir.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
  Case When (clr.StarRating=5 or clrs.Rating=5) and ir.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints,(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,
  isNull(im.interviewdate,ip.Date) as Date, ic.VideoDuration,Case When ir.IsNewSkillChannel=1 then pm.DurationSelectedMin else ir.interviewDuration end as MaxVideoDuration,
  ic.MeetingStatus,ir.JobCountry as  PreferredLocation,ic.InterviewStatus,CASE WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=0 THEN IsNull(ti.PerSuccessRate,0) WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=1 THEN IsNull(tei.PerSuccessRate,ti.PerSuccessRate) ELSE '' END as Rate           
  from InterviewPoints ip left join InterviewMeetings im on im.id = ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId left join Client cl on cl.id=ir.Clientid                    
  left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=@UserId left join TransEmergencyInterviewer tei on tei.IRFId=ir.id and tei.UserId=@UserId                     
  left join TransQualityController tqi on tqi.IRFId=ir.id and tqi.UserId=@UserId            
  left join UserList ipul on ipul.EmailId=ip.Comment
  --left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
  left join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id
  left join InterviewRatings clr on clr.Interviewid=im.id 
  left join ClientCommentsForReport clrs on clrs.Interviewid=im.id  
  left join PlanTypeChargesMaster pm on pm.CountryType=ir.JobCountry and pm.CompanyType=ir.CompanyType and pm.PlanTypeEnumValue=ir.Heuristicsscore
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Advance') and ip.Interviewid!=0                    
  Union                     
  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points as PrePoints,
  Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
  0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,
  isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,null as MaxVideoDuration,                  
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,ISNULL(ti.PerSuccessRate,0) as Rate                     
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                     
  left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=ir.id and ti.UserId=@UserId              
  left join UserList ipul on ipul.EmailId=ip.Comment             
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Advance') and ip.Interviewerid!=0                  
  Union                     
  Select ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points as PrePoints,
  Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
  0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,
  isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,null as MaxVideoDuration,                    
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,ISNULL(ti.PerSuccessRate,0) as Rate                     
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                     
  left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=ir.id and ti.UserId=@UserId                     
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Advance') and ip.Interviewerid=0  and ip.Interviewid=0)
  Select id,InterviewId,Type,Comment,Status,PrePoints,Penaltyamount,(Points+Isnull(Surcharge,0)+Isnull(CandidateRatingAmount,0)+Isnull(ClientRatingAmount,0)+Isnull(QCPoints,0)+ISNULL(VideoReward,0)-ISNULL(PaymentSLA,0)) as Points,
  Surcharge,CandidateRatingAmount,ClientRatingAmount,QCPoints,PaymentSLA,VideoReward,
  Date, VideoDuration,MaxVideoDuration,                
  MeetingStatus,PreferredLocation,InterviewStatus,Rate From CTE
 End                    
Else                    
 Begin                    
  ;With CTE as(Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points as PrePoints,
  Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
  Case When ti.Surcharge>0 and ir.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and ir.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
  Case When cfd.Rating=5 and ir.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
  Case When (clr.StarRating=5 or clrs.Rating=5) and ir.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints,
  (ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,
  isNull(im.interviewdate,ip.Date) as Date, ic.VideoDuration,Case When ir.IsNewSkillChannel=1 then pm.DurationSelectedMin else ir.interviewDuration end as MaxVideoDuration,
  ic.MeetingStatus,ir.JobCountry as  PreferredLocation,ic.InterviewStatus,CASE WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=0 THEN IsNull(ti.PerSuccessRate,0) WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=1 THEN IsNull(tei.PerSuccessRate,ti.PerSuccessRate) ELSE '' END as Rate                     
  from InterviewPoints ip left join InterviewMeetings im on im.id = ip.InterviewId           
  left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId left join Client cl on cl.id=ir.Clientid                    
  left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=@UserId                     
  left join TransEmergencyInterviewer tei on tei.IRFId=ir.id and tei.UserId=@UserId                     
  left join TransQualityController tqi on tqi.IRFId=ir.id and tqi.UserId=@UserId              
  left join UserList ipul on ipul.EmailId=ip.Comment           
  --left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
  left join CandidateFeedbackDetails cfd on cfd.Interviewid=im.id
  left join InterviewRatings clr on clr.Interviewid=im.id 
  left join ClientCommentsForReport clrs on clrs.Interviewid=im.id  
  left join PlanTypeChargesMaster pm on pm.CountryType=ir.JobCountry and pm.CompanyType=ir.CompanyType and pm.PlanTypeEnumValue=ir.Heuristicsscore
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Advance') and ip.Interviewid!=0                    
  union                     
  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points as PrePoints,
  Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
  0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,
  isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,null as MaxVideoDuration,                   
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,IsNull(ti.PerSuccessRate,0) as Rate                     
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId                     
  left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                     
  left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=ir.id and ti.UserId=@UserId             
  left join UserList ipul on ipul.EmailId=ip.Comment          
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Advance') and ip.Interviewerid!=0                    
  union                     
  Select ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points as PrePoints,
  Case When ip.Points>=0 then (ip.Points*IsNull(ip.PenaltyAmount,0)/100) else 0 end as Penaltyamount,Case When ip.Points>=0 then (ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) else ip.Points end as Points,
  0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints,(ip.Points*IsNull(ip.PaymentSLA,0)/100) as PaymentSLA,(ip.Points*IsNull(ip.VideoReward,0)/100) as VideoReward,
  isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,null as MaxVideoDuration,                  
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,IsNull(ti.PerSuccessRate,0) as Rate                     
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId                     
  left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                     
  left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=ir.id and ti.UserId=@UserId                     
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Advance') and ip.Interviewerid=0  and ip.Interviewid=0)
  Select id,InterviewId,Type,Comment,Status,PrePoints,Penaltyamount,(Points+Isnull(Surcharge,0)+Isnull(CandidateRatingAmount,0)+Isnull(ClientRatingAmount,0)+Isnull(QCPoints,0)+ISNULL(VideoReward,0)-ISNULL(PaymentSLA,0)) as Points,
  Surcharge,CandidateRatingAmount,ClientRatingAmount,QCPoints,PaymentSLA,VideoReward, 
  Date, VideoDuration,MaxVideoDuration,                
  MeetingStatus,PreferredLocation,InterviewStatus,Rate From CTE
 End                    
                    
END      
  
GO

