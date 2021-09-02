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
  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date, ic.VideoDuration,              
  ic.MeetingStatus,ir.JobCountry as  PreferredLocation,ic.InterviewStatus
  from InterviewPoints ip left join InterviewMeetings im on im.id = ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId                 
  left join UserList ipul on ipul.EmailId=ip.Comment        
  where ip.Status='Paid' and ip.Interviewid!=0 and ip.PaymentHistoryId=@PaymentHistoryId               
  Union                 
  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,              
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus                 
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                 
  left join UserList ipul on ipul.EmailId=ip.Comment         
  where ip.Status='Paid' and ip.Interviewerid!=0  and ip.PaymentHistoryId=@PaymentHistoryId            
  Union                 
  Select ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points,ip.Date, null as VideoDuration,                
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus                 
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                 
  where ip.Status='Paid' and ip.Interviewerid=0  and ip.Interviewid=0  and ip.PaymentHistoryId=@PaymentHistoryId              
                
END          
  Go
  
  
  
  
  
  
  