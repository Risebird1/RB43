IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'ClientPO')
BEGIN
    CREATE TABLE [dbo].[ClientPO](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [ClientId] [int] NOT NULL,
        [BillingType] [int] NOT NULL,           -- 1: Billing Owner, 3: Client
        [BillingOwnerId] [int] NULL,            -- Required when BillingType = 1
        [BillingModel] [int] NOT NULL,          -- 2 = M2, 3 = M3
        [PONumber] [varchar](200) NULL,
        [POCreditAmount] [decimal](18, 2) NOT NULL CONSTRAINT [DF_ClientPO_POCreditAmount] DEFAULT (0),
        [CashCreditReceived] [decimal](18, 2) NOT NULL CONSTRAINT [DF_ClientPO_CashCreditReceived] DEFAULT (0),
        [POMonth] [int] NOT NULL,
        [POYear] [int] NOT NULL,
        [PODocumentUrl] [varchar](500) NULL,
        [Status] [int] NOT NULL CONSTRAINT [DF_ClientPO_Status] DEFAULT (0),  -- 0=AwaitingCash, 1=PartialCash, 2=CashCredited, 3=DocumentOnly
        [Remarks] [varchar](500) NULL,
        [CreatedBy] [varchar](200) NULL,
        [CreatedOn] [datetime] NULL,
        [ModifiedOn] [datetime] NULL,
        CONSTRAINT [PK_ClientPO] PRIMARY KEY CLUSTERED ([id] ASC)
    ) ON [PRIMARY]

    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Client]') AND type IN (N'U'))
    BEGIN
        ALTER TABLE [dbo].[ClientPO] WITH CHECK ADD CONSTRAINT [FK_ClientPO_Client] FOREIGN KEY([ClientId])
        REFERENCES [dbo].[Client] ([id])
        ALTER TABLE [dbo].[ClientPO] CHECK CONSTRAINT [FK_ClientPO_Client]
    END

    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserList]') AND type IN (N'U'))
    BEGIN
        ALTER TABLE [dbo].[ClientPO] WITH CHECK ADD CONSTRAINT [FK_ClientPO_BillingOwner] FOREIGN KEY([BillingOwnerId])
        REFERENCES [dbo].[UserList] ([id])
        ALTER TABLE [dbo].[ClientPO] CHECK CONSTRAINT [FK_ClientPO_BillingOwner]
    END
END
GO

-- Link wallet cash credits back to the PO they belong to
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = N'Wallet' AND COLUMN_NAME = N'ClientPOId'
)
BEGIN
    ALTER TABLE [dbo].[Wallet] ADD [ClientPOId] [int] NULL

    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ClientPO]') AND type IN (N'U'))
    BEGIN
        ALTER TABLE [dbo].[Wallet] WITH CHECK ADD CONSTRAINT [FK_Wallet_ClientPO] FOREIGN KEY([ClientPOId])
        REFERENCES [dbo].[ClientPO] ([id])
        ALTER TABLE [dbo].[Wallet] CHECK CONSTRAINT [FK_Wallet_ClientPO]
    END
END
GO
