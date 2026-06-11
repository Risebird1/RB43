/*
================================================================================
 Cleanup: remove InterviewerEmailAvailability rows that were inserted purely to
 place a Plivo availability call, but never reached a CONFIRMED availability.

 Background
 ----------
 Previously, profile interviewers were inserted into InterviewerEmailAvailability
 BEFORE the outbound Plivo voice call (IRF auto-call, MAG batch call, campaign
 batch call). An availability record must only exist for interviewers who have
 CONFIRMED availability — for a Plivo call that means a completed call whose
 AI-assessed disposition/intent was positive/interested ("available").

 The application now defers record creation to the hangup webhook, so going
 forward only confirmed interviewers are inserted. This script removes the
 spurious rows that already entered the table under the old behaviour.

 What is deleted
 ---------------
 A row is removed only when ALL of the following hold:
   * Source identifies it as a call-driven insert
       ('IRFAutoCall', 'BatchCall', 'Campaign', or 'Campaign - <name>')
   * The Plivo disposition is NOT a confirmed availability
       (PlivoIntent is NULL or not in the positive set)
   * It was never assigned to an assessment group (AssessmentGroupId IS NULL)
   * Employment was never verified (ISNULL(EmploymentVerified, 0) = 0)

 Rows where the interviewer confirmed availability (positive PlivoIntent) are
 KEPT, as are any genuine email-driven availability records (other Source values).

 Safety
 ------
 Runs inside a transaction. Review the PREVIEW result set, then either COMMIT
 or ROLLBACK by editing the flag below. Child history rows are removed first to
 satisfy the FK_IEAHistory_Record foreign key.
================================================================================
*/

SET NOCOUNT ON;

DECLARE @Commit BIT = 0;   -- set to 1 to actually delete; 0 = preview + rollback

BEGIN TRY
    BEGIN TRANSACTION;

    -- Candidate rows to remove
    IF OBJECT_ID('tempdb..#ToDelete') IS NOT NULL DROP TABLE #ToDelete;

    SELECT ea.Id
    INTO #ToDelete
    FROM InterviewerEmailAvailability ea
    WHERE
        (
            ea.Source IN ('IRFAutoCall', 'BatchCall', 'Campaign', 'CampaignBatchCall')
            OR ea.Source LIKE 'Campaign - %'
        )
        AND ea.AssessmentGroupId IS NULL
        AND ISNULL(ea.EmploymentVerified, 0) = 0
        AND (
            ea.PlivoIntent IS NULL
            OR LOWER(LTRIM(RTRIM(ea.PlivoIntent))) NOT IN
                ('available', 'interested', 'yes', 'confirmed', 'willing')
        );

    -- PREVIEW: what will be deleted
    SELECT
        ea.Id, ea.SenderEmail, ea.InterviewerProfileId, ea.MagName,
        ea.Source, ea.Status, ea.PlivoCallStatus, ea.PlivoIntent,
        ea.CreatedAt, ea.PlivoCallAt
    FROM InterviewerEmailAvailability ea
    INNER JOIN #ToDelete d ON d.Id = ea.Id
    ORDER BY ea.CreatedAt DESC;

    DECLARE @cnt INT = (SELECT COUNT(*) FROM #ToDelete);
    PRINT CONCAT('Rows identified for deletion: ', @cnt);

    -- Remove dependent history rows first (FK_IEAHistory_Record)
    DELETE h
    FROM InterviewerEmailAvailabilityHistory h
    INNER JOIN #ToDelete d ON d.Id = h.RecordId;
    PRINT CONCAT('History rows deleted: ', @@ROWCOUNT);

    -- Remove the availability rows
    DELETE ea
    FROM InterviewerEmailAvailability ea
    INNER JOIN #ToDelete d ON d.Id = ea.Id;
    PRINT CONCAT('Availability rows deleted: ', @@ROWCOUNT);

    DROP TABLE #ToDelete;

    IF @Commit = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'COMMITTED.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'ROLLED BACK (preview mode). Set @Commit = 1 to apply.';
    END
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT CONCAT('ERROR: ', ERROR_MESSAGE());
    THROW;
END CATCH
GO
