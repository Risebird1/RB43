IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchFNAllGeneratedInvoiceDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchFNAllGeneratedInvoiceDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_FetchFNAllGeneratedInvoiceDetails    
@st datetime,    
@se datetime    
AS    
BEGIN    
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,(ul.FirstName+ul.LastName) as FirstName,ip.Status,ip.Userid,    
(ip.Points) as Points,im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,    
irf.JobCountry as  PreferredLocation,ul.EmailId as InterviewerEmailid,IRF.ClientName,ic.VideoLink, ic.VideoDuration,uli.Emailid as ChannelOwner,    
CASE WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=0 THEN IsNull(ti.PerSuccessRate,0) WHEN (ip.Type='Interview' or ip.Type='Penalty QC' or ip.Type='Penalty') and im.IsEmergency=1 THEN IsNull(tei.PerSuccessRate,ti.PerSuccessRate)     
ELSE '' END as Rate,IRF.PrimarySkillInvite,im.IRFId,im.IsEmergency,IRF.InterviewDuration as MaxVideoDuration,IRF.IsFixedInterviewduration     
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join InterviewMeetings im on im.id = ip.InterviewId     
left join InterviewComments ic on ic.InterviewId=im.id join IRF on IRF.IRFID=im.IRFId join Client cl on cl.id=IRF.ClientId   
left join Userlist uli on uli.id=irf.risebirdhr left join TransInterviewer ti on ti.IRFId=irf.id and ti.UserId=ip.Userid     
left join TransEmergencyInterviewer tei on tei.IRFId=irf.id and tei.UserId=ip.Userid     
left join TransQualityController tqi on tqi.IRFId=irf.id and tqi.UserId=ip.Userid     
where ip.Interviewid!=0  and (ip.Status='Submitted' or ip.Status='Pending')  and (isNull(im.interviewdate,ip.Date)>=@st     
and isNull(im.interviewdate,ip.Date)<=@se)     
Union       
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,(ul.FirstName+ul.LastName) as FirstName,ip.Status,ip.Userid,    
(ip.Points) as Points, im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,    
'' as PreferredLocation,ul.EmailId as InterviewerEmailid ,'Interviewer' as ClientName,ic.VideoLink, null as VideoDuration,    
uli.Emailid as ChannelOwner,IsNull(ti.PerSuccessRate,0) as Rate,'' as PrimarySkillInvite,im.ISCId as IRFId,0 as IsEmergency,null as MaxVideoDuration,0 as IsFixedInterviewduration    
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId     
left join InterviewerComments ic on ic.InterviewId=im.id join InterviewerSkillsChannel  irf on IRF.InterviewerSkillId=im.ISCId     
left join Userlist uli on uli.id=irf.risebirdhr left join TransInterviewerSkillsLeadInterviewer ti on ti.InterviewerSkillId=irf.id and ti.UserId=ip.Userid      
where ip.Interviewerid!=0  and (ip.Status='Submitted' or ip.Status='Pending') and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se)      
union     
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,(ul.FirstName+ul.LastName) as FirstName,ip.Status,ip.Userid,    
(ip.Points) as Points,im.candidateEmail,im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,    
'' as PreferredLocation,ul.EmailId as InterviewerEmailid ,'Interviewer' as ClientName,ic.VideoLink, null as VideoDuration,    
uli.Emailid as ChannelOwner,0 as Rate,'' as PrimarySkillInvite,'' as IRFId,0 as IsEmergency,null as MaxVideoDuration,0 as IsFixedInterviewduration      
from InterviewPoints ip join UserList ul on ul.id=ip.userid   left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId       
left join InterviewerComments ic on ic.InterviewId=im.id   left join InterviewerSkillsChannel  irf on IRF.InterviewerSkillId=im.ISCId     
left join Userlist uli on uli.id=irf.risebirdhr     
where ip.Interviewerid=0 and ip.Interviewid=0 and (ip.Status='Submitted' or ip.Status='Pending')     
and (isNull(im.interviewdate,ip.Date)>=@st and isNull(im.interviewdate,ip.Date)<=@se)    
END
GO