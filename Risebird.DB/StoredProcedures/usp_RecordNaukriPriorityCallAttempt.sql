-- Records a priority-intent-collection call attempt against a prospect. Bumps the dedicated
-- priority counter and stamps PriorityCallLastAt (used for the between-call cooldown). Kept
-- separate from the regular PlivoCallAttempts* anti-spam counters by design.
CREATE OR ALTER PROCEDURE [dbo].[usp_RecordNaukriPriorityCallAttempt]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE NaukriProspectiveInterviewer
    SET PriorityCallAttempts = ISNULL(PriorityCallAttempts, 0) + 1,
        PriorityCallLastAt   = GETUTCDATE(),
        UpdatedAt            = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT;
END
GO
