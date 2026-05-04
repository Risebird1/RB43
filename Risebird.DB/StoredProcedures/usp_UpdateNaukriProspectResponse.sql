CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateNaukriProspectResponse]
    @Id               INT,
    @ProspectResponse  VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE NaukriProspectiveInterviewer
    SET ProspectResponse   = @ProspectResponse,
        ProspectResponseAt = GETUTCDATE(),
        UpdatedAt          = GETUTCDATE()
    WHERE Id = @Id
END
GO
