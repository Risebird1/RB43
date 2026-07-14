IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpsertRowActions'))
BEGIN
    DROP PROCEDURE dbo.usp_UpsertRowActions
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpsertRowActions]
    @refType varchar(20),
    @refId varchar(20),
    @actionsTaken varchar(500),
    @updatedBy varchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.RowActionLog', N'U') IS NULL
    BEGIN
        RETURN;
    END

    MERGE dbo.RowActionLog AS target
    USING (SELECT @refType AS RefType, @refId AS RefId) AS source
        ON target.RefType = source.RefType
       AND target.RefId = source.RefId
    WHEN MATCHED THEN
        UPDATE SET
            ActionsTaken = @actionsTaken,
            UpdatedBy = @updatedBy,
            UpdatedAt = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT (RefType, RefId, ActionsTaken, UpdatedBy, UpdatedAt)
        VALUES (@refType, @refId, @actionsTaken, @updatedBy, GETDATE());
END
GO
