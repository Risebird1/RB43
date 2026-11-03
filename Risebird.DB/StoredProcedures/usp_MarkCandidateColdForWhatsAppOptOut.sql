-- WhatsApp unsubscribe -> cold candidate (dbo.ColdCandidateInfos).
--
-- @Mark = 1: marks the candidate's row for @IRFId (IRF.id), or every row of the email when @IRFId is NULL
--   (a "Stop" reply, which is not tied to one role), as cold with @ColdInfo, e.g. "WhatsApp unsubscribe:
--   Got another job". Rows already cold are left as they are. Unlike Usp_ColdCandidateMarked it does NOT
--   cancel booked interviews: unsubscribing from WhatsApp is not cancelling an interview. The client status
--   webhook is queued ('coldcandidate') for each row that became cold, as Usp_ColdCandidateMarked does.
-- @Mark = 0: undoes it on re-enable - only rows still cold from a WhatsApp opt-out (ColdMarkedBY = @MarkedBy
--   values below), so a candidate an ops user marked cold stays cold.
--
-- ColdMarkedBY is 'WhatsApp unsubscribe' (button) or 'WhatsApp Stop reply' (keyword). ColdSource says where the
-- mark came from, e.g. "WhatsApp unsubscribe button (Schedule Interview)"; ColdReason is filled later from the
-- unsubscribe page (WhatsappRepository.UpdateColdReasonAsync). The role is the candidate row (FKCandidateId).
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_MarkCandidateColdForWhatsAppOptOut'))
BEGIN
    DROP PROCEDURE dbo.usp_MarkCandidateColdForWhatsAppOptOut
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_MarkCandidateColdForWhatsAppOptOut]
    @Email    VARCHAR(200),
    @IRFId    INT = NULL,
    @Mark     BIT,
    @MarkedBy VARCHAR(100),
    @ColdInfo VARCHAR(100) = NULL,
    @ColdSource VARCHAR(150) = NULL,
    @Now      DATETIME
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Rows TABLE (CandidateId INT, IRFName VARCHAR(50));

    INSERT INTO @Rows (CandidateId, IRFName)
    SELECT c.id, irf.IRFId
    FROM dbo.Candidate c
    INNER JOIN dbo.IRF irf ON irf.id = c.IRFId
    INNER JOIN dbo.CandidatesMaster cm ON cm.id = c.CandidateMasterId
    INNER JOIN dbo.UserList u ON u.id = cm.UserId
    WHERE u.EmailId = LTRIM(RTRIM(@Email))
      AND (@IRFId IS NULL OR c.IRFId = @IRFId);

    IF @Mark = 1
    BEGIN
        -- The rows that are not cold yet - they get marked, and only they notify the client.
        DECLARE @NewlyCold TABLE (IRFName VARCHAR(50));
        INSERT INTO @NewlyCold (IRFName)
        SELECT r.IRFName
        FROM @Rows r
        LEFT JOIN dbo.ColdCandidateInfos cci ON cci.FKCandidateId = r.CandidateId
        WHERE ISNULL(cci.IsColdCandidate, 0) = 0;

        UPDATE cci SET
            IsColdCandidate = 1,
            ColdMarkedBY    = @MarkedBy,
            ColdMarkedDate  = @Now,
            ColdInfo        = @ColdInfo,
            ColdSource      = @ColdSource,
            ColdReason      = NULL      -- a new mark: the reason, if any, is set from the unsubscribe page
        FROM dbo.ColdCandidateInfos cci
        INNER JOIN @Rows r ON r.CandidateId = cci.FKCandidateId
        WHERE ISNULL(cci.IsColdCandidate, 0) = 0;

        INSERT INTO dbo.ColdCandidateInfos (IsColdCandidate, ColdMarkedBY, ColdMarkedDate, FKCandidateId, ColdInfo, ColdSource)
        SELECT 1, @MarkedBy, @Now, r.CandidateId, @ColdInfo, @ColdSource
        FROM @Rows r
        WHERE NOT EXISTS (SELECT 1 FROM dbo.ColdCandidateInfos cci WHERE cci.FKCandidateId = r.CandidateId);

        DECLARE @IRFName VARCHAR(50);
        DECLARE cold_cursor CURSOR LOCAL FAST_FORWARD FOR SELECT DISTINCT IRFName FROM @NewlyCold;
        OPEN cold_cursor;
        FETCH NEXT FROM cold_cursor INTO @IRFName;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            EXEC usp_AddCandidateStatusWebHookData @Email, @IRFName, 'coldcandidate';
            FETCH NEXT FROM cold_cursor INTO @IRFName;
        END
        CLOSE cold_cursor;
        DEALLOCATE cold_cursor;

        SELECT COUNT(*) FROM @NewlyCold;
    END
    ELSE
    BEGIN
        UPDATE cci SET
            IsColdCandidate = 0,
            ModifyBy        = @MarkedBy,
            ModifyOn        = @Now
        FROM dbo.ColdCandidateInfos cci
        INNER JOIN @Rows r ON r.CandidateId = cci.FKCandidateId
        WHERE cci.IsColdCandidate = 1
          AND cci.ColdMarkedBY IN ('WhatsApp unsubscribe', 'WhatsApp Stop reply');

        SELECT @@ROWCOUNT;
    END
END
GO
