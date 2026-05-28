IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateTransInterviewerPerSuccessRate'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateTransInterviewerPerSuccessRate
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateTransInterviewerPerSuccessRate]
    @transInterviewerId int,
    @perSuccessRate int,
    @performedBy varchar(75) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE TransInterviewer
    SET PerSuccessRate = @perSuccessRate,
        ModifiedAt = GETDATE(),
        ModifiedBy = @performedBy
    WHERE id = @transInterviewerId;
END
GO
