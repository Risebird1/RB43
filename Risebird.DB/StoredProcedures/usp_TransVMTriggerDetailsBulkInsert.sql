/****** Object:  StoredProcedure [dbo].[usp_TransVMTriggerDetailsBulkInsert]    Script Date: 21-09-2020 12.06.08 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_TransVMTriggerDetailsBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_TransVMTriggerDetailsBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_TransVMTriggerDetailsBulkInsert]
(
      @TransVMTriggerDetailsBulkData AS [dbo].[TransVMTriggerDetails_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[TransVMTriggerDetails](
	[VMPropertyId] ,
	[LastAction],
	[ExpectedStatus])

SELECT * FROM @TransVMTriggerDetailsBulkData


END
GO


