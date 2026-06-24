IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'GeneratedInvoices')
BEGIN
CREATE TABLE [dbo].[GeneratedInvoices](
    [id]               [int] IDENTITY(1,1) NOT NULL,
    [ClientId]         [int] NULL,
    [BillingOwnerId]   [int] NULL DEFAULT 0,
    [InvoiceNumber]    [varchar](50) NULL,
    [InvoiceMonth]     [int] NULL,
    [InvoiceYear]      [int] NULL,
    [BillingOwnerEmail][varchar](200) NULL,
    [CreditPeriod]     [varchar](100) NULL,
    [GSTNumber]        [varchar](100) NULL,
    [TaxScenario]      [int] NULL DEFAULT 0,   -- 0=Non-GST 1=GST18%
    [IRN]              [varchar](200) NULL,
    [EInvoiceFileLink] [varchar](MAX) NULL,
    [Subtotal]         [decimal](18,2) NULL DEFAULT 0,
    [GSTAmount]        [decimal](18,2) NULL DEFAULT 0,
    [InvoiceValue]     [decimal](18,2) NULL DEFAULT 0,
    [TDSAmount]        [decimal](18,2) NULL DEFAULT 0,
    [NetReceivable]    [decimal](18,2) NULL DEFAULT 0,
    [Status]           [int] NULL DEFAULT 0,   -- 0=Draft 1=Generated 2=PO Pending 3=Approval Pending 4=Invoice Realisation Pending 5=Paid
    [LatestRemark]     [varchar](500) NULL,
    [UTRTrxn]          [varchar](200) NULL,
    [CreatedOn]        [datetime] NULL DEFAULT GETDATE(),
    [ModifiedOn]       [datetime] NULL,
    [CreatedBy]        [varchar](200) NULL,
 CONSTRAINT [PK_GeneratedInvoices] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'EInvoiceFileLink' AND Object_ID = Object_ID(N'GeneratedInvoices'))
BEGIN
    ALTER TABLE GeneratedInvoices ADD [EInvoiceFileLink] [varchar](MAX) NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'LatestRemark' AND Object_ID = Object_ID(N'GeneratedInvoices'))
BEGIN
    ALTER TABLE GeneratedInvoices ADD [LatestRemark] [varchar](500) NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'UTRTrxn' AND Object_ID = Object_ID(N'GeneratedInvoices'))
BEGIN
    ALTER TABLE GeneratedInvoices ADD [UTRTrxn] [varchar](200) NULL
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'CashCreditAmount' AND Object_ID = Object_ID(N'GeneratedInvoices'))
BEGIN
    ALTER TABLE GeneratedInvoices ADD [CashCreditAmount] [decimal](18,2) NULL DEFAULT 0
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'InvoiceBaseAmount' AND Object_ID = Object_ID(N'GeneratedInvoices'))
BEGIN
    ALTER TABLE GeneratedInvoices ADD [InvoiceBaseAmount] [decimal](18,2) NULL DEFAULT 0
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'NetReceivableAmount' AND Object_ID = Object_ID(N'GeneratedInvoices'))
BEGIN
    ALTER TABLE GeneratedInvoices ADD [NetReceivableAmount] [decimal](18,2) NULL DEFAULT 0
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'BillingCurrency' AND Object_ID = Object_ID(N'GeneratedInvoices'))
BEGIN
    ALTER TABLE GeneratedInvoices ADD [BillingCurrency] [varchar](10) NULL DEFAULT 'INR'
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'GeneratedInvoiceLineItems')
BEGIN
CREATE TABLE [dbo].[GeneratedInvoiceLineItems](
    [id]          [int] IDENTITY(1,1) NOT NULL,
    [InvoiceId]   [int] NULL,
    [ProductName] [varchar](200) NULL,
    [Description] [varchar](500) NULL,
    [Qty]         [int] NULL DEFAULT 0,
    [UnitRate]    [decimal](18,2) NULL DEFAULT 0,
    [LineTotal]   [decimal](18,2) NULL DEFAULT 0,
    [Basis]       [varchar](100) NULL,
 CONSTRAINT [PK_GeneratedInvoiceLineItems] PRIMARY KEY CLUSTERED ([id] ASC)
) ON [PRIMARY]
END
GO
