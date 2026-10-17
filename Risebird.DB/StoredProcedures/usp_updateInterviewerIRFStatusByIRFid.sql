IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_updateInterviewerIRFStatusByIRFid'))
BEGIN
    DROP PROCEDURE dbo.usp_updateInterviewerIRFStatusByIRFid
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- QC Tracking Delta 5: Interviewer Monitoring Remove / Shortlist records who did it. When @ActedBy is given
-- (the Monitoring page passes the session email and the required comment) the channel change is logged as
-- TransInterviewerAuditLog EventType MonitoringRemoved / MonitoringShortlisted (QC Activity, Monitoring column)
-- plus an InterviewerActionLog row (Source ...:Monitoring). Other callers (auto-QC, trial, IRF audit rails)
-- pass no actor and are not logged here, as before.
Create PROCEDURE usp_updateInterviewerIRFStatusByIRFid
@irfId int,
@Interviewer varchar(200),
@action varchar(100) = null,
@timeNow datetime = null,
@ActedBy varchar(200) = null,
@Comment nvarchar(500) = null
AS
BEGIN
declare @interviewerUserId int
declare @IRFName varchar(50)
set @interviewerUserId = (select id from UserList where Emailid=@Interviewer)
Select @IRFName=IRFID from IRF where id=@irfId

-- State before the change, for the audit row
declare @TransId int, @WasActive bit, @PrevClientAction varchar(100)
select top 1 @TransId = id, @WasActive = ISNULL(IsActive, 0), @PrevClientAction = ClientAction
from TransInterviewer where Userid=@interviewerUserId and IRFId=@irfId order by id desc

if(@action is not null and @action='Rejected')
begin
Update TransInterviewer Set ClientAction='Rejected', IsActive=0, IsAvailable=0 Where Userid=@interviewerUserId and IRFId=@irfId
update InterviewMeetings set Interviewer=null  where interviewdate>@timeNow and IRFId=@IRFName and Interviewer=@Interviewer
end
else
begin
Update TransInterviewer Set ClientAction='Shortlisted' Where Userid=@interviewerUserId and IRFId=@irfId
end

SET @ActedBy = NULLIF(LTRIM(RTRIM(@ActedBy)), '')
SET @Comment = NULLIF(LTRIM(RTRIM(@Comment)), N'')
IF @ActedBy IS NOT NULL AND @TransId IS NOT NULL
BEGIN
    DECLARE @EventType varchar(50) = CASE WHEN @action = 'Rejected' THEN 'MonitoringRemoved' ELSE 'MonitoringShortlisted' END

    IF OBJECT_ID(N'dbo.usp_InsertTransInterviewerAuditLog', N'P') IS NOT NULL
    BEGIN
        DECLARE @FromValue nvarchar(500) = CASE WHEN @WasActive = 1 THEN N'Active' ELSE N'Inactive' END
            + ISNULL(N'; ' + CAST(@PrevClientAction AS nvarchar(100)), N'')
        DECLARE @ToValue nvarchar(500) = CASE WHEN @action = 'Rejected' THEN N'Inactive; Rejected' ELSE N'Shortlisted' END
        DECLARE @Reason nvarchar(100) = LEFT(ISNULL(@Comment, CASE WHEN @action = 'Rejected' THEN N'Removed on Interviewer Monitoring' ELSE N'Shortlisted on Interviewer Monitoring' END), 100)
        DECLARE @Meta nvarchar(max) = N'page=InterviewerMonitoring; comment=' + ISNULL(@Comment, N'')
        EXEC dbo.usp_InsertTransInterviewerAuditLog
            @TransInterviewerId = @TransId,
            @EventType = @EventType,
            @FromValue = @FromValue,
            @ToValue = @ToValue,
            @ReasonCategory = @Reason,
            @ChangedBy = @ActedBy,
            @AuditSource = 'InterviewerMonitoring',
            @ExtraMetadata = @Meta
    END

    IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
        INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
        VALUES (@interviewerUserId,
                LEFT(@Interviewer, 200),
                @EventType,
                LEFT(N'IRF ' + ISNULL(CAST(@IRFName AS nvarchar(50)), CAST(@irfId AS nvarchar(20))) + N': ' + ISNULL(@Comment, N''), 500),
                LEFT(@ActedBy, 200),
                'usp_updateInterviewerIRFStatusByIRFid:Monitoring')
END
END
GO
