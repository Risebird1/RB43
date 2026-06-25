CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateTransInterviewerAutoQcFields]
    @TransInterviewerId        INT,
    @AutoQcVerdict             VARCHAR(20),
    @AutoQcTriggeringInterviewId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TransInterviewer
    SET AutoQcVerdict = @AutoQcVerdict,
        AutoQcTriggeringInterviewId = @AutoQcTriggeringInterviewId,
        AutoQcRemovedAt = GETUTCDATE()
    WHERE id = @TransInterviewerId;
END
GO
