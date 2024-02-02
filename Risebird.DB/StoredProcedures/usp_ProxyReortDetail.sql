IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ProxyReortDetail'))
BEGIN
    DROP PROCEDURE dbo.usp_ProxyReortDetail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_ProxyReortDetail
As
Begin
Select pr.id,pr.interviewid as InterviewId,pr.isProxyCandidate,pr.CreatedBy,pr.CreatedOn,IRF.id as irfIntId,im.interviewDate,IRF.IRFId,
im.candidateName,im.candidateEmail,im.interviewer,ul.MobileNumber as InterviewerMobileNumber,uli.EmailId as ChannelOwner,ic.InterviewStatus,
im.confirmationStatus,ic.MeetingStatus,tqc.id as QCPresent,ISNULL(pr.ProxyvotingCount,0) as ProxyvotingCount
from ProxyReport pr
Join InterviewMeetings im on pr.interviewid=im.id
join IRF on IRF.IRFId=im.IRFId
join [dbo].[InterviewComments] ic on im.id = ic.InterviewId
join UserList ul on ul.EmailId=im.candidateEmail
join UserList uli on uli.id= irf.RisebirdHr
left join PaymentSLADetails tqc on tqc.Interviewid=im.id  and ISNULL(tqc.isQcDone,0)=1
where pr.isProxyCandidate=1 and( ic.MeetingStatus=4 or ic.MeetingStatus=6 or ic.MeetingStatus=7)
End
GO