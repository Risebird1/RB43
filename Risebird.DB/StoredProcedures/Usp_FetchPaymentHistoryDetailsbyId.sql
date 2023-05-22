    IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_FetchPaymentHistoryDetailsbyId'))
BEGIN
    DROP PROCEDURE dbo.Usp_FetchPaymentHistoryDetailsbyId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO      
CREATE Procedure Usp_FetchPaymentHistoryDetailsbyId               
@PaymentHistoryId int
AS                
BEGIN                
;with CTE as(  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,
  ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,isNull(im.interviewdate,ip.Date) as Date, ic.VideoDuration,              
  ic.MeetingStatus,ir.JobCountry as  PreferredLocation,ic.InterviewStatus,
  Case When ti.Surcharge>0 and ir.IsNewSkillChannel=1 and ip.Points>0 then ti.Surcharge when ti.Surcharge=0 and ir.IsNewSkillChannel=1 and ip.Points>0 then im.Surcharge Else 0 end as Surcharge,
  Case When cr.Question2=5 and ir.IsNewSkillChannel=1 then 20 Else 0 END as CandidateRatingAmount,
  Case When clr.StarRating=5 and ir.IsNewSkillChannel=1 then 50 Else 0 END as ClientRatingAmount,ip.QCPoints
  from InterviewPoints ip left join InterviewMeetings im on im.id = ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId                 
  left join UserList ipul on ipul.EmailId=ip.Comment   
  left join CandidateRatings cr on cr.Interviewid=im.id and cr.Interviewerid=ip.userid
  left join InterviewRatings clr on clr.Interviewid=im.id 
  left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=ip.userid
  where ip.Status='Paid' and ip.Interviewid!=0 and ip.PaymentHistoryId=@PaymentHistoryId               
  Union                 
  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,
  ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,              
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints                 
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                 
  left join UserList ipul on ipul.EmailId=ip.Comment         
  where ip.Status='Paid' and ip.Interviewerid!=0  and ip.PaymentHistoryId=@PaymentHistoryId            
  Union                 
  Select ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,
  ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,ip.Date, null as VideoDuration,                
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,0 as Surcharge,0 as CandidateRatingAmount,0 as ClientRatingAmount,ip.QCPoints                 
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                 
  where ip.Status='Paid' and ip.Interviewerid=0  and ip.Interviewid=0  and ip.PaymentHistoryId=@PaymentHistoryId)
  Select id,InterviewId,Type,Comment,Status,PrePoints,Penaltyamount,
  (Points+Isnull(Surcharge,0)+ISNULL(CandidateRatingAmount,0)+ISNULL(ClientRatingAmount,0)+ISNULL(QCPoints,0)) as Points,
  Date,VideoDuration,MeetingStatus,PreferredLocation,InterviewStatus,Surcharge,CandidateRatingAmount,ClientRatingAmount,QCPoints From CTE
                
END          
  Go
  
  
  
  
  
  
  