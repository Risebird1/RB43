IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'ClientReportShareDetails')
Begin
CREATE TABLE [dbo].[ClientReportShareDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[ClientEmailid] [varchar](150) NULL,
	[uniqueid] [varchar](150) NULL,
	[SharedOn] [datetime] NULL,
	[OpenCount] [int] NULL,
 CONSTRAINT [PK_ClientReportShareDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


