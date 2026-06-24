-- Mirrors InterviewerAvailabilityController.IsFavorableAvailabilityIntent.
-- Returns 1 when an availability signal is favorable (interviewer indicated they are
-- available/interested with no blocking concern), otherwise 0.
CREATE OR ALTER FUNCTION [dbo].[fn_IsFavorableAvailabilityIntent]
(
    @AvailabilityIntent      NVARCHAR(MAX),
    @MailBodyIntent          NVARCHAR(MAX),
    @ConcernsOrNotInterested NVARCHAR(MAX)
)
RETURNS BIT
AS
BEGIN
    -- A non-trivial concern disqualifies the record.
    DECLARE @concern NVARCHAR(MAX) = LOWER(LTRIM(RTRIM(ISNULL(@ConcernsOrNotInterested, ''))));
    IF (@concern <> '' AND @concern NOT IN ('none', 'no', 'n/a', 'na', 'nil'))
        RETURN 0;

    DECLARE @intent NVARCHAR(MAX) = LOWER(LTRIM(RTRIM(ISNULL(@AvailabilityIntent, ''))));
    IF (@intent LIKE '%available%' OR @intent LIKE '%yes%' OR @intent LIKE '%interested%'
        OR @intent LIKE '%confirmed%' OR @intent LIKE '%willing%' OR @intent LIKE '%agree%'
        OR @intent LIKE '%positive%')
        RETURN 1;

    DECLARE @body NVARCHAR(MAX) = LOWER(LTRIM(RTRIM(ISNULL(@MailBodyIntent, ''))));
    IF (@body LIKE '%available%' OR @body LIKE '%yes%' OR @body LIKE '%interested%'
        OR @body LIKE '%confirmed%' OR @body LIKE '%willing%' OR @body LIKE '%agree%'
        OR @body LIKE '%positive%')
        RETURN 1;

    RETURN 0;
END
GO
