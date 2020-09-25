/****** Object:  StoredProcedure [dbo].[usp_VMUsersBulkInsert]    Script Date: 25-09-2020 11.57.42 AM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_VMUsersBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_VMUsersBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_VMUsersBulkInsert]
(
      @VMUsersBulkData AS [dbo].[TransVMUser_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TransVMUser](
	[VmId],
	[UserId])

SELECT * FROM @VMUsersBulkData


END
GO


