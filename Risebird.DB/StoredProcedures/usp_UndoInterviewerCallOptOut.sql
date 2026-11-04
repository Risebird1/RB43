IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UndoInterviewerCallOptOut'))
BEGIN
    DROP PROCEDURE dbo.usp_UndoInterviewerCallOptOut
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Interviewer flag panel "Calls -> Allowed": a Risebird user reverses a call opt-out.
-- Isavailable 5 -> the status before the opt-out (1 when unknown), back in MAG (restores the 'CallOptOut' exclusion only), CallConsent reset to unknown
-- (an undo is not consent). Logged in CallPreferences as Action = Undo. Returns Undone (bit).
CREATE PROCEDURE [dbo].[usp_UndoInterviewerCallOptOut]
    @UserId      int,
    @PerformedBy varchar(200),
    @Now         datetime
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @EmailId varchar(200), @Phone varchar(10)
    SELECT @EmailId = ul.EmailId, @Phone = dbo.fn_NormalizePhone(ul.MobileNumber)
    FROM UserList ul
    WHERE ul.id = @UserId

    IF NOT EXISTS (SELECT 1 FROM InterviewerProfile WHERE UserId = @UserId AND (Isavailable = 5 OR CallConsent = 0))
       AND NOT EXISTS (SELECT 1 FROM InterviewerMAGExclusions WHERE UserId = @UserId AND Reason = 'CallOptOut' AND RestoredOn IS NULL)
    BEGIN
        SELECT CAST(0 AS bit) AS Undone
        RETURN
    END

    BEGIN TRAN

    UPDATE InterviewerProfile
    SET Isavailable = CASE WHEN Isavailable = 5 THEN ISNULL(IsavailableBeforeOptOut, 1) ELSE Isavailable END,
        AvailabilityResponseDate = CASE WHEN Isavailable = 5 THEN @Now ELSE AvailabilityResponseDate END,
        IsavailableBeforeOptOut = NULL,
        CallConsent = NULL,
        CallConsentOn = NULL,
        CallOptOutSource = NULL,
        ModifiedBy = @PerformedBy,
        ModifiedOn = @Now
    WHERE UserId = @UserId

    EXEC usp_RestoreInterviewerToMAG @UserId, 'CallOptOut', @PerformedBy, @Now

    INSERT INTO CallPreferences (PersonType, UserId, Phone, EmailId, Action, Source, CreatedOn, CreatedBy)
    VALUES ('Interviewer', @UserId, @Phone, @EmailId, 'Undo', 'Portal', @Now, @PerformedBy)

    IF OBJECT_ID(N'dbo.InterviewerActionLog', N'U') IS NOT NULL
        INSERT INTO dbo.InterviewerActionLog (InterviewerUserId, InterviewerEmail, ActionType, Reason, ActedBy, Source)
        VALUES (@UserId, @EmailId, 'CallOptOutUndone', NULL, NULLIF(LTRIM(RTRIM(@PerformedBy)), ''), 'usp_UndoInterviewerCallOptOut')

    COMMIT TRAN

    SELECT CAST(1 AS bit) AS Undone
END
GO
