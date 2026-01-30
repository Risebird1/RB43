CREATE PROCEDURE [dbo].[usp_GetOpenAICostSummaryByDateRange]
    @StartDate DATETIME2(7),
    @EndDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        CAST(LogDate AS DATE) AS [Date],
        OperationType,
        Model,
        COUNT(*) AS TotalCalls,
        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS SuccessfulCalls,
        SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) AS FailedCalls,
        ISNULL(SUM(TotalTokens), 0) AS TotalTokens,
        ISNULL(SUM(PromptTokens), 0) AS TotalPromptTokens,
        ISNULL(SUM(CompletionTokens), 0) AS TotalCompletionTokens,
        ISNULL(SUM(EstimatedCostUSD), 0) AS TotalCostUSD,
        ISNULL(AVG(ResponseTimeMs), 0) AS AvgResponseTimeMs,
        ISNULL(MIN(EstimatedCostUSD), 0) AS MinCostUSD,
        ISNULL(MAX(EstimatedCostUSD), 0) AS MaxCostUSD
    FROM OpenAIApiLogs
    WHERE LogDate >= @StartDate AND LogDate <= @EndDate
    GROUP BY CAST(LogDate AS DATE), OperationType, Model
    ORDER BY [Date] DESC, TotalCostUSD DESC;
END
GO
