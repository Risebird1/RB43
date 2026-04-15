IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_DeleteInterviewerEmailAvailability')
    DROP PROCEDURE usp_DeleteInterviewerEmailAvailability
GO

CREATE PROCEDURE [dbo].[usp_DeleteInterviewerEmailAvailability]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.InterviewerEmailAvailability WHERE Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
