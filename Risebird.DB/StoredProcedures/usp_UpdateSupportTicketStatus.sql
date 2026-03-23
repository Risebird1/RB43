-- usp_UpdateSupportTicketStatus
-- Updates the Status of a SupportTicket, stamps UpdatedAt, and writes
-- a row to SupportTicketHistory so every change is permanently audited.
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateSupportTicketStatus]
    @Id        INT,
    @Status    VARCHAR(50),
    @ChangedBy VARCHAR(150) = 'System'
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

    -- Capture the current status before overwriting it
    DECLARE @FromStatus VARCHAR(50);
    SELECT TOP 1 @FromStatus = Status
    FROM   dbo.SupportTickets
    WHERE  Id = @Id;

    -- Update the ticket
    UPDATE dbo.SupportTickets
    SET    Status    = @Status,
           UpdatedAt = GETUTCDATE()
    WHERE  Id = @Id;

    DECLARE @Updated INT = @@ROWCOUNT;

    -- Write audit history only when the row was actually changed
    IF @Updated > 0
    BEGIN
        INSERT INTO dbo.SupportTicketHistory
            (TicketId, FromStatus, ToStatus, ChangedBy, ChangedAt)
        VALUES
            (@Id, @FromStatus, @Status, @ChangedBy, GETUTCDATE());
    END

    SELECT @Updated AS RowsAffected;
END
GO
