-- usp_UpdateSupportTicketStatus
-- Updates the Status of a single SupportTicket row by its Id.
-- Also stamps UpdatedAt so we have an audit trail.
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateSupportTicketStatus]
    @Id     INT,
    @Status VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL OR @Id <= 0
    BEGIN
        RAISERROR('Invalid ticket Id.', 16, 1);
        RETURN;
    END

    IF @Status NOT IN ('Open', 'InProgress', 'Closed')
    BEGIN
        RAISERROR('Invalid status value. Allowed: Open, InProgress, Closed.', 16, 1);
        RETURN;
    END

    UPDATE dbo.SupportTickets
    SET    Status    = @Status,
           UpdatedAt = GETUTCDATE()
    WHERE  Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
