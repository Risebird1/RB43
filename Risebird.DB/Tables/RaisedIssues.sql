IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RaisedIssues')
BEGIN
CREATE TABLE [dbo].[RaisedIssues](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[RequestType] [varchar](10) NULL,
	[Comment] [varchar](1000) NULL,
	[AttachmentURL] [varchar](1000) NULL,
	[RequestedBy] [varchar](200) NULL,
	[RequestedOn] [datetime] NULL,
	[Status] [varchar](10) NULL,
 CONSTRAINT [PK_RaisedIssues] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


