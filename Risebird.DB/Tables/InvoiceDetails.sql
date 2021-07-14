IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InvoiceDetails')
BEGIN  

CREATE TABLE [dbo].[InvoiceDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Clientid] [int] NULL,
	[JobCountry] [varchar](200) NULL,
	[StartDate] [datetime] NULL,
	[Enddate] [datetime] NULL,
	[AccountNumber] [varchar](20) NULL,
	[AccountDetails] [varchar](200) NULL,
	[Comment] [varchar](200) NULL,
	[PaymentStatus] [int] NULL,
	[ReferenceNumber] [varchar](50) NULL,
	[UploadedFiles] [varchar](max) NULL,
	[Price] [int] NULL,
 CONSTRAINT [PK_InvoiceDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


