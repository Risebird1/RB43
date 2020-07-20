/****** Object:  StoredProcedure [dbo].[usp_CuratedIRFBulkInsert]    Script Date: 13-07-2020 10.43.09 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CuratedIRFBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_CuratedIRFBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



CREATE PROCEDURE [dbo].[usp_CuratedIRFBulkInsert]
(
      @CuratedIRFBulkData AS [dbo].[CuratedIRFMapping_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[CuratedIRFMapping](
	[CuratedGroupId] ,
	[IRFId],
	[IsAdded])

SELECT * FROM @CuratedIRFBulkData


END
GO


