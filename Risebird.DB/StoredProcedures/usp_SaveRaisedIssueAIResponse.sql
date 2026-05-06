IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveRaisedIssueAIResponse'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveRaisedIssueAIResponse
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Persists the full AI-review JSON response into RaisedIssues.AIReviewResponse.
-- Also closes the ticket when @CloseTicket = 1, recording the close in history.
CREATE PROCEDURE usp_SaveRaisedIssueAIResponse
    @TicketId        int,
    @AIReviewResponse varchar(max),
    @StatusComment   nvarchar(2000),
    @ClosedBy        varchar(200),
    @ClosedOn        datetime,
    @CloseTicket     bit = 0,
    @ActionByRole    varchar(50) = 'System'
AS
BEGIN
    SET NOCOUNT ON;

    -- Always persist the AI response regardless of decision
    UPDATE RaisedIssues
    SET AIReviewResponse = ISNULL(@AIReviewResponse,AIReviewResponse)
    WHERE id = @TicketId;

    IF @CloseTicket = 1
    BEGIN
        UPDATE RaisedIssues
        SET Status        = 'Close',
            StatusComment = @StatusComment,
            ClosedBy      = @ClosedBy,
            ClosedOn      = @ClosedOn
        WHERE id = @TicketId;

        INSERT INTO RaisedIssuesHistory(RaisedIssueId, Action, Comment, ActionBy, ActionByRole, ActionOn)
        VALUES (@TicketId, 'Closed', @StatusComment, @ClosedBy, @ActionByRole, @ClosedOn);
    END
    ELSE
    BEGIN
        -- Ticket stays open; record AI feedback as a history note
        INSERT INTO RaisedIssuesHistory(RaisedIssueId, Action, Comment, ActionBy, ActionByRole, ActionOn)
        VALUES (@TicketId, 'Reopened', @StatusComment, @ClosedBy, @ActionByRole, @ClosedOn);
    END
END
GO
