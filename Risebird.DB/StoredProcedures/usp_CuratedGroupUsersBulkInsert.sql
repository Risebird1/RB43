/****** Object:  StoredProcedure [dbo].[usp_CuratedGroupUsersBulkInsert]    Script Date: 13-07-2020 10.45.03 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CuratedGroupUsersBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_CuratedGroupUsersBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO




CREATE PROCEDURE [dbo].[usp_CuratedGroupUsersBulkInsert]
(
      @CuratedGroupUsersBulkData AS [dbo].[CuratedGroupUsers_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;



--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[CuratedGroupUsers](
	[UserId],
	[CuratedGroupId],
	[IsAdded])

SELECT * FROM @CuratedGroupUsersBulkData


END
GO


