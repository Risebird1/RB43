IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerAssessmentVerifications')
BEGIN
CREATE TABLE [dbo].[InterviewerAssessmentVerifications](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[AssessmentId] [int] NULL,
	[UserId] [int] NULL,
	[IsVerified] [bit] NULL,
	[MCQCandidateid] [int] NULL,
 CONSTRAINT [PK_InterviewerAssessmentVerifications] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO


