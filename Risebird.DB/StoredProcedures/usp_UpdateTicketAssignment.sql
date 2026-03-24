-- usp_UpdateTicketAssignment
-- Updates the AssignedTo column on a SupportTicket and records the change
-- in SupportTicketHistory with EventType = 'AssignmentChange'.
CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateTicketAssignment]
    @Id         INT,
    @AssignedTo VARCHAR(150),
    @ChangedBy  VARCHAR(150) = 'System'
AS
BEGIN
    SET NOCOUNT ON;

    IF @Id IS NULL OR @Id <= 0 OR @AssignedTo IS NULL OR LEN(LTRIM(RTRIM(@AssignedTo))) = 0
    BEGIN
        SELECT 0 AS RowsAffected;
        RETURN;
    END

    DECLARE @FromAssignedTo VARCHAR(150);

    SELECT TOP 1 @FromAssignedTo = AssignedTo
    FROM   dbo.SupportTickets
    WHERE  id = @Id;

    UPDATE dbo.SupportTickets
    SET    AssignedTo = @AssignedTo,
           UpdatedAt  = GETUTCDATE()
    WHERE  id = @Id;

    DECLARE @Updated INT = @@ROWCOUNT;

    IF @Updated > 0
    BEGIN
        INSERT INTO dbo.SupportTicketHistory
            (TicketId, FromStatus, ToStatus, ChangedBy, ChangedAt, EventType)
        VALUES
            (@Id,
             ISNULL(@FromAssignedTo, 'Unassigned'),
             @AssignedTo,
             @ChangedBy,
             GETUTCDATE(),
             'AssignmentChange');
    END

    SELECT @Updated AS RowsAffected;
END
GO
