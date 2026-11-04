IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewerFlags'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerFlags
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- @ModifiedBy: session email of the user toggling the flags. Stored as UserList.InactivatedBy when access
-- is turned off ('Admin' when not passed, the legacy value) and as InterviewerActionLog.ActedBy.
-- InterviewerActionLog (QC Activity) gets one row per flag whose value actually changed:
-- Blocked / Unblocked (InterviewerProfile.IsDeleted) and AccessRevoked / AccessRestored
-- (UserList.IsInterviewerAccess). A block also turns access off; only Blocked is logged then.
-- Logging is skipped while InterviewerActionLog is missing.
Create procedure usp_UpdateInterviewerFlags
@Emailid varchar(150),
@LeadInterviewer bit=null,
@IsApproved bit=null,
@IsInterviewerAccess bit=null,
@IsBlocked bit=null,
@ModifiedBy varchar(200)=null
As
BEGIN
-- Values before the change (for InterviewerActionLog).
DECLARE @LogUserId int = NULL, @OldAccess bit = NULL, @OldBlocked bit = NULL;
SELECT TOP 1 @LogUserId = ul.id, @OldAccess = ul.IsInterviewerAccess
FROM UserList ul WHERE ul.EmailId = @Emailid ORDER BY ul.id DESC;
SELECT TOP 1 @OldBlocked = ip.IsDeleted
FROM InterviewerProfile ip WHERE ip.UserId = @LogUserId ORDER BY ip.Id DESC;

Update ip set
ip.IsLeadInterviewer=Isnull(@LeadInterviewer,ip.IsLeadInterviewer),
ip.IsApproved=isnull(@IsApproved,ip.IsApproved),
ip.IsDeleted=isnull(@IsBlocked,ip.IsDeleted)
from InterviewerProfile ip join UserList ul on ip.Userid=ul.id
where ul.EmailId=@Emailid
-- Note: IsActive is only modified via the Edit Interviewer Profile form (Admin only).
-- All portal flag toggles use IsInterviewerAccess.
Update UserList set IsInterviewerAccess=IsNull(@IsInterviewerAccess,IsInterviewerAccess),
    InactivationType = CASE WHEN @IsInterviewerAccess = 0 THEN 'ManualFlag' ELSE InactivationType END,
    InactivatedBy = CASE WHEN @IsInterviewerAccess = 0 THEN ISNULL(NULLIF(LTRIM(RTRIM(@ModifiedBy)), ''), 'Admin') ELSE InactivatedBy END,
    InactivatedAt = CASE WHEN @IsInterviewerAccess = 0 THEN GETDATE() ELSE InactivatedAt END
where EmailId=@Emailid
if(@IsInterviewerAccess=0)
begin
Update InterviewMeetings set Interviewer=null where Interviewer in(Select Emailid from UserList where  EmailId=@Emailid)
and interviewDate>DateAdd(Minute,330,GetDate())
Update TransInterviewer set IsActive=0,IsAvailable=0 where UserId in(Select id from UserList where  EmailId=@Emailid)
end

-- Call opt-out / MAG: a Risebird user switching access off removes the interviewer from every MAG;
-- switching it back on restores the groups removed for that reason (merged with any added since).
DECLARE @MagNow datetime = DATEADD(MINUTE, 330, GETUTCDATE())
DECLARE @MagBy varchar(200) = ISNULL(NULLIF(LTRIM(RTRIM(@ModifiedBy)), ''), 'Admin')
IF @LogUserId IS NOT NULL AND @IsInterviewerAccess = 0 AND ISNULL(@OldAccess, 1) = 1
    EXEC usp_ExcludeInterviewerFromMAG @LogUserId, 'ManualInactive', @MagBy, @MagNow
-- Only while the account is active too: a dashboard Remove (IsActive = 0) stays out of MAG until it is turned back on.
IF @LogUserId IS NOT NULL AND @IsInterviewerAccess = 1 AND ISNULL(@OldAccess, 1) = 0
   AND EXISTS (SELECT 1 FROM UserList WHERE id = @LogUserId AND ISNULL(IsActive, 0) = 1)
    EXEC usp_RestoreInterviewerToMAG @LogUserId, 'ManualInactive', @MagBy, @MagNow

IF @LogUserId IS NOT NULL AND OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
BEGIN
    DECLARE @LogBlock varchar(40) =
        CASE WHEN @IsBlocked = 1 AND ISNULL(@OldBlocked, 0) = 0 THEN 'Blocked'
             WHEN @IsBlocked = 0 AND ISNULL(@OldBlocked, 0) = 1 THEN 'Unblocked' END;
    DECLARE @LogAccess varchar(40) =
        CASE WHEN @IsInterviewerAccess = 0 AND ISNULL(@OldAccess, 1) = 1 AND ISNULL(@LogBlock, '') <> 'Blocked' THEN 'AccessRevoked'
             WHEN @IsInterviewerAccess = 1 AND ISNULL(@OldAccess, 1) = 0 THEN 'AccessRestored' END;

    INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
    SELECT @LogUserId, @Emailid, a.ActionType, NULL, NULLIF(LTRIM(RTRIM(@ModifiedBy)), ''), 'usp_UpdateInterviewerFlags'
    FROM (VALUES (@LogBlock), (@LogAccess)) a (ActionType)
    WHERE a.ActionType IS NOT NULL;
END
END
GO
