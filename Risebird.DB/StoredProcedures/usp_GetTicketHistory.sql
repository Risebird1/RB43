IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetTicketHistory'))
BEGIN
    DROP PROCEDURE dbo.usp_GetTicketHistory
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create Procedure usp_GetTicketHistory
@RaisedIssueId int
AS
BEGIN
    SELECT
        h.id,
        h.RaisedIssueId,
        h.Action,
        h.Comment,
        h.ActionBy,
        h.ActionOn,
        ul.FirstName + ' ' + ISNULL(ul.LastName, '') AS ActionByName
    FROM RaisedIssuesHistory h
    LEFT JOIN UserList ul ON ul.EmailId = h.ActionBy
    WHERE h.RaisedIssueId = @RaisedIssueId
    ORDER BY h.ActionOn ASC
END
GO
