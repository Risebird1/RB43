CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriResumeLink]
    @Id   INT,
    @Link NVARCHAR(2000)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET ResumeLink    = @Link,
        HasAttachment = 1,
        UpdatedAt     = GETUTCDATE()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
