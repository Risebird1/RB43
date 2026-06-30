CREATE OR ALTER PROCEDURE [dbo].[usp_UpdateTransInterviewerAutoQcFields]
    @TransInterviewerId        INT,
    @AutoQcVerdict             VARCHAR(20),
    @AutoQcTriggeringInterviewId INT,
    @AutoQcFlagSource          VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TransInterviewer
    SET AutoQcVerdict = @AutoQcVerdict,
        AutoQcTriggeringInterviewId = @AutoQcTriggeringInterviewId,
        AutoQcRemovedAt = GETUTCDATE(),
        AutoQcFlagSource = @AutoQcFlagSource
    WHERE id = @TransInterviewerId;
END
GO
