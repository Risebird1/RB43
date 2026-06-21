CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriParsedEmail]
    @Id    INT,
    @Email VARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET ParsedEmail = @Email,
        UpdatedAt   = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
