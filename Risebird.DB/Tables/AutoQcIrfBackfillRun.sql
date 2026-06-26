IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'AutoQcIrfBackfillRun')
BEGIN
    CREATE TABLE [dbo].[AutoQcIrfBackfillRun] (
        [Id]                      INT            IDENTITY(1,1) NOT NULL,
        [RunId]                   UNIQUEIDENTIFIER NOT NULL,
        [IrfId]                   INT            NOT NULL,
        [DateFromUtc]             DATETIME       NOT NULL,
        [DateToUtc]               DATETIME       NOT NULL,
        [VerdictsScanned]         INT            NOT NULL DEFAULT 0,
        [NewFlagsSet]             INT            NOT NULL DEFAULT 0,
        [AlreadyFlaggedSkipped]   INT            NOT NULL DEFAULT 0,
        [ExecutedBy]              VARCHAR(150)   NOT NULL,
        [ExecutedAtUtc]           DATETIME       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_AutoQcIrfBackfillRun] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_AutoQcIrfBackfillRun_IrfId]
        ON [dbo].[AutoQcIrfBackfillRun] ([IrfId] ASC, [ExecutedAtUtc] DESC);
END
GO
