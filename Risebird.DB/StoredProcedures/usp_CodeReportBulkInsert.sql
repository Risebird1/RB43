

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CodeReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_CodeReportBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
  
CREATE PROCEDURE [dbo].[usp_CodeReportBulkInsert]      
(      
      @CodeReportBulkData AS [dbo].[CodeReport_TableType] READONLY      
)      
AS      
BEGIN      
      
SET NOCOUNT ON;      
      
--This row performs bulk copy of object DataTable to Table in SQL      
INSERT INTO [dbo].[CodeInviteReports]   
SELECT * FROM @CodeReportBulkData      
      
      
END
Go