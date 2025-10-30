IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'VideoSentimentAnalysis')
BEGIN 

CREATE TABLE [dbo].[VideoSentimentAnalysis](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[biqvId] [int] not NULL,
	[FEAR] [float] NULL,
	[CONFUSED] [float] NULL,
	[SAD] [float] NULL,
	[CALM] [float] NULL,
	[SURPRISED] [float] NULL,
	[DISGUSTED] [float] NULL,
	[ANGRY] [float] NULL,
	[HAPPY] [float] NULL,
	[CreatedAt] [datetime] not NULL,
	[CreatedBy] [varchar](50) not NULL,
 CONSTRAINT [PK_VideoSentimentAnalysis] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END

GO


