IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_BulkAssignNaukriProspectsByMag')
    DROP PROCEDURE usp_BulkAssignNaukriProspectsByMag
GO

-- Bulk-assigns every prospect in a MAG to a single eligible operator (the pre-system practice of
-- distributing tickets MAG-wise, now formalised). Validates the target is an active
-- InterviewerManagement / SeniorInterviewerManagement user, applies the requested scope, records
-- an AssignmentChange in NaukriOnboardingHistory for each ticket actually re-owned, and returns the
-- resolved assignee + affected count via OUTPUT parameters.
--
--   @Scope:
--     'unassigned' – only tickets with no current owner (default; safe top-up)
--     'active'     – all open / in-progress tickets (not Closed, not Onboarded), regardless of owner
--     'all'        – every ticket in the MAG, regardless of status / owner
CREATE PROCEDURE [dbo].[usp_BulkAssignNaukriProspectsByMag]
    @MagName          NVARCHAR(200),
    @AssignedToUserId INT,
    @Scope            VARCHAR(20)  = 'unassigned',
    @ChangedBy        VARCHAR(150) = 'System',
    @Email            VARCHAR(150) OUTPUT,
    @Name             NVARCHAR(200) OUTPUT,
    @Affected         INT          OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Email    = NULL;
    SET @Name     = NULL;
    SET @Affected = 0;

    IF @MagName IS NULL OR LTRIM(RTRIM(@MagName)) = '' OR @AssignedToUserId IS NULL OR @AssignedToUserId <= 0
        RETURN;

    IF @Scope NOT IN ('unassigned', 'active', 'all') SET @Scope = 'unassigned';

    -- Resolve + validate the target operator.
    SELECT TOP 1
        @Email = ul.EmailId,
        @Name  = NULLIF(LTRIM(RTRIM(ISNULL(ul.FirstName, '') + ' ' + ISNULL(ul.LastName, ''))), '')
    FROM dbo.UserList ul
    INNER JOIN dbo.Profile pf ON ul.idProfile = pf.id
    WHERE ul.id = @AssignedToUserId
      AND pf.[Name] IN ('InterviewerManagement', 'SeniorInterviewerManagement')
      AND ul.IsActive = 1;

    IF @Email IS NULL RETURN;  -- not an eligible operator

    DECLARE @Norm NVARCHAR(200) = dbo.fn_NormalizeMagName(@MagName);

    -- Collect the target ticket ids (+ their prior assignee for history) in one shot.
    DECLARE @Targets TABLE (Id INT PRIMARY KEY, FromEmail VARCHAR(150) NULL);

    INSERT INTO @Targets (Id, FromEmail)
    SELECT np.Id, np.AssignedToEmail
    FROM dbo.NaukriProspectiveInterviewer np
    WHERE dbo.fn_NormalizeMagName(np.MagName) = @Norm
      AND (
            @Scope = 'all'
            OR (@Scope = 'unassigned' AND np.AssignedToUserId IS NULL)
            OR (@Scope = 'active'     AND ISNULL(np.FunnelStage, '') <> 'Onboarded'
                                      AND ISNULL(np.TicketStatus, 'Open') <> 'Closed')
          )
      -- Never rewrite a ticket that is already owned by the same operator (nothing changes).
      AND ISNULL(np.AssignedToUserId, -1) <> @AssignedToUserId;

    UPDATE np
    SET np.AssignedToUserId = @AssignedToUserId,
        np.AssignedToEmail  = @Email,
        np.AssignedToName   = @Name,
        np.AssignedAt       = GETUTCDATE(),
        np.UpdatedAt        = GETUTCDATE()
    FROM dbo.NaukriProspectiveInterviewer np
    INNER JOIN @Targets t ON t.Id = np.Id;

    SET @Affected = @@ROWCOUNT;

    -- Audit every re-owned ticket (mirrors the single-assign AssignmentChange history).
    INSERT INTO dbo.NaukriOnboardingHistory (RecordId, EventType, FromValue, ToValue, ChangedBy, ChangedAt)
    SELECT t.Id, 'AssignmentChange', ISNULL(t.FromEmail, 'Unassigned'), @Email, @ChangedBy, GETUTCDATE()
    FROM @Targets t;
END
GO
