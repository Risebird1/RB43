IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_DeleteNaukriProspectiveInterviewer')
    DROP PROCEDURE usp_DeleteNaukriProspectiveInterviewer
GO

CREATE PROCEDURE [dbo].[usp_DeleteNaukriProspectiveInterviewer]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM dbo.NaukriProspectiveInterviewer WHERE Id = @Id;
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
