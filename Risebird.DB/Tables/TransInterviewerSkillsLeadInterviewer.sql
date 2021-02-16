/****** Object:  Table [dbo].[TransInterviewerSkillsLeadInterviewer]    Script Date: 5/15/2020 9:48:27 AM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransInterviewerSkillsLeadInterviewer')
BEGIN 

CREATE TABLE [dbo].[TransInterviewerSkillsLeadInterviewer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerSkillId] [int] NOT NULL,
	[InterviewerId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_TransInterviewerSkillsInterviewer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

END
GO
IF Not EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'PerSuccessRate')
      AND Object_ID = Object_ID(N'TransInterviewerSkillsLeadInterviewer'))
BEGIN
    ALTER TABLE TransInterviewerSkillsLeadInterviewer
    Add  PerSuccessRate int
END
GO




