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
    INSERT INTO dbo.TrialFlags (
        InterviewerId, IrfId, ChannelId, TriggeringInterviewId, TriggeringInterviewDate,
        TranscriptQualityPass, ReportQualityPass, DecisionQualityPass, CandidateRatingPass,
        JoinTimePass, InterviewDurationPass,
        TranscriptQualityScore, CandidateRating, JoinTimeMinutes, InterviewDurationMinutes,
        OverallVerdict, Severity, ReviewStatus, FlaggedAtUtc) 
    VALUES (
        @InterviewerId, @IrfId, @ChannelId, @TriggeringInterviewId, @TriggeringInterviewDate,
        @TranscriptQualityPass, @ReportQualityPass, @DecisionQualityPass, @CandidateRatingPass,
        @JoinTimePass, @InterviewDurationPass,
        @TranscriptQualityScore, @CandidateRating, @JoinTimeMinutes, @InterviewDurationMinutes,
        @OverallVerdict, @Severity, 'pending', GETUTCDATE()); 
    SELECT CAST(1 AS BIT) AS Inserted, CAST(SCOPE_IDENTITY() AS INT) AS TrialFlagId;
END
GO