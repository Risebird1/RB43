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

    UPDATE UserList
    SET IsActive = 1,
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

    UPDATE TransInterviewer
    SET IsActive = 1
    WHERE UserId = @UserId

    INSERT INTO RedMarkerStatusDetails (InterviewerUserid, IRFId, Comment, Type, CreatedOn, CreatedBy, Status)
    VALUES (@UserId, 0, 'Reactivated by admin', 'TotalInterview', GETDATE(), @ReactivatedBy, 0)
END
GO
