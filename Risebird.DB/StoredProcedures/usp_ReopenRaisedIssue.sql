IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ReopenRaisedIssue'))
BEGIN
    DROP PROCEDURE dbo.usp_ReopenRaisedIssue
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Reopens a ticket without touching any of the original fields.
-- Only Status is updated; a new history row is inserted with the reopen comment.
Create Procedure usp_ReopenRaisedIssue
@id          int,
@Comment     varchar(2000),
@ActionBy    varchar(200),
@ActionByRole varchar(50) = NULL,
@ActionOn    datetime
AS
BEGIN
    UPDATE RaisedIssues
    SET Status = 'Open', ClosedBy = NULL, ClosedOn = NULL, StatusComment = NULL
    WHERE id = @id;

    INSERT INTO RaisedIssuesHistory(RaisedIssueId, Action, Comment, ActionBy, ActionByRole, ActionOn)
    VALUES (@id, 'Reopened', @Comment, @ActionBy, @ActionByRole, @ActionOn);
END
GO
