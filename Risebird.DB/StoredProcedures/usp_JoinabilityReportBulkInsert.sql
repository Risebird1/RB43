/****** Object:  StoredProcedure [dbo].[usp_JoinabilityReportBulkInsert]    Script Date: 25-06-2020 7.29.06 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_JoinabilityReportBulkInsert'))
BEGIN
    DROP PROCEDURE dbo.usp_JoinabilityReportBulkInsert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_JoinabilityReportBulkInsert]
(
      @JoinabilityReportBulkData AS [dbo].[JoinabilityReport_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL
INSERT INTO [dbo].[JoinabilityReport](
	[RequiredLevelId] ,
	[Type],
	[ActualLevelId],
	[WeightageRequired],
	[WeightageActual],
	[Value],
	[CandidateId],
	[IRFId])

SELECT * FROM @JoinabilityReportBulkData


END
GO


