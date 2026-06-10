IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateNaukriProspectiveStatus')
    DROP PROCEDURE usp_UpdateNaukriProspectiveStatus
GO

CREATE PROCEDURE [dbo].[usp_UpdateNaukriProspectiveStatus]
    @Id         INT,
    @Status     VARCHAR(50),
    @ChangedBy  VARCHAR(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Manual status changes now drive the operational TicketStatus (Open | InProgress | Closed).
    -- The legacy Status column is left untouched (it carries pipeline/AI outcomes only).
    UPDATE dbo.NaukriProspectiveInterviewer
    SET    TicketStatus = @Status,
           ProcessedBy  = ISNULL(@ChangedBy, ProcessedBy),
           UpdatedAt    = GETUTCDATE()
    WHERE  Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
