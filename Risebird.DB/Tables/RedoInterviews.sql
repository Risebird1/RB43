IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'RedoInterviews')
BEGIN
CREATE TABLE [dbo].[RedoInterviews](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[interviewid] [int] NULL,
	[CandidateFace] [varchar](30) NULL,
	[Inappropriate] [varchar](30) NULL,
	[RequiredDuration] [int] NULL,
	[ActualDuration] [int] NULL,
	[HandsOfCoding] [varchar](30) NULL,
	[Comments] [varchar](500) NULL,
	[RejectionComment] [varchar](500) NULL,
	[Createdon] [datetime] NULL,
	[CreatedBy] [varchar](200) NULL,
	[ModifiedOn] [datetime] NULL,
	[ModifiedBy] [varchar](200) NULL,
 CONSTRAINT [PK_RedoInterviews] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


