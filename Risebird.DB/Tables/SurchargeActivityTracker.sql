/****** Object:  Table [dbo].[SurchargeActivityTracker]    ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SurchargeActivityTracker')
BEGIN
CREATE TABLE [dbo].[SurchargeActivityTracker](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewId] [int] NULL,
	[Surcharge] [int] NULL,
	[SurchargeComment] [varchar](500) NULL,
	[RequestBy] [varchar](150) NULL,
	[RequestAt] [datetime] NULL,
	[RequestType] [varchar](100) NULL,
	[IRFId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

GO


