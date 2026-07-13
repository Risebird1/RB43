CREATE OR ALTER PROCEDURE [dbo].[usp_InsertTrialFlag]
    @InterviewerId              INT,
    @IrfId                      INT,
    @ChannelId                  INT,
    @TriggeringInterviewId      INT,
    @TriggeringInterviewDate    DATETIME,
    @TranscriptQualityPass      BIT,
    @ReportQualityPass          BIT,
    @DecisionQualityPass        BIT,
    @CandidateRatingPass        BIT,
    @JoinTimePass               BIT,
    @InterviewDurationPass      BIT,
    @TranscriptQualityScore     DECIMAL(5,2) = NULL,
    @CandidateRating            DECIMAL(5,2) = NULL,
    @JoinTimeMinutes            DECIMAL(8,2) = NULL,
    @InterviewDurationMinutes   DECIMAL(8,2) = NULL,
    @OverallVerdict             VARCHAR(30),
    @Severity                   VARCHAR(4) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF OBJECT_ID(N'dbo.TrialFlags', N'U') IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS Inserted, CAST(0 AS INT) AS TrialFlagId;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.TrialFlags WHERE InterviewerId = @InterviewerId AND IrfId = @IrfId)
    BEGIN
        SELECT CAST(0 AS BIT) AS Inserted, tf.Id AS TrialFlagId
        FROM dbo.TrialFlags tf
        WHERE tf.InterviewerId = @InterviewerId AND tf.IrfId = @IrfId;
        RETURN;
    END

    INSERT INTO dbo.TrialFlags (
        InterviewerId, IrfId, ChannelId, TriggeringInterviewId, TriggeringInterviewDate,
        TranscriptQualityPass, ReportQualityPass, DecisionQualityPass, CandidateRatingPass, JoinTimePass, InterviewDurationPass,
        TranscriptQualityScore, CandidateRating, JoinTimeMinutes, InterviewDurationMinutes,
        OverallVerdict, Severity, ReviewStatus, FlaggedAtUtc)
    VALUES (
        @InterviewerId, @IrfId, @ChannelId, @TriggeringInterviewId, @TriggeringInterviewDate,
        @TranscriptQualityPass, @ReportQualityPass, @DecisionQualityPass, @CandidateRatingPass, @JoinTimePass, @InterviewDurationPass,
        @TranscriptQualityScore, @CandidateRating, @JoinTimeMinutes, @InterviewDurationMinutes,
        @OverallVerdict, @Severity, 'pending', GETUTCDATE());

    SELECT CAST(1 AS BIT) AS Inserted, CAST(SCOPE_IDENTITY() AS INT) AS TrialFlagId;
END
GO
