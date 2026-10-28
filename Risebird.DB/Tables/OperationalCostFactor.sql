-- Running costs that are not recorded per interview (cloud hosting, email, licences, ...), entered by
-- finance so the Interview Profitability grid can charge each interview its share.
--
-- Basis decides how an amount is spread:
--   PerDay            Amount per calendar day, shared equally by that day's held interviews.
--   PerMonth          Amount per calendar month, spread evenly over the month's days, then as PerDay.
--   PerInterview      Amount added to every held interview.
--   PerAIInterview    Amount added to every held AI interview (IRF.IsAIInterview = 1).
--   PerHumanInterview Amount added to every held interview that is not an AI interview.
-- "Held" means not cancelled (confirmationStatus <> 2) and not deleted.
--
-- A factor applies on the days between EffectiveFrom and EffectiveTo (inclusive, open-ended when
-- EffectiveTo is NULL). To change a running cost, end-date the old row and add a new one, so past
-- periods keep the cost they actually had.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'OperationalCostFactor')
BEGIN
    CREATE TABLE [dbo].[OperationalCostFactor](
        [Id]            [int] IDENTITY(1,1) NOT NULL,
        [Name]          [varchar](100)  NOT NULL,
        [Category]      [varchar](50)   NOT NULL,
        [Amount]        [decimal](18, 2) NOT NULL,
        [Currency]      [varchar](3)    NOT NULL CONSTRAINT [DF_OperationalCostFactor_Currency] DEFAULT ('INR'),
        [Basis]         [varchar](20)   NOT NULL,
        [EffectiveFrom] [date]          NOT NULL,
        [EffectiveTo]   [date]          NULL,
        [IsActive]      [bit]           NOT NULL CONSTRAINT [DF_OperationalCostFactor_IsActive] DEFAULT (1),
        [Notes]         [varchar](500)  NULL,
        [CreatedBy]     [varchar](200)  NULL,
        [CreatedOn]     [datetime]      NOT NULL CONSTRAINT [DF_OperationalCostFactor_CreatedOn] DEFAULT (GETDATE()),
        [ModifiedBy]    [varchar](200)  NULL,
        [ModifiedOn]    [datetime]      NULL,
        CONSTRAINT [PK_OperationalCostFactor] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [CK_OperationalCostFactor_Basis]
            CHECK ([Basis] IN ('PerDay', 'PerMonth', 'PerInterview', 'PerAIInterview', 'PerHumanInterview')),
        CONSTRAINT [CK_OperationalCostFactor_Currency] CHECK ([Currency] IN ('INR', 'USD')),
        CONSTRAINT [CK_OperationalCostFactor_Amount] CHECK ([Amount] >= 0),
        CONSTRAINT [CK_OperationalCostFactor_Dates] CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom])
    ) ON [PRIMARY]
END
GO
