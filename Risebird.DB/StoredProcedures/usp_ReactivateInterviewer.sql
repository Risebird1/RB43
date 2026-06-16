IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ReactivateInterviewer'))
BEGIN
    DROP PROCEDURE dbo.usp_ReactivateInterviewer
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_ReactivateInterviewer
    @UserId         INT,
    @ReactivatedBy  VARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Note: IsActive is only modified via the Edit Interviewer Profile form (Admin only).
    UPDATE UserList
    SET IsInterviewerAccess = 1,
        InactivationType = NULL,
        InactivatedBy    = NULL,
        InactivatedAt    = NULL
    WHERE id = @UserId

    UPDATE InterviewerProfile
    SET RedMarkerStatus = NULL,
        ModifiedBy      = @ReactivatedBy,
        ModifiedOn      = GETDATE()
    WHERE UserId = @UserId
      AND RedMarkerStatus = 3

    -- NOTE: We intentionally do NOT reactivate the interviewer in their existing skill channels.
    -- Reactivation only restores the user account; the interviewer's TransInterviewer (skill
    -- channel) memberships are left as-is and must be re-enabled deliberately if required.

    INSERT INTO RedMarkerStatusDetails (InterviewerUserid, IRFId, Comment, Type, CreatedOn, CreatedBy, Status)
    VALUES (@UserId, 0, 'Reactivated by admin', 'TotalInterview', GETDATE(), @ReactivatedBy, 0)
END
GO
