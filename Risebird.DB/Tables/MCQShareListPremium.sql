IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MCQShareListPremium')
BEGIN 
CREATE TABLE [dbo].[MCQShareListPremium](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[PartitionKey] [varchar](50) NULL,
	[ApplyingCompany] [varchar](50) NULL,
	[ClientHR] [varchar](200) NULL,
	[EmailAddress] [varchar](200) NULL,
	[MCQId] [varchar](50) NULL,
	[MCQSet] [varchar](50) NULL,
	[Videorecording] [int] NULL,
	[status] [int] NULL,
	[ShareDate] [datetime] NULL,
	[roomId] [varchar](50) NULL,
	[Expiretime] [int] NULL,
	[WhoShared] [varchar](200) NULL,
 CONSTRAINT [PK_MCQShareListPremium] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
