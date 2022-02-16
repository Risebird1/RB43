IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_FetchAllFNDetailAdvance'))
BEGIN
    DROP PROCEDURE dbo.usp_FetchAllFNDetailAdvance
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE Procedure usp_FetchAllFNDetailAdvance
AS
BEGIN
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,ul.FirstName,ip.Status,ip.Userid,(ip.Points) as Points,im.candidateEmail,
im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,'' as  PreferredLocation,
ul.EmailId as InterviewerEmailid,IRF.ClientName,ic.VideoLink, ic.VideoDuration 
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join InterviewMeetings im on im.id = ip.InterviewId 
left join InterviewComments ic on ic.InterviewId=im.id join IRF on IRF.IRFID=im.IRFId 
where ip.Status='Advance' and ip.Interviewid!=0
Union 
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,ul.FirstName,ip.Status,ip.Userid,(ip.Points) as Points,im.candidateEmail,
im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,'' as PreferredLocation,
ul.EmailId as InterviewerEmailid ,'Interviewer' as ClientName,ic.VideoLink, null as VideoDuration 
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId 
left join InterviewerComments ic on ic.InterviewId=im.id join InterviewerSkillsChannel  irf on IRF.InterviewerSkillId=im.ISCId 
where ip.Status='Advance' and ip.Interviewerid!=0 
Union 
Select Distinct ip.id,ip.InterviewId,ip.Type,ip.Comment,ul.FirstName,ip.Status,ip.Userid,(ip.Points) as Points,im.candidateEmail,
im.candidateName,ic.MeetingStatus,ic.InterviewStatus,isNull(im.interviewdate,ip.Date) as Date,'' as PreferredLocation,
ul.EmailId as InterviewerEmailid ,'Interviewer' as ClientName,ic.VideoLink, null as VideoDuration 
from InterviewPoints ip join UserList ul on ul.id=ip.userid left join UnapprovedInterviewerMeetings im on im.id = ip.InterviewerId 
left join InterviewerComments ic on ic.InterviewId=im.id left join InterviewerSkillsChannel  irf on IRF.InterviewerSkillId=im.ISCId
where ip.Status='Advance' and ip.Interviewerid=0 and ip.Interviewid=0
END
GO