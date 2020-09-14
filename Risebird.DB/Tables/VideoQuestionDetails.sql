IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'VideoQuestionDetails')
BEGIN 

CREATE TABLE [dbo].[VideoQuestionDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Interviewid] [int] NULL,
	[VideoLink] [varchar](max) NULL,
	[VideoTitle] [varchar](200) NULL,
	[Priority] [int] NULL,
	[Question] [varchar](max) NULL,
	[ThumbnailLink] [varchar](max) NULL,
 CONSTRAINT [PK_VideoQuestionDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
End
GO


