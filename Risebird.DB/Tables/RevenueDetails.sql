IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RevenueDetails')
BEGIN  
CREATE TABLE [dbo].[RevenueDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Clientid] [int] NULL,
	[Months] [int] NULL,
	[Years] [int] NULL,
	[POPendingAmount] [int] NULL,
	[InvoiceAmount] [int] NULL,
	[CurrentStatus] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[CreadtedBy] [int] NULL,
	[ModifiedOn] [datetime] NULL,
	[MOdifiedBy] [int] NULL,
	[IRFId] [varchar](10) NULL,
	[BillingType] [int] NULL,
	[BillingOwner] [int] NULL,
	[invoicerealization] [int] NULL,
 CONSTRAINT [PK_RevenueDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
ELSE
BEGIN
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'invoicerealization')
      AND Object_ID = Object_ID(N'RevenueDetails'))
BEGIN
    ALTER TABLE RevenueDetails
    Add invoicerealization INT
END
END
GO


