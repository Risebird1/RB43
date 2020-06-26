/****** Object:  StoredProcedure [dbo].[usp_SelectabilityReportBulkInsert]    Script Date: 25-06-2020 7.29.20 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SelectabilityReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_SelectabilityReportBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_SelectabilityReportBulkInsert]
(
      @SelectabilityReportBulkData AS [dbo].[SelectabilityReport_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[SelectabilityReport](
	[RequiredLevelId] ,
	[Type],
	[ActualLevelId],
	[WeightageRequired],
	[WeightageActual],
	[Value],
	[CandidateId],
	[IRFId])

SELECT * FROM @SelectabilityReportBulkData


END
GO


