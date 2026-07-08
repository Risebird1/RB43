-- Migration: store candidate answer audio URL for adaptive AI interviews (Report playback).
-- Run this BEFORE usp_SaveBotInterviewQuestionVideos.sql.

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE Name = N'CandidateAnswerAudioLink'
      AND Object_ID = Object_ID(N'BotInterviewQuestionVideos')
)
BEGIN
    ALTER TABLE BotInterviewQuestionVideos
    ADD CandidateAnswerAudioLink VARCHAR(2000) NULL;
END
GO
