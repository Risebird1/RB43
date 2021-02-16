
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
Select distinct  ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,ir.JobCountry as  PreferredLocation from InterviewPoints ip     
Left join InterviewMeetings im on im.id=ip.InterviewId left join InterviewComments ic on ic.InterviewId=im.id left join IRF ir on ir.IRFId=im.IRFId     
where ip.userid=@Userid and ip.Interviewerid!=0   
union  
Select distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,ip.Status,ip.Points,isNull(im.interviewdate,ip.Date) as Date,ic.MeetingStatus,'' as PreferredLocation from InterviewPoints ip     
Left join UnapprovedInterviewerMeetings im on im.id=ip.InterviewId left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel ir on ir.InterviewerSkillId=im.ISCId     
where ip.userid=@Userid and ip.Interviewid!=0      

END