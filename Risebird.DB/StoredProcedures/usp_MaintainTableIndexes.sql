-- =============================================================================
-- usp_MaintainTableIndexes
--
-- Reorganizes and rebuilds all indexes on a single dbo table.
-- Called by the IndexMaintenanceJob Azure Timer Function (daily).
--
-- Parameters
--   @TableName            SYSNAME  — table name only (dbo schema)
--   @MinReorganizePercent TINYINT  — REORGANIZE when fragmentation >= this (default 5)
--   @MinRebuildPercent    TINYINT  — REBUILD when fragmentation >= this (default 30)
--   @ForceFullMaintenance BIT      — when 1, REORGANIZE ALL then REBUILD ALL (ignores thresholds)
--
-- Security: @TableName is validated against sys.tables in dbo before any dynamic SQL.
-- =============================================================================

IF OBJECT_ID(N'[dbo].[usp_MaintainTableIndexes]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[usp_MaintainTableIndexes];
GO

CREATE PROCEDURE [dbo].[usp_MaintainTableIndexes]
    @TableName            SYSNAME,
    @MinReorganizePercent TINYINT = 5,
    @MinRebuildPercent    TINYINT = 30,
    @ForceFullMaintenance BIT     = 0
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SchemaName SYSNAME = N'dbo';
    DECLARE @ObjectId   INT;
    DECLARE @Sql        NVARCHAR(MAX);
    DECLARE @StartedAt  DATETIME2 = SYSUTCDATETIME();

    IF @TableName IS NULL OR LTRIM(RTRIM(@TableName)) = N''
    BEGIN
        RAISERROR('usp_MaintainTableIndexes: @TableName is required.', 16, 1);
        RETURN;
    END;

    SELECT @ObjectId = t.object_id
    FROM   sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE  s.name  = @SchemaName
      AND  t.name  = @TableName;

    IF @ObjectId IS NULL
    BEGIN
        RAISERROR('usp_MaintainTableIndexes: table dbo.%s was not found.', 16, 1, @TableName);
        RETURN;
    END;

    IF @ForceFullMaintenance = 1
    BEGIN
        SET @Sql = N'ALTER INDEX ALL ON '
                 + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName)
                 + N' REORGANIZE WITH ( LOB_COMPACTION = ON );';
        EXEC sys.sp_executesql @Sql;

        SET @Sql = N'ALTER INDEX ALL ON '
                 + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName)
                 + N' REBUILD;';
        EXEC sys.sp_executesql @Sql;
    END
    ELSE
    BEGIN
        DECLARE @IndexName              SYSNAME;
        DECLARE @AvgFragmentation       FLOAT;
        DECLARE @PartitionNumber        INT;
        DECLARE @IsPartitioned          BIT;
        DECLARE @ReorganizedCount       INT = 0;
        DECLARE @RebuiltCount           INT = 0;
        DECLARE @SkippedCount           INT = 0;

        DECLARE index_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT
                i.name,
                ps.avg_fragmentation_in_percent,
                ps.partition_number,
                CASE WHEN part_counts.cnt > 1 THEN 1 ELSE 0 END
            FROM sys.indexes i
            CROSS APPLY sys.dm_db_index_physical_stats(DB_ID(), @ObjectId, i.index_id, NULL, N'LIMITED') ps
            CROSS APPLY (
                SELECT COUNT(DISTINCT p.partition_number) AS cnt
                FROM sys.partitions p
                WHERE p.object_id = i.object_id
                  AND p.index_id  = i.index_id
            ) part_counts
            WHERE i.object_id = @ObjectId
              AND i.index_id  > 0
              AND i.is_disabled = 0
              AND ps.index_id = i.index_id
              AND ps.avg_fragmentation_in_percent IS NOT NULL;

        OPEN index_cursor;
        FETCH NEXT FROM index_cursor INTO @IndexName, @AvgFragmentation, @PartitionNumber, @IsPartitioned;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @Sql = N'ALTER INDEX '
                     + QUOTENAME(@IndexName) + N' ON '
                     + QUOTENAME(@SchemaName) + N'.' + QUOTENAME(@TableName);

            IF @AvgFragmentation >= @MinRebuildPercent
            BEGIN
                IF @IsPartitioned = 1
                    SET @Sql = @Sql + N' REBUILD PARTITION = ' + CAST(@PartitionNumber AS NVARCHAR(11)) + N';';
                ELSE
                    SET @Sql = @Sql + N' REBUILD;';

                EXEC sys.sp_executesql @Sql;
                SET @RebuiltCount += 1;
            END
            ELSE IF @AvgFragmentation >= @MinReorganizePercent
            BEGIN
                IF @IsPartitioned = 1
                    SET @Sql = @Sql + N' REORGANIZE PARTITION = ' + CAST(@PartitionNumber AS NVARCHAR(11))
                             + N' WITH (LOB_COMPACTION = ON);';
                ELSE
                    SET @Sql = @Sql + N' REORGANIZE WITH (LOB_COMPACTION = ON);';

                EXEC sys.sp_executesql @Sql;
                SET @ReorganizedCount += 1;
            END
            ELSE
            BEGIN
                SET @SkippedCount += 1;
            END;

            FETCH NEXT FROM index_cursor INTO @IndexName, @AvgFragmentation, @PartitionNumber, @IsPartitioned;
        END;

        CLOSE index_cursor;
        DEALLOCATE index_cursor;

        SELECT
            @TableName            AS TableName,
            @ReorganizedCount      AS IndexesReorganized,
            @RebuiltCount          AS IndexesRebuilt,
            @SkippedCount          AS IndexesSkipped,
            @StartedAt             AS StartedAtUtc,
            SYSUTCDATETIME()       AS CompletedAtUtc,
            @ForceFullMaintenance AS ForceFullMaintenance;
        RETURN;
    END;

    SELECT
        @TableName            AS TableName,
        NULL                  AS IndexesReorganized,
        NULL                  AS IndexesRebuilt,
        NULL                  AS IndexesSkipped,
        @StartedAt             AS StartedAtUtc,
        SYSUTCDATETIME()       AS CompletedAtUtc,
        @ForceFullMaintenance AS ForceFullMaintenance;
END
GO
