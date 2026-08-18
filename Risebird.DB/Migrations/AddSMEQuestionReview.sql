-- SME Question Review (F1 rev B)
-- Additive: existing SkillChannelSampleQuestions rows keep working.
-- VettingStatus NULL = legacy (still served). 0 Draft · 1 Confirmed · 2 Rejected.
-- Each ALTER group is followed by GO so later statements compile against the new columns.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ---------------------------------------------------------------------------
-- 1. SkillChannelSampleQuestions — review + expected-answer parts
-- ---------------------------------------------------------------------------
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'MaxScore') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD MaxScore int NOT NULL CONSTRAINT DF_SCSQ_MaxScore DEFAULT (0);
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'ExpectedOutput') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD ExpectedOutput nvarchar(max) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'AnswerExplanation') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD AnswerExplanation nvarchar(max) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'SafeImplementation') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD SafeImplementation nvarchar(max) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'SemanticMatchCriteria') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD SemanticMatchCriteria nvarchar(max) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'MisconceptionRedFlags') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD MisconceptionRedFlags nvarchar(max) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'AIVideoStatus') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD AIVideoStatus tinyint NOT NULL CONSTRAINT DF_SCSQ_AIVideoStatus DEFAULT (0);
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'AIVideoGeneratedOn') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD AIVideoGeneratedOn datetime NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'AIDraftPayload') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD AIDraftPayload nvarchar(max) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'VettingStatus') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD VettingStatus tinyint NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'VettedBy') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD VettedBy varchar(150) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'VettedOn') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD VettedOn datetime NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'RejectionReason') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD RejectionReason nvarchar(1000) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'QuestionSlot') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD QuestionSlot varchar(80) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'QuestionVariant') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD QuestionVariant varchar(40) NULL;
GO
IF COL_LENGTH('dbo.SkillChannelSampleQuestions', 'RubricItemId') IS NULL
    ALTER TABLE dbo.SkillChannelSampleQuestions ADD RubricItemId int NULL;
GO

-- Backfill video status for existing rows that already have a link.
UPDATE dbo.SkillChannelSampleQuestions
SET AIVideoStatus = 2
WHERE ISNULL(AIVideoStatus, 0) = 0
  AND NULLIF(LTRIM(RTRIM(ISNULL(AIQuestionVideoLink, ''))), '') IS NOT NULL;
GO

-- ---------------------------------------------------------------------------
-- 2. IRF — client expert sign-off gate (advisory by default)
-- ---------------------------------------------------------------------------
IF COL_LENGTH('dbo.IRF', 'RequireClientExpertSignOff') IS NULL
    ALTER TABLE dbo.IRF ADD RequireClientExpertSignOff bit NOT NULL CONSTRAINT DF_IRF_RequireClientExpertSignOff DEFAULT (0);
GO

-- ---------------------------------------------------------------------------
-- 3. InterviewMeetings — chosen set, resolved server-side
-- ---------------------------------------------------------------------------
IF COL_LENGTH('dbo.InterviewMeetings', 'QuestionSetId') IS NULL
    ALTER TABLE dbo.InterviewMeetings ADD QuestionSetId int NULL;
GO

-- ---------------------------------------------------------------------------
-- 4. New tables
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.AICodeInterviewQuestionReviewLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AICodeInterviewQuestionReviewLog (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        QuestionBankId int NOT NULL,
        FieldName varchar(80) NOT NULL,
        AIValue nvarchar(max) NULL,
        SMEValue nvarchar(max) NULL,
        Severity tinyint NOT NULL CONSTRAINT DF_AIQRL_Severity DEFAULT (1),
        ChangedBy varchar(150) NULL,
        ChangedOn datetime NOT NULL CONSTRAINT DF_AIQRL_ChangedOn DEFAULT (GETUTCDATE())
    );
    CREATE INDEX IX_AIQRL_Question ON dbo.AICodeInterviewQuestionReviewLog (QuestionBankId);
END
GO

