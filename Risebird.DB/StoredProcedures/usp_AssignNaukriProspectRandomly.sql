IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_AssignNaukriProspectRandomly')
    DROP PROCEDURE usp_AssignNaukriProspectRandomly
GO

-- Assigns a prospect (ticket) to a RANDOM eligible operator (active InterviewerManagement /
-- SeniorInterviewerManagement user), but only if it is not already assigned. Idempotent: safe to
-- call again for the same prospect — it will not re-assign an already-owned ticket.
CREATE PROCEDURE [dbo].[usp_AssignNaukriProspectRandomly]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL OR @Id <= 0 RETURN;

    -- Skip if already assigned.
    IF EXISTS (SELECT 1 FROM dbo.NaukriProspectiveInterviewer WHERE Id = @Id AND AssignedToUserId IS NOT NULL)
        RETURN;

    DECLARE @UserId INT, @Email VARCHAR(150), @Name NVARCHAR(200);

    SELECT TOP 1
        @UserId = ul.id,
        @Email  = ul.EmailId,
        @Name   = NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '')
    FROM dbo.UserList ul
    INNER JOIN dbo.Profile pf ON ul.idProfile = pf.id
    WHERE pf.[Name] IN ('InterviewerManagement', 'SeniorInterviewerManagement')
      AND ul.IsActive = 1
    ORDER BY NEWID();

    IF @UserId IS NULL RETURN;  -- no eligible operators; leave unassigned

    UPDATE dbo.NaukriProspectiveInterviewer
    SET AssignedToUserId = @UserId,
        AssignedToEmail  = @Email,
        AssignedToName   = @Name,
        AssignedAt       = GETUTCDATE(),
        UpdatedAt        = GETUTCDATE()
    WHERE Id = @Id
      AND AssignedToUserId IS NULL;  -- guard against a concurrent assignment
END
GO
