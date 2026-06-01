CREATE OR ALTER PROCEDURE [dbo].[usp_GetAllIABatchCallLogs]
    @Page     INT = 1,
    @PageSize INT = 50,
    @MagName  NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Filtered AS (
        SELECT *
        FROM IABatchCallLog
        WHERE (@MagName IS NULL OR @MagName = '' OR MagName = @MagName)
    )

    SELECT
        (SELECT COUNT(*) FROM Filtered) AS TotalCount,
        f.*
    FROM Filtered f
    ORDER BY f.InitiatedAt DESC
    OFFSET (@Page - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO
