-- An expert (skill channel SME) assigned to audit one completed AI code interview report.
-- WasDiscrepancyFound and MaxSeverity are stamped on submit so QC listings can triage at a glance.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'AICodeInterviewExpertAssignment')
BEGIN
CREATE TABLE [dbo].[AICodeInterviewExpertAssignment](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [varchar](50) NOT NULL,
	[InterviewId] [int] NULL,
	[IRFIntId] [int] NULL,
	[ExpertEmail] [varchar](150) NOT NULL,
	[AssignedBy] [varchar](150) NULL,
	[AssignedAt] [datetime] NULL,
	[SubmittedAt] [datetime] NULL,
	[IsActive] [bit] NOT NULL CONSTRAINT [DF_AICodeIntExpertAssignment_IsActive] DEFAULT (1),
	[WasDiscrepancyFound] [bit] NULL,
	[MaxSeverity] [tinyint] NULL,
 CONSTRAINT [PK_AICodeInterviewExpertAssignment] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

-- At most one live assignment per session, which is what keeps an item out of the open pool
-- once somebody has picked it up.
IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UQ_AICodeIntExpertAssignment_ActiveSession'
    AND object_id = OBJECT_ID('AICodeInterviewExpertAssignment')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AICodeIntExpertAssignment_ActiveSession
    ON AICodeInterviewExpertAssignment (SessionId)
    WHERE IsActive = 1;
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'IX_AICodeIntExpertAssignment_Expert'
    AND object_id = OBJECT_ID('AICodeInterviewExpertAssignment')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_AICodeIntExpertAssignment_Expert
    ON AICodeInterviewExpertAssignment (ExpertEmail, IsActive);
END
GO
