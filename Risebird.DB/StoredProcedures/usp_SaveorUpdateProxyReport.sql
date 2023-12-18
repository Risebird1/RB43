IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveorUpdateProxyReport'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveorUpdateProxyReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE procedure usp_SaveorUpdateProxyReport
@InterviewId int,
@isProxyCandidate bit,
@EyeMovement varchar(500),
@LipsMovement varchar(500),
@CandidateDesktop varchar(500),
@LinkedinPhoto varchar(500),
@HeadMovement varchar(500),
@LatencyCheck varchar(500),
@VoiceImpostering varchar(500),
@SusipicousDevices varchar(500),
@SuspiciousVoice varchar(500),
@SuspiciousEnvironment varchar(500),
@IsQCdone bit,
@emailId varchar(200),
@CreatedOn datetime,
@ProxyvotingCount int
AS
BEGIN
Declare @Qcdoneby varchar(200)
if(@IsQCdone=1)
begin
set @Qcdoneby=@emailId
end

IF EXISTS (Select 1 FRom ProxyReport where interviewid=@InterviewId)  
begin
Update ProxyReport set isProxyCandidate=@isProxyCandidate,EyeMovement=@EyeMovement,LipsMovement=@LipsMovement,CandidateDesktop=@CandidateDesktop,
LinkedinPhoto=@LinkedinPhoto,HeadMovement=@HeadMovement,LatencyCheck=@LatencyCheck,VoiceImpostering=@VoiceImpostering,SusipicousDevices=@SusipicousDevices,
SuspiciousVoice=@SuspiciousVoice,SuspiciousEnvironment=@SuspiciousEnvironment,Qcdoneby=@Qcdoneby,ProxyvotingCount=@ProxyvotingCount where interviewid=@InterviewId
end
else 
begin
Insert into ProxyReport (interviewid,isProxyCandidate,EyeMovement,LipsMovement,CandidateDesktop,LinkedinPhoto,HeadMovement,LatencyCheck,
VoiceImpostering,SusipicousDevices,SuspiciousVoice,SuspiciousEnvironment,CreatedBy,CreatedOn,IsQCdone,Qcdoneby,ProxyvotingCount)
values(@interviewid,@isProxyCandidate,@EyeMovement,@LipsMovement,@CandidateDesktop,@LinkedinPhoto,@HeadMovement,@LatencyCheck,
@VoiceImpostering,@SusipicousDevices,@SuspiciousVoice,@SuspiciousEnvironment,@emailId,@CreatedOn,@IsQCdone,@Qcdoneby,@ProxyvotingCount)
end
END
GO