IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetRaisedIssueHistory'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRaisedIssueHistory
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Returns the full chronological history for a given ticket.
Create Procedure usp_GetRaisedIssueHistory
@RaisedIssueId int
AS
BEGIN
    SELECT
        h.id,
        h.RaisedIssueId,
        h.Action,
        h.Comment,
        h.ActionBy,
        h.ActionByRole,
        h.ActionOn,
        ul.FirstName AS ActionByName
    FROM RaisedIssuesHistory h
    LEFT JOIN UserList ul ON ul.EmailId = h.ActionBy
    WHERE h.RaisedIssueId = @RaisedIssueId
    ORDER BY h.ActionOn ASC;
END
GO
