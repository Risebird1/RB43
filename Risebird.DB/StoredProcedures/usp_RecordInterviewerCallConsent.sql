IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_RecordInterviewerCallConsent'))
BEGIN
    DROP PROCEDURE dbo.usp_RecordInterviewerCallConsent
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Interviewer call opt-in: the acknowledgement email "Yes" button (link carries callConsent=1), confirmed by POST.
-- Sets CallConsent = 1 and logs an OptIn row in CallPreferences with IP address and user agent (UCC consent proof).
-- Reverses a previous call opt-out: Isavailable 5 -> 1 and the 'CallOptOut' MAG exclusion is restored.
-- A 'ManualInactive' exclusion is left alone. Returns ConsentRecorded (bit), WasOptedOut (bit).
CREATE PROCEDURE [dbo].[usp_RecordInterviewerCallConsent]
    @UserId    int,
    @IPAddress varchar(64) = NULL,
    @UserAgent varchar(500) = NULL,
    @Now       datetime
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EmailId varchar(200), @Phone varchar(10)
    SELECT @EmailId = ul.EmailId, @Phone = RIGHT(CAST(ul.MobileNumber AS varchar(20)), 10)
    FROM UserList ul
    WHERE ul.id = @UserId
      AND EXISTS (SELECT 1 FROM InterviewerProfile ip WHERE ip.UserId = ul.id)

    IF @EmailId IS NULL
    BEGIN
        SELECT CAST(0 AS bit) AS ConsentRecorded, CAST(0 AS bit) AS WasOptedOut
        RETURN
    END

    DECLARE @WasOptedOut bit =
        CASE WHEN EXISTS (SELECT 1 FROM InterviewerProfile WHERE UserId = @UserId AND (Isavailable = 5 OR CallConsent = 0))
               OR EXISTS (SELECT 1 FROM InterviewerMAGExclusions WHERE UserId = @UserId AND Reason = 'CallOptOut' AND RestoredOn IS NULL)
             THEN 1 ELSE 0 END

    BEGIN TRAN

    UPDATE InterviewerProfile
    SET CallConsent = 1,
        CallConsentOn = @Now,
        CallOptOutSource = NULL,
        IsavailableBeforeOptOut = NULL,
        -- The "Yes" is an explicit "I'm available", so 5 -> 1 rather than the status before the opt-out.
        Isavailable = CASE WHEN Isavailable = 5 THEN 1 ELSE Isavailable END,
        AvailabilityResponseDate = CASE WHEN Isavailable = 5 THEN @Now ELSE AvailabilityResponseDate END
    WHERE UserId = @UserId

    IF @WasOptedOut = 1
        EXEC usp_RestoreInterviewerToMAG @UserId, 'CallOptOut', 'Interviewer (AcknowledgementEmail)', @Now

    INSERT INTO CallPreferences (PersonType, UserId, Phone, EmailId, Action, Source, IPAddress, UserAgent, CreatedOn, CreatedBy)
    VALUES ('Interviewer', @UserId, @Phone, @EmailId, 'OptIn', 'AcknowledgementEmail', LEFT(@IPAddress, 64), LEFT(@UserAgent, 500), @Now, @EmailId)

    COMMIT TRAN

    SELECT CAST(1 AS bit) AS ConsentRecorded, @WasOptedOut AS WasOptedOut
END
GO
