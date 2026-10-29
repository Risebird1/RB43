IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetOpenAICostOptimizationReport'))
    DROP PROCEDURE dbo.usp_GetOpenAICostOptimizationReport
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Where OpenAI money goes, per operation and model, and how much of it was spent asking a question
-- that had already been asked: the evidence for OpenAI:CostOptimization (which operations to move
-- to a cheaper model, which to put in the response cache).
--
-- A duplicate is a successful call whose request body is byte-identical to an earlier one in the
-- window for the same operation: same model, prompt, variables and input. Its cost is what the
-- response cache would have saved. Calls answered from the cache are logged with
-- ApiMethod = 'CACHE_HIT' and the cost they avoided in RequestPayload.avoidedCostUSD.
--
-- Hashing request payloads reads them in full, so keep the window to a few weeks.
CREATE PROCEDURE usp_GetOpenAICostOptimizationReport
    @StartDate DATE,
    @EndDate   DATE   -- inclusive
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CAST(@StartDate AS DATETIME);
    DECLARE @To   DATETIME = DATEADD(DAY, 1, CAST(@EndDate AS DATETIME));

    ;WITH L AS (
        SELECT ISNULL(NULLIF(l.OperationType, ''), '(not set)') AS OperationType,
               ISNULL(NULLIF(l.Model, ''), '(unknown)')         AS Model,
               CASE WHEN l.ApiMethod = 'CACHE_HIT' THEN 1 ELSE 0 END AS IsCacheHit,
               l.IsSuccess,
               ISNULL(l.EstimatedCostUSD, 0) AS CostUSD,
               l.PromptTokens,
               l.CompletionTokens,
               CASE WHEN ISNULL(l.ApiMethod, '') <> 'CACHE_HIT' AND l.IsSuccess = 1
                         AND l.RequestPayload IS NOT NULL AND l.ApiMethod = 'POST'
                    THEN HASHBYTES('SHA2_256', l.RequestPayload) END AS RequestHash,
               CASE WHEN l.ApiMethod = 'CACHE_HIT'
                    THEN TRY_CAST(JSON_VALUE(l.RequestPayload, '$.avoidedCostUSD') AS DECIMAL(18, 6)) END AS AvoidedUSD,
               CASE WHEN l.ApiEndpoint LIKE '%/responses' THEN 1 ELSE 0 END AS IsResponsesApi
        FROM OpenAIApiLogs l
        WHERE l.LogDate >= @From AND l.LogDate < @To
    ),
    R AS (
        SELECT L.*,
               CASE WHEN RequestHash IS NULL THEN 0
                    ELSE ROW_NUMBER() OVER (PARTITION BY OperationType, RequestHash ORDER BY (SELECT NULL)) END AS Occurrence
        FROM L
    )
    SELECT OperationType,
           Model,
           SUM(CASE WHEN IsCacheHit = 0 THEN 1 ELSE 0 END)                       AS Calls,
           SUM(IsCacheHit)                                                       AS CacheHits,
           SUM(CASE WHEN IsCacheHit = 0 AND IsSuccess = 0 THEN 1 ELSE 0 END)     AS FailedCalls,
           SUM(CostUSD)                                                          AS CostUSD,
           AVG(CASE WHEN IsCacheHit = 0 THEN CAST(PromptTokens AS FLOAT) END)     AS AvgPromptTokens,
           AVG(CASE WHEN IsCacheHit = 0 THEN CAST(CompletionTokens AS FLOAT) END) AS AvgCompletionTokens,
           SUM(CASE WHEN Occurrence > 1 THEN 1 ELSE 0 END)                       AS DuplicateCalls,
           SUM(CASE WHEN Occurrence > 1 THEN CostUSD ELSE 0 END)                 AS DuplicateCostUSD,
           SUM(ISNULL(AvoidedUSD, 0))                                            AS AvoidedCostUSD,
           -- Only Responses API operations (OpenAIResponsesHelper) can be re-routed or cached by config.
           CAST(MAX(IsResponsesApi) AS BIT)                                      AS ViaResponsesApi
    FROM R
    GROUP BY OperationType, Model
    ORDER BY SUM(CostUSD) DESC;
END
GO
