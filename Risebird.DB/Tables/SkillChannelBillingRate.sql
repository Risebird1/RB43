IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SkillChannelBillingRate')
BEGIN
CREATE TABLE [dbo].[SkillChannelBillingRate](
    [id]         [int]          IDENTITY(1,1) NOT NULL,
    [IRFId]      [int]          NOT NULL,   -- References IRF.id (PK)
    [BillingRate][int]          NOT NULL,
    [CreatedBy]  [varchar](200) NULL,
    [CreatedOn]  [datetime]     NULL,
    [ModifiedOn] [datetime]     NULL,
 CONSTRAINT [PK_SkillChannelBillingRate] PRIMARY KEY CLUSTERED ([id] ASC)
    WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
