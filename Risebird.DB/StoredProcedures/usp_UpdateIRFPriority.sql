IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateIRFPriority'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateIRFPriority
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateIRFPriority
@irfid int,
@PriorityLevel int
AS
BEGIN
Update IRF set PriorityLevel=@PriorityLevel where id=@irfid
END
GO