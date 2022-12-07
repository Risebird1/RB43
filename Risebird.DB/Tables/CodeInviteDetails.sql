IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'CodeInviteDetails')
BEGIN
CREATE TABLE [dbo].[CodeInviteDetails](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserName] [varchar](200) NULL,
	[Experience] [decimal](10, 2) NULL,
	[ProgrammingLanguage] [VARCHAR](MAX) NULL,
	[DifficultyLevel][varchar](20) NULL,
	[InviteStartTime] [varchar](20) NULL,
	[InviteEndTime] [varchar](20) NULL,
	[TimeZone] [varchar](10) NULL,
	[inviteId] [varchar](60) NULL,
	[invite_UrL] [varchar](200) NULL,
	[CreatedAt] [varchar] (20)  NULL,
	[CreatedBy] [varchar] (20)  NULL,
	[ModifiedAt] [varchar] (20) NULL,
	[ModifiedBy] [varchar] (20) NULL,
	[submissionId] [varchar] (60) NULL,
	[assignmentId] [varchar] (60) NULL,
	[submittedBy] [varchar] (200) NULL,
	[submittedTime] [datetime] NULL,
	[score] [int] NULL,
	[totalAssignmentScore] [int] NULL,
	[percentageScore] [int] NULL,
	[status] [varchar] (20) NULL,

 CONSTRAINT [PK_CodeInviteDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON))
END

GO