IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ProxyReortDetail'))
BEGIN
    DROP PROCEDURE dbo.usp_ProxyReortDetail
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_ProxyReortDetail
AS
Begin
Select pr.id,pr.interviewid,pr.isProxyCandidate,pr.EyeMovement,pr.LipsMovement,pr.CandidateDesktop,pr.LinkedinPhoto,pr.HeadMovement,pr.LatencyCheck,
pr.VoiceImpostering,pr.SusipicousDevices,pr.SuspiciousVoice,pr.SuspiciousEnvironment,pr.CreatedBy,pr.CreatedOn
from ProxyReport pr
Join InterviewMeetings im on pr.interviewid=im.id
where isProxyCandidate=0
End
GO