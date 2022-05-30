IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'MCQShareList')
BEGIN 
CREATE TABLE [dbo].[MCQShareList](
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
 CONSTRAINT [PK_MCQShareList] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
