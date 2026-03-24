-- usp_GetSupportTicketHistory
-- Returns the full audit trail for a single SupportTicket, newest first.
CREATE OR ALTER PROCEDURE [dbo].[usp_GetSupportTicketHistory]
    @TicketId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        h.[Id],
        h.[TicketId],
        h.[FromStatus],
        h.[ToStatus],
        h.[ChangedBy],
        h.[ChangedAt],
        ISNULL(h.[EventType], 'StatusChange') AS [EventType]
    FROM   dbo.SupportTicketHistory h
    WHERE  h.TicketId = @TicketId
    ORDER  BY h.ChangedAt DESC;
END
GO
