-- ============================================
-- How to Flag a Question for Listening Comprehension Assessment
-- ============================================

-- A listening comprehension question needs FOUR things set on it:
--   1. IsListeningComprehension        = 1
--   2. ListeningAudioUrl               = the audio clip the candidate listens to
--   3. ListeningAudioTranscription     = transcript of that clip (used for scoring; never sent to the browser)
--   4. QuestionGist                    = plain-language setup the Synthesia avatar speaks
--      plus ExpectedAnswer             = the model summary the candidate's answer is scored against
--
-- The avatar reads QuestionGist, NOT Comment - the question text describes what the
-- candidate is supposed to listen for, so narrating it would give the exercise away.
--
-- During the interview the candidate may play the audio at most 2 times, and gets
-- 3 minutes to type a summary. The timer starts only once the first playback finishes.
-- The overall interview timer is unaffected and keeps running as usual.

-- Step 1: View all questions for a specific IRF
-- Replace [YourIRFId] with the actual IRF ID
SELECT
    id,
    IRFId,
    Skill,
    SkillName,
    Comment AS QuestionText,
    IsListeningComprehension,
    ListeningAudioUrl,
    QuestionGist,
    ExpectedAnswer,
    AIQuestionVideoLink
FROM SkillChannelSampleQuestions
WHERE IRFId = [YourIRFId]
ORDER BY id;

-- Step 2: Check which questions are already flagged for this IRF
SELECT
    id,
    Comment AS QuestionText,
    IsListeningComprehension,
    ListeningAudioUrl
FROM SkillChannelSampleQuestions
WHERE IRFId = [YourIRFId]
  AND IsListeningComprehension = 1;

-- Step 3: Flag a specific question and give it its audio, transcript, gist and expected answer
-- Replace the bracketed placeholders with actual values
UPDATE SkillChannelSampleQuestions
SET IsListeningComprehension  = 1,
    ListeningAudioUrl         = '[https://your-blob-url/listening-clip.mp3]',
    ListeningAudioTranscription = N'[Full transcript of the audio clip]',
    QuestionGist              = N'[Plain-language setup for the avatar to speak, e.g. "You will now hear a short audio clip. You may listen to it twice. Then summarise what you heard in your own words. You will have three minutes to type your summary."]',
    ExpectedAnswer            = N'[The model summary the candidate answer is scored against]'
WHERE id = [QuestionId]
  AND IRFId = [YourIRFId];

-- Verify the change
SELECT
    id,
    IRFId,
    Comment AS QuestionText,
    IsListeningComprehension,
    ListeningAudioUrl,
    QuestionGist,
    ExpectedAnswer
FROM SkillChannelSampleQuestions
WHERE id = [QuestionId];

-- Step 4: Generate the Synthesia avatar video for the question
-- Use the "Generate AI question video" action in the Delivery Head sample questions UI.
-- It will refuse to generate until both ListeningAudioUrl and QuestionGist are populated.


-- ============================================
-- Example: Flag question ID 1234 for IRF 567
-- ============================================

/*
UPDATE SkillChannelSampleQuestions
SET IsListeningComprehension  = 1,
    ListeningAudioUrl         = 'https://risebirdstorage.blob.core.windows.net/audio/listening-clip-01.mp3',
    ListeningAudioTranscription = N'Good morning. The quarterly review meeting has been moved from Tuesday to Thursday at 3 pm...',
    QuestionGist              = N'You will now hear a short audio clip. You may listen to it a maximum of two times. After the first playback finishes you will have three minutes to type a summary of what you heard.',
    ExpectedAnswer            = N'The quarterly review meeting was rescheduled from Tuesday to Thursday at 3 pm...'
WHERE id = 1234
  AND IRFId = 567;
*/


-- ============================================
-- View Listening Comprehension Results for a Candidate
-- ============================================

-- Replace [CandidateEmail] with actual email
SELECT
    lca.id,
    lca.SessionId,
    im.candidateEmail,
    im.candidateName,
    lca.QuestionId,
    lca.CandidateSummary,
    lca.ExpectedAnswer,
    lca.AudioPlayCount,
    lca.IsTimeExpired,
    lca.TimeTakenInSeconds,
    lca.Score,
    lca.MaxScore,
    lca.IsPassed,
    lca.TimeStarted,
    lca.TimeCompleted,
    lca.CreatedAt
FROM ListeningComprehensionAssessment lca
INNER JOIN InterviewSessions isess ON isess.SessionId = CAST(lca.SessionId AS UNIQUEIDENTIFIER)
INNER JOIN InterviewMeetings im ON im.SessionId = isess.SessionId
WHERE im.candidateEmail = '[CandidateEmail]'
ORDER BY lca.CreatedAt DESC;


-- ============================================
-- View All Listening Comprehension Results for an IRF
-- ============================================

-- Replace [YourIRFId] with actual IRF ID
SELECT
    lca.id,
    im.candidateEmail,
    im.candidateName,
    lca.Score,
    lca.MaxScore,
    lca.IsPassed,
    lca.AudioPlayCount,
    lca.IsTimeExpired,
    lca.TimeTakenInSeconds,
    lca.TimeCompleted
FROM ListeningComprehensionAssessment lca
INNER JOIN InterviewSessions isess ON isess.SessionId = CAST(lca.SessionId AS UNIQUEIDENTIFIER)
INNER JOIN InterviewMeetings im ON im.SessionId = isess.SessionId
WHERE im.IRFId = [YourIRFId]
ORDER BY lca.Score DESC;


-- ============================================
-- Answers awaiting scoring (prompt not yet configured)
-- ============================================

-- Rows land here when OpenAI:ListeningComprehensionPromptId is not set. The candidate's
-- answer is still captured, so it can be scored retrospectively once the prompt is wired.
SELECT
    lca.id,
    lca.SessionId,
    lca.QuestionId,
    lca.CandidateSummary,
    lca.ExpectedAnswer,
    lca.CreatedAt
FROM ListeningComprehensionAssessment lca
WHERE lca.Score IS NULL
ORDER BY lca.CreatedAt DESC;


-- ============================================
-- Remove the Listening Comprehension Flag from a Question
-- ============================================

UPDATE SkillChannelSampleQuestions
SET IsListeningComprehension = 0
WHERE id = [QuestionId];


-- ============================================
-- Best Practices
-- ============================================

/*
1. Keep the audio clip short - the candidate has only three minutes to summarise it.
2. Make sure ListeningAudioTranscription matches the clip exactly; it is the scoring baseline.
3. Write ExpectedAnswer as the summary a strong candidate would produce, not as a transcript.
4. QuestionGist should set up the exercise WITHOUT revealing the audio content.
5. Host the audio on a URL the candidate's browser can reach for the whole interview window.
6. Use a widely supported format (mp3 / m4a) so playback works across browsers.
7. Review AudioPlayCount and IsTimeExpired alongside the score - a candidate who ran out of
   time or only listened once gives useful context for the result.
*/
