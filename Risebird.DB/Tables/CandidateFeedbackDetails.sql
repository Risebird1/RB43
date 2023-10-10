/****** Object:  Table [dbo].[CandidateFeedbackDetails]    Script Date: 04-12-2019 6.40.24 PM ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CandidateFeedbackDetails')
Begin
CREATE TABLE [dbo].[CandidateFeedbackDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Rating] [int] NULL,
	[CandidateInteractionQuality] [varchar](250) NULL,
	[LimitedHandoncoding] [varchar](250) NULL,
	[WeakInfrastructure] [varchar](250) NULL,
	[UnethicalPractices] [varchar](250) NULL,
	[TimeManagement] [varchar](250) NULL,
	[PoorCommunicationSkills] [varchar](250) NULL,
	[CreatedOn] [datetime] NULL,
	[InterviewId][int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
ELSE
BEGIN

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'ClientId')
      AND Object_ID = Object_ID(N'CandidateFeedbackDetails'))
BEGIN
    ALTER TABLE CandidateFeedbackDetails
    Add InterviewId INT
END

END
GO






