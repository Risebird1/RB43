-- =============================================
-- Recalculate OpenAIApiLogs token counts and costs written before the October 2026 logger fix.
--
-- Two defects in OpenAIApiLogger made historical AI cost wrong in opposite directions:
--   1. Responses API calls (every OpenAIResponsesHelper / stored-prompt call) report usage as
--      usage.input_tokens / usage.output_tokens. The logger only read prompt_tokens /
--      completion_tokens, so these rows were logged with 0 tokens and $0 cost.
--   2. gpt-4.1, gpt-4.1-mini and gpt-4.1-nano were matched by the legacy "gpt-4" rule and billed
--      at $30/$60 per 1M tokens instead of $2/$8, $0.40/$1.60 and $0.10/$0.40.
--
-- This script backs up the current values, re-reads token usage from ResponsePayload where it is
-- missing, and re-prices every row with tokens using the corrected rates (including the cached-input
-- discount). Run it once after deploying the logger fix. It is safe to re-run: it reads only the
-- stored payloads and model names, never the previously computed cost.
-- =============================================

SET NOCOUNT ON;

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OpenAIApiLogs_CostBackup_202610]') AND type IN (N'U'))
BEGIN
    SELECT Id, EstimatedCostUSD, Model, PromptTokens, CompletionTokens, TotalTokens
    INTO OpenAIApiLogs_CostBackup_202610
    FROM OpenAIApiLogs;

    PRINT 'Backup created: OpenAIApiLogs_CostBackup_202610';
END
GO

-- Rates per 1K tokens, matching OpenAIApiLogger.CalculateCost. Order matters: more specific names first.
CREATE OR ALTER FUNCTION dbo.fn_CalculateOpenAICostV2
(
    @Model            NVARCHAR(100),
    @PromptTokens     INT,
    @CompletionTokens INT,
    @CachedTokens     INT
)
RETURNS DECIMAL(18, 6)
AS
BEGIN
    DECLARE @In DECIMAL(18, 6), @Out DECIMAL(18, 6), @Cached DECIMAL(18, 6);
    SET @Model = LOWER(ISNULL(@Model, ''));

    IF @Model = '' OR (ISNULL(@PromptTokens, 0) = 0 AND ISNULL(@CompletionTokens, 0) = 0)
        RETURN 0;

    IF      @Model LIKE '%gpt-4o-mini%'      SELECT @In = 0.00015,  @Out = 0.0006, @Cached = 0.000075
    ELSE IF @Model LIKE '%gpt-4o%'           SELECT @In = 0.0025,   @Out = 0.0100, @Cached = 0.00125
    ELSE IF @Model LIKE '%gpt-4.1-nano%'     SELECT @In = 0.0001,   @Out = 0.0004, @Cached = 0.000025
    ELSE IF @Model LIKE '%gpt-4.1-mini%'     SELECT @In = 0.0004,   @Out = 0.0016, @Cached = 0.0001
    ELSE IF @Model LIKE '%gpt-4.1%'          SELECT @In = 0.0020,   @Out = 0.0080, @Cached = 0.0005
    ELSE IF @Model LIKE '%gpt-5-nano%'       SELECT @In = 0.00005,  @Out = 0.0004, @Cached = 0.000005
    ELSE IF @Model LIKE '%gpt-5-mini%'       SELECT @In = 0.00025,  @Out = 0.0020, @Cached = 0.000025
    ELSE IF @Model LIKE '%gpt-5%'            SELECT @In = 0.00125,  @Out = 0.0100, @Cached = 0.000125
    ELSE IF @Model LIKE '%gpt-4-turbo%' OR @Model LIKE '%gpt-4-1106%' OR @Model LIKE '%gpt-4-0125%'
                                             SELECT @In = 0.01,     @Out = 0.03,   @Cached = NULL
    ELSE IF @Model LIKE '%gpt-4%'            SELECT @In = 0.03,     @Out = 0.06,   @Cached = NULL
    ELSE IF @Model LIKE '%gpt-3.5-turbo%'    SELECT @In = 0.0015,   @Out = 0.002,  @Cached = NULL
    ELSE                                     SELECT @In = 0.0025,   @Out = 0.0100, @Cached = 0.00125;

    DECLARE @Prompt INT = ISNULL(@PromptTokens, 0);
    DECLARE @CachedCount INT = CASE WHEN ISNULL(@CachedTokens, 0) < 0 THEN 0
                                    WHEN ISNULL(@CachedTokens, 0) > @Prompt THEN @Prompt
                                    ELSE ISNULL(@CachedTokens, 0) END;

    RETURN ROUND(
          ((@Prompt - @CachedCount) / 1000.0 * @In)
        + (@CachedCount / 1000.0 * ISNULL(@Cached, @In))
        + (ISNULL(@CompletionTokens, 0) / 1000.0 * @Out), 6);
