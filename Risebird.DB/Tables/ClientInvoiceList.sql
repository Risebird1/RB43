IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'ClientInvoiceList')
BEGIN
CREATE TABLE [dbo].[ClientInvoiceList](
	[id]            [int] IDENTITY(1,1) NOT NULL,
	[ClientId]      [int] NULL,
	[InvoiceModel]  [varchar](100) NULL,
	[BillingOwner]  [int] NULL,
	[InvoiceNumber] [varchar](100) NULL,
	[IRN]           [varchar](200) NULL,
	[GrossAmount]   [decimal](18,2) NULL,
	[Status]        [int] NULL DEFAULT 0,
	[Owner]         [varchar](200) NULL,
	[LatestRemark]  [varchar](500) NULL,
	[UTRTrxn]       [varchar](200) NULL,
	[BillingType]   [int] NULL,
	[InvoiceMonth]  [int] NULL,
	[InvoiceYear]   [int] NULL,
	[CreatedOn]     [datetime] NULL DEFAULT GETDATE(),
	[ModifiedOn]    [datetime] NULL,
 CONSTRAINT [PK_ClientInvoiceList] PRIMARY KEY CLUSTERED
(
	[id] ASC
) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
