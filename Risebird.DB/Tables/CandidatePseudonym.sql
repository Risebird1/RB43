IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CandidatePseudonym')
BEGIN
    CREATE TABLE [dbo].[CandidatePseudonym](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [CandidateMasterId] [int] NULL,
        [CandidateEmail] [varchar](200) NOT NULL,
        [NickName] [varchar](20) NOT NULL,
        [CreatedOn] [datetime] NOT NULL CONSTRAINT [DF_CandidatePseudonym_CreatedOn] DEFAULT (GETDATE()),
        [CreatedBy] [varchar](200) NULL,
        CONSTRAINT [PK_CandidatePseudonym] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [UQ_CandidatePseudonym_Email] UNIQUE ([CandidateEmail]),
        CONSTRAINT [UQ_CandidatePseudonym_NickName] UNIQUE ([NickName])
    ) ON [PRIMARY];

    CREATE NONCLUSTERED INDEX [IX_CandidatePseudonym_CandidateMasterId]
        ON [dbo].[CandidatePseudonym]([CandidateMasterId])
        WHERE [CandidateMasterId] IS NOT NULL;
END
GO
