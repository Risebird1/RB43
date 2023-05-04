
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_FetchInterviewPayDetails'))
BEGIN
    DROP PROCEDURE dbo.Usp_FetchInterviewPayDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  

CREATE Procedure Usp_FetchInterviewPayDetails
@Emailid varchar(200)          
As          
BEGIN          
Declare @Userid int          
Select @Userid=id from UserList where EmailId=@Emailid  
Select distinct  ip.id,ip.InterviewId,ip.Type,im.candidateName as Comment,ip.Status,ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,ir.JobCountry as  PreferredLocation,ic.VideoLink, ic.VideoDuration,IsNull(IsNull(ISNULL(ti.PerSuccessRate,tei.PerSuccessRate),tqi.PerSuccessRate),ip.Points) as Rate,ip.Comment as CrossCheckvalue from InterviewPoints ip           
Left join InterviewMeetings im on im.id=ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId    
left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=@UserId left join TransEmergencyInterviewer tei on tei.IRFId=ir.id and tei.UserId=@UserId             
left join TransQualityController tqi on tqi.IRFId=ir.id and tqi.UserId=@UserId     
where ip.userid=@Userid and ip.Interviewid!=0         
union        
Select distinct ip.id,ip.InterviewId,ip.Type,im.candidateName as Comment,ip.Status,ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,'' as PreferredLocation,ic.VideoLink, '' as VideoDuration,0 as Rate,ip.Comment as CrossCheckvalue from InterviewPoints ip           
Left join UnapprovedInterviewerMeetings im on im.id=ip.InterviewId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId           
where ip.userid=@Userid and ip.Interviewerid!=0            
union       
Select distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points as PrePoints,(ip.Points*IsNull(ip.PenaltyAmount,0)/100) Penaltyamount,(ip.Points-(ip.Points*IsNull(ip.PenaltyAmount,0)/100)) Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ic.InterviewStatus,'' as PreferredLocation,'' as VideoLink, '' as VideoDuration,0 as Rate,ip.Comment as CrossCheckvalue from InterviewPoints ip        
Left join UnapprovedInterviewerMeetings im on im.id=ip.InterviewId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId           
where ip.userid=@Userid and ip.Interviewerid=0 and ip.Interviewid=0           
END    
    
    
  
  
  GO
    



