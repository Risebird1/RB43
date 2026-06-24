-- Tracks PerSuccessRate and GSTApplicable changes per client over time.
-- Each row records a rate + GST flag that are effective from EffectiveFrom onwards.
--
-- To find the rate / GST active on a given interview date:
--   SELECT TOP 1 PerSuccessRate, GSTApplicable
--   FROM ClientBillingRateHistory
--   WHERE ClientId = @ClientId AND EffectiveFrom <= @interviewDate
--   ORDER BY EffectiveFrom DESC
--
-- Applicable for InterviewerBillingType = 1 (Client) only.
-- For InterviewerBillingType = 2 (SkillChannel), effective dates are tracked
-- directly on SkillChannelBillingRate via the EffectiveFrom column.

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'ClientBillingRateHistory')
BEGIN
    CREATE TABLE [dbo].[ClientBillingRateHistory] (
        [Id]             INT           IDENTITY(1,1) NOT NULL,
        [ClientId]       INT           NOT NULL,
        [PerSuccessRate] INT           NOT NULL,
        -- GSTApplicable: whether GST (18%) is charged from this effective date.
        -- NULL = inherit from Client.GSTApplicable (backward compat for rows saved before this column existed).
        [GSTApplicable]  BIT           NULL,
        [EffectiveFrom]  DATE          NOT NULL,
        [CreatedBy]      VARCHAR(200)  NULL,
        [CreatedOn]      DATETIME      NULL,
        CONSTRAINT [PK_ClientBillingRateHistory] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ClientBillingRateHistory_Client]
            FOREIGN KEY ([ClientId]) REFERENCES [dbo].[Client]([id])
    )

    CREATE NONCLUSTERED INDEX [IX_ClientBillingRateHistory_ClientId_EffectiveFrom]
        ON [dbo].[ClientBillingRateHistory] ([ClientId] ASC, [EffectiveFrom] ASC)
END
ELSE
BEGIN
    IF NOT EXISTS (
        SELECT * FROM sys.columns
        WHERE Name = N'GSTApplicable' AND Object_ID = OBJECT_ID(N'ClientBillingRateHistory'))
    BEGIN
        ALTER TABLE [dbo].[ClientBillingRateHistory]
            ADD [GSTApplicable] BIT NULL
    END
END
GO
