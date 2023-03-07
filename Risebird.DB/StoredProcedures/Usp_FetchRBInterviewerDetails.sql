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
  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date, ic.VideoDuration,ir.InterviewDuration as MaxVideoDuration,
  ic.MeetingStatus,ir.JobCountry as  PreferredLocation,ic.InterviewStatus,CASE WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=0 THEN IsNull(ti.PerSuccessRate,0) WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=1 THEN IsNull(tei.PerSuccessRate,ti.PerSuccessRate) ELSE '' END as Rate           
  from InterviewPoints ip left join InterviewMeetings im on im.id = ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId left join Client cl on cl.id=ir.Clientid                    
  left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=@UserId left join TransEmergencyInterviewer tei on tei.IRFId=ir.id and tei.UserId=@UserId                     
  left join TransQualityController tqi on tqi.IRFId=ir.id and tqi.UserId=@UserId            
  left join UserList ipul on ipul.EmailId=ip.Comment            
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Advance') and ip.Interviewid!=0                    
  Union                     
  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,null as MaxVideoDuration,                  
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,ISNULL(ti.PerSuccessRate,0) as Rate                     
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                     
  left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=ir.id and ti.UserId=@UserId              
  left join UserList ipul on ipul.EmailId=ip.Comment             
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Advance') and ip.Interviewerid!=0                  
  Union                     
  Select ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,null as MaxVideoDuration,                    
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,ISNULL(ti.PerSuccessRate,0) as Rate                     
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                     
  left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=ir.id and ti.UserId=@UserId                     
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Pending' or ip.Status='Advance') and ip.Interviewerid=0  and ip.Interviewid=0                    
 End                    
Else                    
 Begin                    
  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date, ic.VideoDuration,ir.InterviewDuration as MaxVideoDuration,                   
  ic.MeetingStatus,ir.JobCountry as  PreferredLocation,ic.InterviewStatus,CASE WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=0 THEN IsNull(ti.PerSuccessRate,0) WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=1 THEN IsNull(tei.PerSuccessRate,ti.PerSuccessRate) ELSE '' END as Rate                     
  from InterviewPoints ip left join InterviewMeetings im on im.id = ip.InterviewId           
left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId left join Client cl on cl.id=ir.Clientid                    
  left join TransInterviewer ti on ti.IRFId=ir.id and ti.UserId=@UserId                     
  left join TransEmergencyInterviewer tei on tei.IRFId=ir.id and tei.UserId=@UserId                     
  left join TransQualityController tqi on tqi.IRFId=ir.id and tqi.UserId=@UserId              
  left join UserList ipul on ipul.EmailId=ip.Comment           
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Advance') and ip.Interviewid!=0                    
  union                     
  Select ip.id,ip.InterviewId,ip.Type,ISNULL((Isnull(ipul.FirstName,'')+Isnull(ipul.LastName,'')),ip.Comment) as Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,null as MaxVideoDuration,                   
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,IsNull(ti.PerSuccessRate,0) as Rate                     
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId                     
  left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                     
  left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=ir.id and ti.UserId=@UserId             
  left join UserList ipul on ipul.EmailId=ip.Comment          
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Advance') and ip.Interviewerid!=0                    
  union                     
  Select ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date, null as VideoDuration,null as MaxVideoDuration,                  
  ic.MeetingStatus,'' as  PreferredLocation,ic.InterviewStatus,IsNull(ti.PerSuccessRate,0) as Rate                     
  from InterviewPoints ip left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId                     
  left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId                     
  left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=ir.id and ti.UserId=@UserId                     
  where ip.userid = @Userid and (ip.Status='Submitted' or ip.Status='Advance') and ip.Interviewerid=0  and ip.Interviewid=0                      
 End                    
                    
END      
  
GO

