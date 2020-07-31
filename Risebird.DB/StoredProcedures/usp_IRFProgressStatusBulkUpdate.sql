/****** Object:  StoredProcedure [dbo].[usp_IRFProgressStatusBulkUpdate]    Script Date: 29-07-2020 10.49.09 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_IRFProgressStatusBulkUpdate'))
BEGIN
    DROP PROCEDURE dbo.usp_IRFProgressStatusBulkUpdate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_IRFProgressStatusBulkUpdate]
(
      @ProgressBulkData AS [dbo].[IRFProgressStatus_TableType] READONLY
)
AS
BEGIN

SET NOCOUNT ON;

--This row performs bulk copy of object DataTable to Table in SQL

UPDATE IRF
      SET idProgress = c2.idProgress
      ,Comment = c2.Comment
      FROM IRF c1
      INNER JOIN @ProgressBulkData c2
      ON c1.id = c2.id


END
GO


