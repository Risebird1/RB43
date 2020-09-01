/****** Object:  UserDefinedTableType [dbo].[VideoSentiment_TableType]    Script Date: 28-08-2020 2.50.48 PM ******/

IF Not EXISTS (SELECT * FROM sys.types WHERE is_table_type = 1 AND name = 'VideoSentiment_TableType')

BEGIN
CREATE TYPE [dbo].[VideoSentiment_TableType] AS TABLE(
	[InterviewId] [int] NOT NULL,
	[ImageUrl] [varchar](max) NULL,
	[EmotionType] [int] NOT NULL,
	[Value] [float] NOT NULL
)
End
GO


