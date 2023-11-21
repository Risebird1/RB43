IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetFormatanddifficultylevel'))
BEGIN
    DROP PROCEDURE dbo.usp_GetFormatanddifficultylevel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetFormatanddifficultylevel
@irfid int
AS
BEGIN
Select * FRom IRFInstructionDetails where IRFId=@irfid
END
GO
