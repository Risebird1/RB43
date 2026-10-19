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
-- QC Activity (ProxyForm): an update overwrites Qcdoneby and keeps the first CreatedBy, so every
-- save is also logged in InterviewerActionLog (Reason starts "Interview #<id>:", read by
-- usp_QCActivity_CollectRows). Skipped while the log table is missing.
Declare @ProxyIsUpdate bit = CASE WHEN EXISTS (Select 1 From ProxyReport where interviewid=@InterviewId) THEN 1 ELSE 0 END

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

IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL AND NULLIF(LTRIM(RTRIM(@emailId)), '') IS NOT NULL
BEGIN
    Declare @ProxyIvEmail varchar(200) = (Select TOP 1 LTRIM(RTRIM(interviewer)) From InterviewMeetings where id=@InterviewId)
    INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
    SELECT (Select TOP 1 ul.id From UserList ul where ul.EmailId=@ProxyIvEmail ORDER BY ul.id DESC),
           @ProxyIvEmail,
           'ProxyForm',
           LEFT(N'Interview #' + CAST(@InterviewId AS NVARCHAR(20)) + N': proxy form '
                + CASE WHEN @ProxyIsUpdate = 1 THEN N'updated' ELSE N'filled' END
                + N'; proxy candidate: ' + CASE WHEN @isProxyCandidate = 1 THEN N'yes' ELSE N'no' END
                + CASE WHEN @IsQCdone = 1 THEN N'; QC done' ELSE N'' END, 500),
           LEFT(LTRIM(RTRIM(@emailId)), 200),
           'usp_SaveorUpdateProxyReport'
END
END
GO