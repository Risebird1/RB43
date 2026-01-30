CREATE PROCEDURE [dbo].[usp_GetOpenAICostByInterviewer]
    @StartDate DATETIME2(7) = NULL,
    @EndDate DATETIME2(7) = NULL,
    @Top INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Top)
        InterviewerId,
        COUNT(*) AS TotalCalls,
        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessfulCalls,
        ISNULL(SUM(TotalTokens), 0) AS TotalTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) AS TotalCostUSD,
        MIN(LogDate) AS FirstCallDate,
        MAX(LogDate) AS LastCallDate
    FROM OpenAIApiLogs
    WHERE InterviewerId IS NOT NULL
        AND (@StartDate IS NULL OR LogDate >= @StartDate)
        AND (@EndDate IS NULL OR LogDate <= @EndDate)
    GROUP BY InterviewerId
    ORDER BY TotalCostUSD DESC;
END
GO
