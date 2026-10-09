IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'BotInterviewQuestionVideos')
BEGIN 

CREATE TABLE [dbo].[BotInterviewQuestionVideos](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NULL,
	[VideoLink] [varchar](1000) NULL,
	[question] [nvarchar](max) NULL,
	[CreatedAt] [datetime] NULL,
	[BotSynthesizedVideoLink] [varchar](2000) NULL,
	[CandidateAnswer] [nvarchar](max) NULL,
	[BotAudioLink] [varchar](2000) NULL,
	[DeliveryHeadQuestionId] [int] NULL,
	[AnswerModifiedAt] [datetime] NULL,
	[AnswerModifiedBy] [varchar](100) NULL,
 CONSTRAINT [PK_BotInterviewQuestionVideos] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

ELSE
BEGIN

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BotSynthesizedVideoLink')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add BotSynthesizedVideoLink varchar(2000)
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateAnswer')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add CandidateAnswer nvarchar(4000)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'BotAudioLink')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add BotAudioLink varchar(2000)
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DeliveryHeadQuestionId')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add DeliveryHeadQuestionId int null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AnswerModifiedAt')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add AnswerModifiedAt datetime null
END
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'AnswerModifiedBy')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add AnswerModifiedBy varchar(100) null
END

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateAnswerAudioLink')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add CandidateAnswerAudioLink varchar(2000) null
END

IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'question')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    ALTER COLUMN question NVARCHAR(max);
END
IF EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'CandidateAnswer')
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    ALTER COLUMN CandidateAnswer NVARCHAR(max);
END

END
GO

-- =====================================================
-- Add unique index to prevent duplicate entries
-- =====================================================
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_BotInterviewQuestionVideos_SessionQuestion' 
    AND object_id = OBJECT_ID('BotInterviewQuestionVideos')
)
BEGIN
    -- Use filtered index to only apply uniqueness where DeliveryHeadQuestionId IS NOT NULL
    CREATE UNIQUE NONCLUSTERED INDEX UQ_BotInterviewQuestionVideos_SessionQuestion
    ON BotInterviewQuestionVideos (SessionId, DeliveryHeadQuestionId)
    WHERE DeliveryHeadQuestionId IS NOT NULL;
    PRINT '✓ Unique constraint added to BotInterviewQuestionVideos to prevent duplicates';
END
GO



-- ---------------------------------------------------------------------------------------------
-- Candidate transcript review.
--
-- The candidate now sees what speech-to-text produced and may correct it before submitting, so
-- CandidateAnswer is what they CONFIRMED. These three keep what the machine actually heard:
--
--   RawTranscript                what speech-to-text returned, before any correction. Never
--                                overwritten -- it is the tiebreaker on any dispute, alongside
--                                the recording itself.
--   TranscriptEditedByCandidate  1 when the confirmed text differs from the raw transcript.
--   TranscriptDivergencePct      how much it differs, 0-100. A small number is a mis-heard name
--                                or figure; a large one is worth a QC look.
--
-- NULL on every answer recorded before this existed, and on channels where review is switched off.
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'RawTranscript' AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add RawTranscript nvarchar(max) NULL
END
GO

IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'TranscriptEditedByCandidate' AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add TranscriptEditedByCandidate bit NULL
END
GO

IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'TranscriptDivergencePct' AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add TranscriptDivergencePct int NULL
END
GO

-- ---------------------------------------------------------------------------------------------
-- TranscriptTakeCount
--
-- How many recordings the candidate made before this answer was accepted. 1 is the normal case.
--
-- Anything higher means an attempt failed - nothing attached, a clipped recording, speech-to-text
-- erroring or returning noise - and the candidate was sent back to record again rather than being
-- dropped out of the interview, which is what used to happen. Read it as an equipment and
-- environment signal, not a candidate one: a channel with high takes across many candidates has a
-- problem of its own.
--
-- NULL on every answer recorded before this existed.
-- ---------------------------------------------------------------------------------------------
IF NOT EXISTS(
    SELECT * FROM sys.columns
    WHERE Name = N'TranscriptTakeCount' AND Object_ID = Object_ID(N'BotInterviewQuestionVideos'))
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    Add TranscriptTakeCount int NULL
END
GO
