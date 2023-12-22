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
Select pr.id,pr.interviewid as InterviewId,pr.isProxyCandidate,pr.EyeMovement,pr.LipsMovement,pr.CandidateDesktop,pr.LinkedinPhoto,pr.HeadMovement,pr.LatencyCheck,
pr.VoiceImpostering,pr.SusipicousDevices,pr.SuspiciousVoice,pr.SuspiciousEnvironment,pr.CreatedBy,pr.CreatedOn,IRF.id as irfIntId,im.interviewDate,im.confirmationStatus,ic.MeetingStatus
from ProxyReport pr
Join InterviewMeetings im on pr.interviewid=im.id
join IRF on IRF.IRFId=im.IRFId
join [dbo].[InterviewComments] ic on im.id = ic.InterviewId 
where isProxyCandidate=0
End
GO