IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'usp_UpdateNaukriAckEmailSent')
    DROP PROCEDURE usp_UpdateNaukriAckEmailSent
GO

CREATE PROCEDURE [dbo].[usp_UpdateNaukriAckEmailSent]
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.NaukriProspectiveInterviewer
    SET    AcknowledgementEmailSent = 1,
           UpdatedAt                = GETUTCDATE()
    WHERE  Id = @Id;

    SELECT @@ROWCOUNT AS RowsAffected;
END
GO
