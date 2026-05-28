IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateTransInterviewerCapacity'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateTransInterviewerCapacity
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateTransInterviewerCapacity]
    @transInterviewerId int,
    @interviewCapacity int,
    @performedBy varchar(75) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE TransInterviewer
    SET InterviewCapacity = @interviewCapacity,
        ModifiedAt = GETDATE(),
        ModifiedBy = @performedBy
    WHERE id = @transInterviewerId;
END
GO
