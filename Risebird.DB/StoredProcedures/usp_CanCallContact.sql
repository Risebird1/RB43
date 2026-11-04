IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CanCallContact'))
BEGIN
    DROP PROCEDURE dbo.usp_CanCallContact
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Pre-dial check, run before every outbound call (campaign jobs, dialer, api/CallPreference/*/CanCall).
-- @PersonType = Interviewer: blocked when an interviewer with that phone opted out (Isavailable = 5 or CallConsent = 0).
-- @PersonType = Candidate:   blocked when a candidate with that phone is cold in @IRFId (IRF.id), whether
--                            from a call opt-out or a manual cold mark. Other IRFs are not checked.
-- Interviewer and candidate checks never look at each other. Returns CanCall (bit), Reason (NULL when allowed).
CREATE PROCEDURE [dbo].[usp_CanCallContact]
    @PersonType varchar(20),
    @Phone      nvarchar(200),
    @IRFId      int = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @NormPhone varchar(10) = dbo.fn_NormalizePhone(@Phone)

    IF @NormPhone IS NULL
    BEGIN
        SELECT CAST(1 AS bit) AS CanCall, CAST(NULL AS varchar(200)) AS Reason
        RETURN
    END

    IF @PersonType = 'Interviewer'
    BEGIN
        -- Runs before every dial. Driven from the (small) opted-out set, matching the last 10 digits of
        -- MobileNumber (bigint) whatever country code it is stored with, like usp_RecordInterviewerCallOptOut.
        -- CallConsent = 0 also blocks: availability emails and My Profile can overwrite Isavailable = 5.
        IF EXISTS (SELECT 1
                   FROM InterviewerProfile ip
                   JOIN UserList ul ON ul.id = ip.UserId
                   WHERE (ip.Isavailable = 5 OR ip.CallConsent = 0)
                     AND ul.MobileNumber % 10000000000 = CAST(@NormPhone AS bigint))
            SELECT CAST(0 AS bit) AS CanCall, CAST('Interviewer opted out of calls' AS varchar(200)) AS Reason
        ELSE
            SELECT CAST(1 AS bit) AS CanCall, CAST(NULL AS varchar(200)) AS Reason
        RETURN
    END

    IF @PersonType = 'Candidate'
    BEGIN
        IF EXISTS (SELECT 1
                   FROM Candidate c
                   JOIN ColdCandidateInfos cci ON cci.FKCandidateId = c.id
                   WHERE c.IRFId = @IRFId
                     AND cci.IsColdCandidate = 1
                     AND dbo.fn_NormalizePhone(c.Phone) = @NormPhone)
            SELECT CAST(0 AS bit) AS CanCall, CAST('Candidate opted out or was marked cold in this skill channel' AS varchar(200)) AS Reason
        ELSE
            SELECT CAST(1 AS bit) AS CanCall, CAST(NULL AS varchar(200)) AS Reason
        RETURN
    END

    SELECT CAST(1 AS bit) AS CanCall, CAST(NULL AS varchar(200)) AS Reason
END
GO
