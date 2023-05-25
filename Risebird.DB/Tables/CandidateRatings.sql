IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateRatings')
BEGIN
CREATE TABLE [dbo].[CandidateRatings](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[interviewid] [int] NULL,
	[Question1] [int] NULL,
	[Question2] [int] NULL,
	[Question3] [int] NULL,
	[Question4] [varchar](1000) NULL,
	[Question5] [varchar](1000) NULL,
	[Question6] [int] NULL,
	[Interviewerid] [int] NULL,
	[CreatedOn] [datetime] NULL,
	[Type] [varchar](20) NULL,
	[Comment] [varchar](1000) NULL,
	[Name] [varchar](100) NULL,
	[Phone] [varchar](25) NULL,
 CONSTRAINT [PK_CandidateRating] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


