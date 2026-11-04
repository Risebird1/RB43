IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_RecordInterviewerCallOptOut'))
BEGIN
    DROP PROCEDURE dbo.usp_RecordInterviewerCallOptOut
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Interviewer call opt-out (platform level). Called by the api/CallPreference/Interviewer/OptOut webhook
-- (AI agent / Plivo IVR) and by the interviewer flag panel "Calls -> Opted out" (human caller).
-- Matches by @UserId, else phone (last 10 digits of UserList.MobileNumber), else email.
-- Each matched interviewer: Isavailable = 5 (Opted out of calls, shows in Interviewer Churn), CallConsent = 0,
-- removed from every MAG (usp_ExcludeInterviewerFromMAG 'CallOptOut'). One CallPreferences row per match,
-- or one unmatched row so ops can reconcile. Never touches candidates, even with the same phone.
-- @OptOutSource: AICall | HumanCall. Returns OptedOut (interviewers matched), IRFId (always NULL).
CREATE PROCEDURE [dbo].[usp_RecordInterviewerCallOptOut]
    @UserId         int = NULL,
    @Phone          nvarchar(200) = NULL,
    @EmailId        varchar(200) = NULL,
    @Source         varchar(50) = NULL,
    @OptOutSource   varchar(20) = 'AICall',
    @ProviderCallId varchar(100) = NULL,
    @Reason         nvarchar(500) = NULL,
    @RawPayload     nvarchar(max) = NULL,
    @CreatedBy      varchar(200) = NULL,
    @Now            datetime
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @NormPhone varchar(10) = dbo.fn_NormalizePhone(@Phone)
    SET @EmailId = NULLIF(LTRIM(RTRIM(@EmailId)), '')
    SET @OptOutSource = CASE WHEN @OptOutSource IN ('AICall', 'HumanCall') THEN @OptOutSource ELSE 'AICall' END
    SET @CreatedBy = ISNULL(NULLIF(LTRIM(RTRIM(@CreatedBy)), ''), 'System (' + ISNULL(@Source, 'Unknown') + ')')

    DECLARE @Matched TABLE (UserId int PRIMARY KEY, EmailId varchar(200), Phone varchar(10))

    IF @UserId IS NOT NULL
        INSERT INTO @Matched (UserId, EmailId, Phone)
        SELECT ul.id, ul.EmailId, dbo.fn_NormalizePhone(ul.MobileNumber)
        FROM UserList ul
        WHERE ul.id = @UserId
          AND EXISTS (SELECT 1 FROM InterviewerProfile ip WHERE ip.UserId = ul.id)

    IF NOT EXISTS (SELECT 1 FROM @Matched) AND @NormPhone IS NOT NULL
        INSERT INTO @Matched (UserId, EmailId, Phone)
        -- From interviewers only, so the phone function runs on their rows, not every user (candidates included).
        -- MobileNumber is nvarchar in the live databases (bigint in Tables/UserList.sql); the function handles both.
        SELECT DISTINCT ul.id, ul.EmailId, @NormPhone
        FROM InterviewerProfile ip
        JOIN UserList ul ON ul.id = ip.UserId
        WHERE dbo.fn_NormalizePhone(ul.MobileNumber) = @NormPhone

    IF NOT EXISTS (SELECT 1 FROM @Matched) AND @EmailId IS NOT NULL
        INSERT INTO @Matched (UserId, EmailId, Phone)
        SELECT ul.id, ul.EmailId, dbo.fn_NormalizePhone(ul.MobileNumber)
        FROM UserList ul
        WHERE ul.EmailId = @EmailId
          AND EXISTS (SELECT 1 FROM InterviewerProfile ip WHERE ip.UserId = ul.id)

    DECLARE @OptedOut int = (SELECT COUNT(*) FROM @Matched)

    IF @OptedOut = 0
    BEGIN
        INSERT INTO CallPreferences (PersonType, UserId, Phone, EmailId, Action, Source, ProviderCallId, Reason, RawPayload, CreatedOn, CreatedBy)
        VALUES ('Interviewer', @UserId, @NormPhone, @EmailId, 'OptOut', @Source, @ProviderCallId, @Reason, @RawPayload, @Now, @CreatedBy)

        SELECT 0 AS OptedOut, CAST(NULL AS int) AS IRFId
        RETURN
    END

    BEGIN TRAN

    UPDATE ip
    SET IsavailableBeforeOptOut = CASE WHEN ip.Isavailable = 5 THEN ip.IsavailableBeforeOptOut ELSE ip.Isavailable END,
        Isavailable = 5,
        AvailabilityResponseDate = @Now,
        CallConsent = 0,
        CallConsentOn = @Now,
        CallOptOutSource = @OptOutSource,
        ModifiedBy = @CreatedBy,
        ModifiedOn = @Now
    FROM InterviewerProfile ip
    JOIN @Matched m ON m.UserId = ip.UserId

    -- Repeated opt-outs (tool + post-call backup) only log again: skip the MAG exclusion when one is open.
    DECLARE @Uid int
    DECLARE opt_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT m.UserId FROM @Matched m
        WHERE NOT EXISTS (SELECT 1 FROM InterviewerMAGExclusions e
                          WHERE e.UserId = m.UserId AND e.Reason = 'CallOptOut' AND e.RestoredOn IS NULL)
    OPEN opt_cur
    FETCH NEXT FROM opt_cur INTO @Uid
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC usp_ExcludeInterviewerFromMAG @Uid, 'CallOptOut', @CreatedBy, @Now
        FETCH NEXT FROM opt_cur INTO @Uid
    END
    CLOSE opt_cur
    DEALLOCATE opt_cur

    INSERT INTO CallPreferences (PersonType, UserId, Phone, EmailId, Action, Source, ProviderCallId, Reason, RawPayload, CreatedOn, CreatedBy)
    SELECT 'Interviewer', m.UserId, ISNULL(@NormPhone, m.Phone), m.EmailId, 'OptOut', @Source, @ProviderCallId, @Reason, @RawPayload, @Now, @CreatedBy
    FROM @Matched m

    -- InterviewerActionLog is for changes a portal user made: log human-caller opt-outs only.
    IF @OptOutSource = 'HumanCall' AND OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
        INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
        SELECT m.UserId, m.EmailId, 'CallOptOut',
               LEFT(@OptOutSource + ISNULL(': ' + NULLIF(LTRIM(RTRIM(@Reason)), ''), ''), 500),
               @CreatedBy, 'usp_RecordInterviewerCallOptOut' + ISNULL(':' + @Source, '')
        FROM @Matched m

    COMMIT TRAN

    SELECT @OptedOut AS OptedOut, CAST(NULL AS int) AS IRFId
END
GO