IF OBJECT_ID(N'dbo.AICodeInterviewQuestionSet', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AICodeInterviewQuestionSet (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IRFId int NOT NULL,
        Name nvarchar(200) NOT NULL,
        IsAIProposed bit NOT NULL CONSTRAINT DF_AIQS_IsAIProposed DEFAULT (0),
        ReviewStatus tinyint NOT NULL CONSTRAINT DF_AIQS_ReviewStatus DEFAULT (0),
        IsDeleted bit NOT NULL CONSTRAINT DF_AIQS_IsDeleted DEFAULT (0),
        ApprovedBy varchar(150) NULL,
        ApprovedOn datetime NULL,
        AIDraftPayload nvarchar(max) NULL,
        CreatedBy varchar(150) NULL,
        CreatedOn datetime NOT NULL CONSTRAINT DF_AIQS_CreatedOn DEFAULT (GETUTCDATE())
    );
    CREATE INDEX IX_AIQS_IRF ON dbo.AICodeInterviewQuestionSet (IRFId) WHERE IsDeleted = 0;
END
GO

IF OBJECT_ID(N'dbo.AICodeInterviewQuestionSetItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AICodeInterviewQuestionSetItem (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        QuestionSetId int NOT NULL,
        QuestionBankId int NOT NULL,
        DisplayOrder int NOT NULL CONSTRAINT DF_AIQSI_Order DEFAULT (0)
    );
    CREATE INDEX IX_AIQSI_Set ON dbo.AICodeInterviewQuestionSetItem (QuestionSetId, DisplayOrder);
END
GO

IF OBJECT_ID(N'dbo.ExpertInLoopClientConfirmation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpertInLoopClientConfirmation (
        Id int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        IRFId int NOT NULL,
        SEId int NULL,
        InterviewerProfileId int NULL,
        Scope tinyint NOT NULL CONSTRAINT DF_EIL_Scope DEFAULT (0),
        Status tinyint NOT NULL CONSTRAINT DF_EIL_Status DEFAULT (0),
        ClientUserId varchar(150) NULL,
        ConfirmedOn datetime NULL,
        ClientComment nvarchar(1000) NULL
    );
    CREATE UNIQUE INDEX UX_EIL_ScopeIrf ON dbo.ExpertInLoopClientConfirmation (IRFId, Scope) WHERE SEId IS NULL;
    CREATE UNIQUE INDEX UX_EIL_ScopeSe ON dbo.ExpertInLoopClientConfirmation (IRFId, Scope, SEId) WHERE SEId IS NOT NULL;
END
GO

-- ---------------------------------------------------------------------------
-- 5. Serving path — only confirmed (or legacy NULL) questions. Set wins when approved.
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.usp_GetRandomQuestionForSession', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_GetRandomQuestionForSession;
GO

CREATE PROCEDURE dbo.usp_GetRandomQuestionForSession
    @interviewId int,
    @irfId int
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SessionId UNIQUEIDENTIFIER =
        (SELECT SessionId FROM InterviewMeetings WHERE id = @interviewId);

    DECLARE @AskedCount INT = (
        SELECT COUNT(*)
        FROM TranscriptsConv
        WHERE SessionId = @SessionId AND [Role] = 'assistant'
    );

    IF (@AskedCount >= 35)
    BEGIN
        SELECT NULL AS [Message], NULL AS AIQuestionVideoLink, NULL AS Id;
        RETURN;
    END;

    DECLARE @SetId int = NULL;
    SELECT @SetId = m.QuestionSetId
    FROM InterviewMeetings m
    WHERE m.id = @interviewId;

    -- If no set is mapped yet, freeze the least-used approved set for this IRF (round-robin).
    -- Draft / rejected / deleted sets are ignored. No approved set → randomization below.
    IF (@SetId IS NULL OR NOT EXISTS (
            SELECT 1 FROM dbo.AICodeInterviewQuestionSet s
            WHERE s.Id = @SetId AND s.IsDeleted = 0 AND s.ReviewStatus = 1
       ))
    BEGIN
        SELECT TOP 1 @SetId = s.Id
        FROM dbo.AICodeInterviewQuestionSet s
        WHERE s.IRFId = @irfId AND s.IsDeleted = 0 AND s.ReviewStatus = 1
        ORDER BY (
            SELECT COUNT(*) FROM dbo.InterviewMeetings m WHERE m.QuestionSetId = s.Id
        ), s.Id;

        IF @SetId IS NOT NULL
            UPDATE dbo.InterviewMeetings
            SET QuestionSetId = @SetId
            WHERE id = @interviewId
              AND (QuestionSetId IS NULL OR QuestionSetId <> @SetId);
    END

    IF @SetId IS NOT NULL
       AND EXISTS (
            SELECT 1 FROM dbo.AICodeInterviewQuestionSet s
            WHERE s.Id = @SetId AND s.IsDeleted = 0 AND s.ReviewStatus = 1
       )
    BEGIN
        ;WITH SetQs AS (
            SELECT q.id, q.Comment, q.AIQuestionVideoLink, q.IsTypingSpeedAssessment,
                   q.IsListeningComprehension, q.ListeningAudioUrl, q.ListeningAudioTranscription,
                   q.IsCodingQuestion, q.CodingTimeLimitInMinutes,
                   ROW_NUMBER() OVER (ORDER BY i.DisplayOrder, q.Id) AS RowNum
            FROM dbo.AICodeInterviewQuestionSetItem i
            INNER JOIN dbo.SkillChannelSampleQuestions q ON q.id = i.QuestionBankId
            WHERE i.QuestionSetId = @SetId
              AND (q.VettingStatus IS NULL OR q.VettingStatus = 1)
        )
        SELECT Comment AS [Message], AIQuestionVideoLink, id,
               ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment,
               ISNULL(IsListeningComprehension, 0) AS IsListeningComprehension,
               ListeningAudioUrl,
               ListeningAudioTranscription,
               ISNULL(IsCodingQuestion, 0) AS IsCodingQuestion,
               CodingTimeLimitInMinutes
        FROM SetQs
        WHERE RowNum = @AskedCount + 1;
        RETURN;
    END

    ;WITH OrderedQuestions AS (
        SELECT q.id, q.Comment, q.AIQuestionVideoLink, q.IsTypingSpeedAssessment,
               q.IsListeningComprehension, q.ListeningAudioUrl, q.ListeningAudioTranscription,
               q.IsCodingQuestion, q.CodingTimeLimitInMinutes,
               ROW_NUMBER() OVER (ORDER BY q.Id) AS RowNum
        FROM dbo.SkillChannelSampleQuestions q
        WHERE q.IRFId = @irfId
          AND (q.VettingStatus IS NULL OR q.VettingStatus = 1)
          AND (
                q.QuestionSlot IS NULL
                OR LTRIM(RTRIM(q.QuestionSlot)) = ''
                OR NOT EXISTS (
                    SELECT 1
                    FROM TranscriptsConv t
                    INNER JOIN dbo.SkillChannelSampleQuestions aq ON aq.id = t.DeliveryHeadQuestionId
                    WHERE t.SessionId = @SessionId
                      AND t.[Role] = 'assistant'
                      AND aq.QuestionSlot = q.QuestionSlot
                      AND NULLIF(LTRIM(RTRIM(aq.QuestionSlot)), '') IS NOT NULL
                )
              )
    )
    SELECT Comment AS [Message], AIQuestionVideoLink, id,
           ISNULL(IsTypingSpeedAssessment, 0) AS IsTypingSpeedAssessment,
           ISNULL(IsListeningComprehension, 0) AS IsListeningComprehension,
           ListeningAudioUrl,
           ListeningAudioTranscription,
           ISNULL(IsCodingQuestion, 0) AS IsCodingQuestion,
           CodingTimeLimitInMinutes
    FROM OrderedQuestions
    WHERE RowNum = @AskedCount + 1;
END
GO
