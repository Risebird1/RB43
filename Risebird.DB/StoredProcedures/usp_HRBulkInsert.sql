/****** Object:  StoredProcedure [dbo].[usp_HRBulkInsert]    Script Date: 24-06-2020 1.33.57 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_HRBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_HRBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_HRBulkInsert]
(
      @HRBulkData AS [dbo].[TransHR_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TransHR](
	[IRFId] ,
	[HRId])
	

SELECT hrbd.IRFId, HR.id as HRId FROM @HRBulkData hrbd left join HR on hrbd.UserId = HR.UserId


END
GO


