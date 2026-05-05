IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ReopenRaisedIssue'))
BEGIN
    DROP PROCEDURE dbo.usp_ReopenRaisedIssue
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_ReopenRaisedIssue
@id int,
@ReopenComment varchar(1000),
@ReopenedBy varchar(200),
@ReopenedOn datetime
AS
BEGIN
    Update RaisedIssues
    set Status = 'Open',
        StatusComment = 'Reopened: ' + @ReopenComment,
        ClosedBy = NULL,
        ClosedOn = NULL
    where id = @id and RequestedBy = @ReopenedBy
END
GO
