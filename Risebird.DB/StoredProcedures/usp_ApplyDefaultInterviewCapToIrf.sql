IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_ApplyDefaultInterviewCapToIrf'))
BEGIN
    DROP PROCEDURE dbo.usp_ApplyDefaultInterviewCapToIrf
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ApplyDefaultInterviewCapToIrf]
    @irfId int,
    @defaultCap int,
    @performedBy varchar(75) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE TransInterviewer
    SET InterviewCapacity = @defaultCap,
        ModifiedAt = GETDATE(),
        ModifiedBy = @performedBy
    WHERE IRFId = @irfId;
END
GO