END
GO

-- 1. Fill in token counts for Responses API rows that were logged with none. Walks the table once
--    in Id ranges so the log table is never locked for long. (An earlier version looped "until no
--    row matches", which never ended when a response really reported 0 tokens: such a row is
--    "filled" with 0 again and still matches.)
DECLARE @Batch INT = 5000, @LastId BIGINT = 0, @MaxId BIGINT, @Filled INT = 0;
SELECT @MaxId = MAX(Id) FROM OpenAIApiLogs;

WHILE @LastId < ISNULL(@MaxId, 0)
BEGIN
    UPDATE l
    SET PromptTokens     = TRY_CAST(JSON_VALUE(l.ResponsePayload, '$.usage.input_tokens') AS INT),
        CompletionTokens = TRY_CAST(JSON_VALUE(l.ResponsePayload, '$.usage.output_tokens') AS INT),
        TotalTokens      = ISNULL(TRY_CAST(JSON_VALUE(l.ResponsePayload, '$.usage.total_tokens') AS INT),
                                  ISNULL(TRY_CAST(JSON_VALUE(l.ResponsePayload, '$.usage.input_tokens') AS INT), 0)
                                + ISNULL(TRY_CAST(JSON_VALUE(l.ResponsePayload, '$.usage.output_tokens') AS INT), 0))
    FROM OpenAIApiLogs l
    WHERE l.Id > @LastId AND l.Id <= @LastId + @Batch
      AND ISNULL(l.PromptTokens, 0) = 0
      AND ISNULL(l.CompletionTokens, 0) = 0
      AND l.ResponsePayload IS NOT NULL
      AND ISJSON(l.ResponsePayload) = 1
      AND JSON_VALUE(l.ResponsePayload, '$.usage.input_tokens') IS NOT NULL;

    SET @Filled += @@ROWCOUNT;
    SET @LastId += @Batch;
END
PRINT CONCAT('Token counts filled: ', @Filled);
GO

-- 2. Re-price every row that has tokens. Whisper and TTS rows are priced by audio length / characters,
--    and Realtime (voice) sessions by audio and text tokens separately, so all three are left alone.
DECLARE @Batch INT = 5000, @LastId BIGINT = 0, @MaxId BIGINT;
SELECT @MaxId = MAX(Id) FROM OpenAIApiLogs;

WHILE @LastId < ISNULL(@MaxId, 0)
BEGIN
    UPDATE l
    SET EstimatedCostUSD = dbo.fn_CalculateOpenAICostV2(
            l.Model, l.PromptTokens, l.CompletionTokens,
            CASE WHEN l.ResponsePayload IS NOT NULL AND ISJSON(l.ResponsePayload) = 1
                 THEN COALESCE(TRY_CAST(JSON_VALUE(l.ResponsePayload, '$.usage.input_tokens_details.cached_tokens') AS INT),
                               TRY_CAST(JSON_VALUE(l.ResponsePayload, '$.usage.prompt_tokens_details.cached_tokens') AS INT),
                               0)
                 ELSE 0 END)
    FROM OpenAIApiLogs l
    WHERE l.Id > @LastId AND l.Id <= @LastId + @Batch
      AND (ISNULL(l.PromptTokens, 0) > 0 OR ISNULL(l.CompletionTokens, 0) > 0)
      AND ISNULL(l.ApiEndpoint, '') NOT LIKE '%/audio/%'
      AND ISNULL(l.ApiEndpoint, '') NOT LIKE '%/realtime%';

    SET @LastId = @LastId + @Batch;
END
PRINT 'Costs re-priced.';
GO

-- Before / after by model, to sanity-check the change.
SELECT l.Model,
       COUNT(*)                     AS Calls,
       SUM(b.EstimatedCostUSD)      AS CostBeforeUSD,
       SUM(l.EstimatedCostUSD)      AS CostAfterUSD
FROM OpenAIApiLogs l
JOIN OpenAIApiLogs_CostBackup_202610 b ON b.Id = l.Id
GROUP BY l.Model
ORDER BY SUM(l.EstimatedCostUSD) DESC;
GO
