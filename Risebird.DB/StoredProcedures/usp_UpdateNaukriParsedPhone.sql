CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriParsedPhone]
    @Id    INT,
    @Phone VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET ParsedPhone = @Phone,
        UpdatedAt   = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
