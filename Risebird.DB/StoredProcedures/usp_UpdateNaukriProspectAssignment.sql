IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateNaukriProspectAssignment')
    DROP PROCEDURE usp_UpdateNaukriProspectAssignment
GO

-- Manually (re)assigns a prospect (ticket) to an eligible operator. Validates that the target is an
-- active InterviewerManagement / SeniorInterviewerManagement user; rejects otherwise. Returns the
-- previous assignee (for history logging) and the number of rows affected via OUTPUT parameters.
CREATE PROCEDURE [dbo].[usp_UpdateNaukriProspectAssignment]
    @Id               INT,
    @AssignedToUserId INT,
    @FromAssignedTo   VARCHAR(150) OUTPUT,
    @RowsAffected     INT          OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @FromAssignedTo = NULL;
    SET @RowsAffected   = 0;

    IF @Id IS NULL OR @Id <= 0 OR @AssignedToUserId IS NULL OR @AssignedToUserId <= 0
        RETURN;

    -- Resolve + validate the target operator.
    DECLARE @Email VARCHAR(150), @Name NVARCHAR(200);

    SELECT TOP 1
        @Email = ul.EmailId,
        @Name  = NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '')
    FROM dbo.UserList ul
    INNER JOIN dbo.Profile pf ON ul.idProfile = pf.id
    WHERE ul.id = @AssignedToUserId
      AND pf.[Name] IN ('InterviewerManagement', 'SeniorInterviewerManagement')
      AND ul.IsActive = 1;

    IF @Email IS NULL RETURN;  -- not an eligible operator

    -- Capture the current assignee for history.
    SELECT TOP 1 @FromAssignedTo = AssignedToEmail
    FROM dbo.NaukriProspectiveInterviewer
    WHERE Id = @Id;

    UPDATE dbo.NaukriProspectiveInterviewer
    SET AssignedToUserId = @AssignedToUserId,
        AssignedToEmail  = @Email,
        AssignedToName   = @Name,
        AssignedAt       = GETUTCDATE(),
        UpdatedAt        = GETUTCDATE()
    WHERE Id = @Id;

    SET @RowsAffected = @@ROWCOUNT;
END
GO
