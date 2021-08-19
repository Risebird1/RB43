/****** Object:  Table [dbo].[InstaRequests]    Script Date: 18-08-2021 22:48:38 ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InstaRequests')
BEGIN 

CREATE TABLE [dbo].[InstaRequests](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InstaRequestName] [varchar](15) NULL,
	[AssessmentGroup] [int] NULL,
	[JDLink] [varchar](1000) NULL,
	[RequestedBy] [varchar](150) NULL,
	[CreatedOn] [datetime] NULL,
 CONSTRAINT [PK_InstaRequests] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


