/****** Object:  Table [dbo].[InterviewerSkillsChannel]    Script Date: 5/15/2020 9:47:04 AM ******/

IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'InterviewerSkillsChannel')
BEGIN 
CREATE TABLE [dbo].[InterviewerSkillsChannel](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[InterviewerSkillId] [varchar](200) NULL,
	[JDTiltle] [nvarchar](500) NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NOT NULL,
	[PrimarySkill] [int] NULL,
	[RisebirdHr] [int] NULL,
	[PerMintuteRate] [real] NULL,
	[CalendlyLink] [varchar](500) NULL,
	[IsPaused] [bit] NULL,
	[IsDeleted] [bit] NOT NULL DEFAULT 0,
	[HeuristicProfileType] [int] NULL,
	[JDLink] [varchar](2000) NULL,
 CONSTRAINT [PK_InterviewerSkills] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] 


END

Else

	Begin
		IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'HeuristicProfileType')
		  AND Object_ID = Object_ID(N'InterviewerSkillsChannel'))
		BEGIN
			ALTER TABLE InterviewerSkillsChannel
			Add [HeuristicProfileType] INT null
		END

		IF NOT EXISTS(
		SELECT *
		FROM sys.columns
		WHERE Name IN (N'JDLink')
		  AND Object_ID = Object_ID(N'InterviewerSkillsChannel'))
		BEGIN
			ALTER TABLE InterviewerSkillsChannel
			Add [JDLink] varchar(2000) null
		END
 
	End


GO




