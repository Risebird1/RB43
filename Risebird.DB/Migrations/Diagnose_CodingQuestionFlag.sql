/*
    Diagnose why ChatMessageViewModel.IsCodingQuestion arrives as false in the interview runtime.

    Read-only. Run against the same database the web app is pointed at, then read the four
    result sets top to bottom. The first one that reports a problem is the cause.

    Set the question you were debugging here (from the QuickWatch: CurrentQuestionId / IRFId).
*/
DECLARE @questionId INT = 3062;
DECLARE @irfId      INT = 26249;

/* --------------------------------------------------------------------------
   1. Do the coding columns exist on the table at all?
   Expected: both rows report 'OK'.
   -------------------------------------------------------------------------- */
SELECT
    N'1. Table columns' AS Check_,
    N'IsCodingQuestion' AS Item,
    CASE WHEN COL_LENGTH('dbo.SkillChannelSampleQuestions', 'IsCodingQuestion') IS NULL
         THEN N'MISSING -- run AddCodingQuestionSupport.sql'
         ELSE N'OK' END AS Result
UNION ALL
SELECT
    N'1. Table columns',
    N'CodingTimeLimitInMinutes',
    CASE WHEN COL_LENGTH('dbo.SkillChannelSampleQuestions', 'CodingTimeLimitInMinutes') IS NULL
         THEN N'MISSING -- run AddCodingQuestionSupport.sql'
         ELSE N'OK' END;

/* --------------------------------------------------------------------------
   2. Does the DEPLOYED stored procedure return the coding columns?
   This is the usual culprit: the column exists, the admin screen saves it, but the
   procedure the interview uses has an explicit column list and was never redeployed.
   Expected: 'OK'.
   -------------------------------------------------------------------------- */
SELECT
    N'2. Deployed procedure' AS Check_,
    N'usp_GetRandomQuestionForSession' AS Item,
    CASE
        WHEN OBJECT_ID('dbo.usp_GetRandomQuestionForSession') IS NULL
            THEN N'MISSING -- procedure does not exist'
        WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.usp_GetRandomQuestionForSession')) NOT LIKE '%IsCodingQuestion%'
            THEN N'STALE -- deployed version never selects IsCodingQuestion, so the flag can never reach the app. Run AddCodingQuestionSupport.sql'
        ELSE N'OK'
    END AS Result;

/* --------------------------------------------------------------------------
   3. Is the question you were served actually flagged as a coding question?
   Expected: IsCodingQuestion = 1.
   -------------------------------------------------------------------------- */
SELECT
    N'3. The served question' AS Check_,
    q.id,
    q.IRFId,
    ISNULL(q.IsCodingQuestion, 0) AS IsCodingQuestion,
    q.CodingTimeLimitInMinutes,
    CASE WHEN ISNULL(q.IsCodingQuestion, 0) = 1
         THEN N'OK'
         ELSE N'NOT FLAGGED -- tick "Coding question" on this row in the questions panel' END AS Result,
    LEFT(q.Comment, 120) AS QuestionPreview
FROM dbo.SkillChannelSampleQuestions q
WHERE q.id = @questionId;

/* --------------------------------------------------------------------------
   4. Which questions in this IRF are flagged, and in what order are they asked?
   The interview serves questions ordered by id, so confirm the one you flagged is
   the one you actually reached.
   -------------------------------------------------------------------------- */
SELECT
    N'4. All questions in IRF' AS Check_,
    ROW_NUMBER() OVER (ORDER BY q.Id) AS AskedAtPosition,
    q.id,
    ISNULL(q.IsCodingQuestion, 0) AS IsCodingQuestion,
    q.CodingTimeLimitInMinutes,
    LEFT(q.Comment, 120) AS QuestionPreview
FROM dbo.SkillChannelSampleQuestions q
WHERE q.IRFId = @irfId
ORDER BY q.Id;
