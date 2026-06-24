IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SkillChannelBillingRate')
BEGIN
CREATE TABLE [dbo].[SkillChannelBillingRate](
    [id]            [int]          IDENTITY(1,1) NOT NULL,
    [IRFId]         [int]          NOT NULL,   -- References IRF.id (PK)
    [BillingRate]   [int]          NOT NULL,
    -- EffectiveFrom: the date from which this rate is active for the IRF.
    -- Multiple rows per IRF are allowed (one per rate change).
    -- NULL means "legacy/initial entry — effective from the beginning".
    [EffectiveFrom] [date]         NULL,
    [CreatedBy]     [varchar](200) NULL,
    [CreatedOn]     [datetime]     NULL,
    [ModifiedOn]    [datetime]     NULL,
 CONSTRAINT [PK_SkillChannelBillingRate] PRIMARY KEY CLUSTERED ([id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

    CREATE NONCLUSTERED INDEX [IX_SkillChannelBillingRate_IRFId_EffectiveFrom]
        ON [dbo].[SkillChannelBillingRate] ([IRFId] ASC, [EffectiveFrom] ASC)
END
ELSE
BEGIN
    -- Add EffectiveFrom column to existing table if it does not exist
    IF NOT EXISTS (
        SELECT * FROM sys.columns
        WHERE Name = N'EffectiveFrom' AND Object_ID = OBJECT_ID(N'SkillChannelBillingRate'))
    BEGIN
        ALTER TABLE [dbo].[SkillChannelBillingRate]
            ADD [EffectiveFrom] [date] NULL

        CREATE NONCLUSTERED INDEX [IX_SkillChannelBillingRate_IRFId_EffectiveFrom]
            ON [dbo].[SkillChannelBillingRate] ([IRFId] ASC, [EffectiveFrom] ASC)
    END
END
GO
