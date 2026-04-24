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

    UPDATE dbo.NaukriProspectiveInterviewer
    SET    Status     = @Status,
           ProcessedBy = ISNULL(@ChangedBy, ProcessedBy),
           UpdatedAt  = GETUTCDATE()
    WHERE  Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
