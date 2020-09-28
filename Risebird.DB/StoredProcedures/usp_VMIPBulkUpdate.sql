/****** Object:  StoredProcedure [dbo].[usp_IRFProgressStatusBulkUpdate]    Script Date: 22-09-2020 12.27.44 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_VMIPBulkUpdate'))
BEGIN
    DROP PROCEDURE dbo.usp_VMIPBulkUpdate
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_VMIPBulkUpdate]
(
      @VMIPBulkData AS [dbo].[VMIP_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL


UPDATE VMProperties
      SET OSType = c2.OSType
      ,PrivateIP = c2.PrivateIP
	  ,PublicIP = c2.PublicIP
      FROM VMProperties c1
      INNER JOIN @VMIPBulkData c2
      ON c1.id = c2.id

END
GO


