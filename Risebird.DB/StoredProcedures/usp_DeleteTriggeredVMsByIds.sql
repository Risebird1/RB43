IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteTriggeredVMsByIds'))
BEGIN
    DROP PROCEDURE dbo.usp_DeleteTriggeredVMsByIds
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_DeleteTriggeredVMsByIds]
    @Ids VARCHAR(MAX)
AS
BEGIN
    DELETE t
    FROM TransVMTriggerDetails t
    INNER JOIN STRING_SPLIT(@Ids, ',') s ON t.id = TRY_CAST(s.value AS INT)
    WHERE TRY_CAST(s.value AS INT) IS NOT NULL
END
GO
