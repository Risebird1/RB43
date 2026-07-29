IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetSchedulerEventList'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSchedulerEventList
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*
  Light paginated scheduler list — newest first (id DESC).
  Result set 1: total count
  Result set 2: page rows
*/
CREATE PROCEDURE dbo.usp_GetSchedulerEventList
    @Search   NVARCHAR(200) = NULL,
    @Offset   INT = 0,
    @PageSize INT = 25
AS
BEGIN
    SET NOCOUNT ON;

    IF @Offset IS NULL OR @Offset < 0 SET @Offset = 0;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 25;
    IF @PageSize > 200 SET @PageSize = 200;

    DECLARE @SearchTrim NVARCHAR(200) = NULLIF(LTRIM(RTRIM(@Search)), N'');

    SELECT COUNT(1)
    FROM dbo.ScheduleInterviewEvent WITH (NOLOCK)
    WHERE ISNULL(IsSingleUse, 0) <> 1
      AND (
            @SearchTrim IS NULL
            OR EventName LIKE N'%' + @SearchTrim + N'%'
            OR SkillChannel LIKE N'%' + @SearchTrim + N'%'
          );

    SELECT
        id,
        EventName,
        SkillChannel,
        duration,
        StartDate,
        EndDate,
        Uniqueid,
        IsActive
    FROM dbo.ScheduleInterviewEvent WITH (NOLOCK)
    WHERE ISNULL(IsSingleUse, 0) <> 1
      AND (
            @SearchTrim IS NULL
            OR EventName LIKE N'%' + @SearchTrim + N'%'
            OR SkillChannel LIKE N'%' + @SearchTrim + N'%'
          )
    ORDER BY id DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ScheduleInterviewEvent_List'
      AND object_id = OBJECT_ID(N'dbo.ScheduleInterviewEvent')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ScheduleInterviewEvent_List
    ON dbo.ScheduleInterviewEvent (id DESC)
    INCLUDE (EventName, SkillChannel, duration, StartDate, EndDate, Uniqueid, IsActive, IsSingleUse);
END
GO
