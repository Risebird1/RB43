-- ============================================
-- How to Flag a Question for Typing Speed Assessment
-- ============================================

-- IMPORTANT: Only ONE question per IRF should be flagged as a typing speed assessment question
-- The question can be in any order - the system will trigger the typing test when that question appears

-- Step 1: View all questions for a specific IRF
-- Replace [YourIRFId] with the actual IRF ID
SELECT 
    id,
    IRFId,
    Skill,
    SkillName,
    Comment AS QuestionText,
    IsTypingSpeedAssessment,
    AIQuestionVideoLink
FROM SkillChannelSampleQuestions
WHERE IRFId = [YourIRFId]
ORDER BY id;

-- Step 2: Check if any question is already flagged for this IRF
SELECT 
    id,
    Comment AS QuestionText,
    IsTypingSpeedAssessment
FROM SkillChannelSampleQuestions
WHERE IRFId = [YourIRFId] 
  AND IsTypingSpeedAssessment = 1;

-- Step 3: Clear any existing typing speed flags for this IRF (if needed)
-- This ensures only ONE question per IRF is flagged
UPDATE SkillChannelSampleQuestions
SET IsTypingSpeedAssessment = 0
WHERE IRFId = [YourIRFId] 
  AND IsTypingSpeedAssessment = 1;

-- Step 4: Flag a specific question for typing speed assessment
-- Replace [QuestionId] and [YourIRFId] with actual values
UPDATE SkillChannelSampleQuestions
SET IsTypingSpeedAssessment = 1
WHERE id = [QuestionId] 
  AND IRFId = [YourIRFId];

-- Verify the change
SELECT 
    id,
    IRFId,
    Comment AS QuestionText,
    IsTypingSpeedAssessment
FROM SkillChannelSampleQuestions
WHERE id = [QuestionId];


-- ============================================
-- Example: Flag question ID 1234 for IRF 567
-- ============================================

-- First, clear any existing flags for IRF 567
/*
UPDATE SkillChannelSampleQuestions
SET IsTypingSpeedAssessment = 0
WHERE IRFId = 567 
  AND IsTypingSpeedAssessment = 1;

-- Then, flag question 1234
UPDATE SkillChannelSampleQuestions
SET IsTypingSpeedAssessment = 1
WHERE id = 1234 
  AND IRFId = 567;

-- Verify
SELECT 
    id,
    IRFId,
    Comment AS QuestionText,
    IsTypingSpeedAssessment
FROM SkillChannelSampleQuestions
WHERE IRFId = 567;
*/


-- ============================================
-- View Typing Speed Results for a Candidate
-- ============================================

-- Replace [CandidateEmail] with actual email
SELECT 
    tsa.id,
    tsa.SessionId,
    im.candidateEmail,
    im.candidateName,
    tsa.QuestionId,
    biqv.question,
    tsa.TimeTakenInSeconds,
    tsa.AccuracyPercentage,
    tsa.WordsPerMinute,
    tsa.TotalCharacters,
    tsa.CorrectCharacters,
    tsa.TimeStarted,
    tsa.TimeCompleted,
    tsa.CreatedAt
FROM TypingSpeedAssessment tsa
INNER JOIN InterviewSessions isess ON isess.SessionId = CAST(tsa.SessionId AS UNIQUEIDENTIFIER)
INNER JOIN InterviewMeetings im ON im.SessionId = isess.SessionId
LEFT JOIN BotInterviewQuestionVideos biqv ON biqv.DeliveryHeadQuestionId = tsa.QuestionId 
    AND biqv.SessionId = tsa.SessionId
WHERE im.candidateEmail = '[CandidateEmail]'
ORDER BY tsa.CreatedAt DESC;


-- ============================================
-- View All Typing Speed Results for an IRF
-- ============================================

-- Replace [YourIRFId] with actual IRF ID
SELECT 
    tsa.id,
    im.candidateEmail,
    im.candidateName,
    tsa.WordsPerMinute,
    tsa.AccuracyPercentage,
    tsa.TimeTakenInSeconds,
    tsa.TimeCompleted,
    CASE 
        WHEN tsa.WordsPerMinute >= 60 AND tsa.AccuracyPercentage >= 95 THEN 'Excellent'
        WHEN tsa.WordsPerMinute >= 40 AND tsa.AccuracyPercentage >= 90 THEN 'Good'
        WHEN tsa.WordsPerMinute >= 30 AND tsa.AccuracyPercentage >= 85 THEN 'Average'
        ELSE 'Below Average'
    END AS TypingSkillLevel
FROM TypingSpeedAssessment tsa
INNER JOIN InterviewSessions isess ON isess.SessionId = CAST(tsa.SessionId AS UNIQUEIDENTIFIER)
INNER JOIN InterviewMeetings im ON im.SessionId = isess.SessionId
WHERE im.IRFId = [YourIRFId]
ORDER BY tsa.WordsPerMinute DESC, tsa.AccuracyPercentage DESC;


-- ============================================
-- Statistics: Average Typing Speed by IRF
-- ============================================

SELECT 
    im.IRFId,
    irf.JDTiltle,
    COUNT(DISTINCT tsa.id) AS TotalCandidatesTested,
    ROUND(AVG(tsa.WordsPerMinute), 2) AS AvgWPM,
    ROUND(AVG(tsa.AccuracyPercentage), 2) AS AvgAccuracy,
    ROUND(AVG(tsa.TimeTakenInSeconds), 2) AS AvgTimeTaken,
    MAX(tsa.WordsPerMinute) AS HighestWPM,
    MIN(tsa.WordsPerMinute) AS LowestWPM
FROM TypingSpeedAssessment tsa
INNER JOIN InterviewSessions isess ON isess.SessionId = CAST(tsa.SessionId AS UNIQUEIDENTIFIER)
INNER JOIN InterviewMeetings im ON im.SessionId = isess.SessionId
LEFT JOIN IRFInterviewDetails irf ON irf.Id = im.IRFId
GROUP BY im.IRFId, irf.JDTiltle
ORDER BY AvgWPM DESC;


-- ============================================
-- Remove Typing Speed Flag from a Question
-- ============================================

-- To disable typing speed assessment for a question
UPDATE SkillChannelSampleQuestions
SET IsTypingSpeedAssessment = 0
WHERE id = [QuestionId];


-- ============================================
-- Best Practices
-- ============================================

/*
1. Only flag ONE question per IRF as a typing speed assessment
2. Choose a question that's appropriate for typing assessment (not a video-only question)
3. The flagged question can be in any position - the test will trigger when it appears
4. Ensure the question text itself instructs the candidate appropriately
5. Review typing speed results along with other interview metrics
6. Consider role requirements when evaluating typing speed results
   - Administrative roles: 40-60 WPM minimum
   - Data entry roles: 60-80 WPM minimum
   - Transcription roles: 80+ WPM minimum
7. Accuracy is as important as speed - look for 90%+ accuracy
*/
