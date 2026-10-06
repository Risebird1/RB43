/*
================================================================================
usp_SaveInterviewerCoachingSession — records one coaching contact.

  - Links the coach's latest call to this interviewer made in the 3 hours
    before @NowServer (CallRecordingData.DateCreated uses the same app-server
    clock), so talk time and the recording attach to the session.
  - Channel = 'Email' is refused (Result = -1) when this interviewer already got a
    summary email in the last @EmailCooldownHours hours.
  - Unknown interviewer email: Result = -2.

Returns one row: Result (new Id, or -1 / -2), LinkedCallSid.
Requires Tables/InterviewerCoachingSession.sql.
================================================================================
*/
CREATE OR ALTER PROCEDURE [dbo].[usp_SaveInterviewerCoachingSession]
    @InterviewerEmail   VARCHAR(150),
    @CoachEmail         VARCHAR(150),
    @Channel            VARCHAR(20),
    @Outcome            VARCHAR(30),
    @Themes             NVARCHAR(1000) = NULL,
    @Agreed             NVARCHAR(2000) = NULL,
    @EmailSubject       NVARCHAR(300)  = NULL,
    @IssueCount         INT            = NULL,
    @FromIst            DATE           = NULL,
    @ToIst              DATE           = NULL,
    @NowServer          DATETIME       = NULL,
    @LinkRecentCall     BIT            = 1,
    @EmailCooldownHours INT            = 12
AS
BEGIN
    SET NOCOUNT ON;

    SET @InterviewerEmail = NULLIF(LTRIM(RTRIM(@InterviewerEmail)), '');
    SET @CoachEmail       = NULLIF(LTRIM(RTRIM(@CoachEmail)), '');
    SET @NowServer        = ISNULL(@NowServer, GETDATE());

    DECLARE @UserId INT = (SELECT TOP 1 id FROM dbo.UserList WHERE EmailId = @InterviewerEmail ORDER BY id DESC);
    IF @UserId IS NULL OR @CoachEmail IS NULL
    BEGIN
        SELECT -2 AS Result, CAST(NULL AS VARCHAR(50)) AS LinkedCallSid;
        RETURN;
    END

    BEGIN TRAN;

    IF @Channel = 'Email' AND EXISTS (
        SELECT 1 FROM dbo.InterviewerCoachingSession WITH (UPDLOCK, HOLDLOCK)
        WHERE InterviewerUserId = @UserId
          AND Channel = 'Email'
          AND CreatedAtUtc >= DATEADD(HOUR, -@EmailCooldownHours, GETUTCDATE()))
    BEGIN
        ROLLBACK;
        SELECT -1 AS Result, CAST(NULL AS VARCHAR(50)) AS LinkedCallSid;
        RETURN;
    END

    DECLARE @CallSid VARCHAR(50) = NULL;
    IF @LinkRecentCall = 1
        SELECT TOP 1 @CallSid = c.CallSid
        FROM dbo.CallRecordingData c
        WHERE c.Caller = @CoachEmail
          AND c.CallRecipient = @InterviewerEmail
          AND c.DateCreated >= DATEADD(HOUR, -3, @NowServer)
          AND c.DateCreated <= DATEADD(MINUTE, 5, @NowServer)
        ORDER BY c.DateCreated DESC, c.id DESC;

    INSERT INTO dbo.InterviewerCoachingSession
        (InterviewerUserId, InterviewerEmail, CoachEmail, Channel, Outcome, Themes, Agreed, CallSid,
         EmailSubject, IssueCount, FromIst, ToIst, CreatedAtUtc)
    VALUES
        (@UserId, @InterviewerEmail, @CoachEmail, LEFT(@Channel, 20), LEFT(@Outcome, 30),
         NULLIF(LTRIM(RTRIM(@Themes)), N''), NULLIF(LTRIM(RTRIM(@Agreed)), N''), @CallSid,
         NULLIF(LTRIM(RTRIM(@EmailSubject)), N''), @IssueCount, @FromIst, @ToIst, GETUTCDATE());

    DECLARE @Id INT = SCOPE_IDENTITY();
    COMMIT;

    SELECT @Id AS Result, @CallSid AS LinkedCallSid;
END
GO
