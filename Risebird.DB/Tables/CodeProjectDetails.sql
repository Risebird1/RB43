IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectDetails')
BEGIN
    CREATE TABLE [dbo].[CodeProjectDetails](
        [QuestionId]        [int]           NOT NULL,
        [Language]          [varchar](50)   NOT NULL,
        [Framework]         [varchar](50)   NULL,
        [StackImageCode]    [varchar](50)   NOT NULL,
        [ProjectBlobUri]    [varchar](500)  NULL,   -- candidate-visible zip
        [SecretsBlobUri]    [varchar](500)  NULL,   -- hidden tests + reference solution, never on the VM
        [EntryPointFile]    [varchar](500)  NULL,
        [ReadOnlyPathsJson] [nvarchar](max) NULL,
        [BuildCommand]      [varchar](500)  NULL,
        [RunCommand]        [varchar](500)  NULL,
        [TestCommand]       [varchar](500)  NULL,
        [HasWebserver]      [bit]           NULL,
        [PortsJson]         [varchar](200)  NULL,
        [RequirementsJson]  [nvarchar](max) NULL,
        [EstimatedMinutes]  [int]           NULL,
        [ValidationStatus]  [varchar](20)   NOT NULL CONSTRAINT DF_CodeProjectDetails_ValidationStatus DEFAULT 'pending',
        [ValidationJson]    [nvarchar](max) NULL,
        [ValidatedAtUtc]    [datetime2]     NULL,
        [ApprovedBySmeId]   [int]           NULL,
        [ApprovedAtUtc]     [datetime2]     NULL,
     CONSTRAINT [PK_CodeProjectDetails] PRIMARY KEY CLUSTERED ([QuestionId] ASC),
     CONSTRAINT [FK_CodeProjectDetails_Question] FOREIGN KEY ([QuestionId])
        REFERENCES [dbo].[SkillChannelSampleQuestions]([id]) ON DELETE CASCADE,
     CONSTRAINT [FK_CodeProjectDetails_StackImage] FOREIGN KEY ([StackImageCode])
        REFERENCES [dbo].[StackImages]([Code])
    ) ON [PRIMARY];
END
GO
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = N'CK_CodeProjectDetails_Servable')
BEGIN
    ALTER TABLE CodeProjectDetails ADD CONSTRAINT CK_CodeProjectDetails_Servable
        CHECK (ValidationStatus IN ('pending','validating','failed')
            OR (ValidationStatus = 'passed' AND ProjectBlobUri IS NOT NULL));
END
GO
