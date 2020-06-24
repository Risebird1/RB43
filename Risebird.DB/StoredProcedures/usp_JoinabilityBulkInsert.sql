/****** Object:  StoredProcedure [dbo].[usp_JoinabilityBulkInsert]    Script Date: 24-06-2020 1.21.59 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_JoinabilityBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_JoinabilityBulkInsert
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_JoinabilityBulkInsert]
(
      @JoinabilityBulkData AS [dbo].[TransJoinability_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TransJoinability](
	[IRFId] ,
	[LevelId],
	[Type],
	[Value],
	[Weightage])

SELECT * FROM @JoinabilityBulkData


END
GO


