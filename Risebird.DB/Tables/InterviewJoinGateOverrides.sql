-- Interview alignment: the Join button on the interview details page stays disabled until the
-- interviewer has confirmed the four briefing points (rubric, questions, candidate pre-check,
-- previous issues). A hard block would cancel real interviews over a page that failed to register a
-- click, so the interviewer can always "join without confirming" - and that override is recorded
-- here with what was still outstanding. It is keyed interviewer x interview, the same shape as the
-- other per-interview signals the trial machinery grades.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
               WHERE TABLE_NAME = N'InterviewJoinGateOverrides')
BEGIN
    CREATE TABLE [dbo].[InterviewJoinGateOverrides] (
        [Id]                      int IDENTITY(1,1) NOT NULL,
        [InterviewId]             int NOT NULL,
        [InterviewerEmail]        varchar(200) NOT NULL,
        [OutstandingRequirements] varchar(500) NULL, -- comma separated: rubric,questions,precheck,issues
        [CreatedOn]               datetime NOT NULL CONSTRAINT [DF_IJGO_CreatedOn] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_InterviewJoinGateOverrides] PRIMARY KEY CLUSTERED ([Id] ASC)
    )

    CREATE NONCLUSTERED INDEX [IX_IJGO_InterviewId] ON [dbo].[InterviewJoinGateOverrides]([InterviewId])
END
GO
