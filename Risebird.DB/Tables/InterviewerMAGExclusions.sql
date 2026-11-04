-- Snapshot of the assessment groups (MAGs) stripped from an interviewer, so they can be restored.
-- MAG membership is read from InterviewerProfile.AssessmentGroupId (CSV) by ~25 procedures, many of which
-- don't check UserList.IsActive, so clearing the source removes the interviewer from all of them at once.
-- Reason: CallOptOut (interviewer opted out of calls) | ManualInactive (a Risebird user inactivated them).
-- The two are restored independently. RestoredOn IS NULL = still excluded.
-- No foreign key to UserList on purpose: usp_DeleteUser / usp_DeleteInterviewerProfile delete UserList rows and
-- would fail half-way for any interviewer that was ever excluded. Orphan rows are harmless.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'InterviewerMAGExclusions')
BEGIN
CREATE TABLE [dbo].[InterviewerMAGExclusions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[RemovedAssessmentGroupIds] [varchar](max) NULL,  -- InterviewerProfile.AssessmentGroupId at removal time (max: groups merged in from the other reason can pass 500)
	[Reason] [varchar](50) NOT NULL,
	[PerformedBy] [varchar](200) NULL,
	[CreatedOn] [datetime] NOT NULL,
	[RestoredOn] [datetime] NULL,
	[RestoredBy] [varchar](200) NULL,
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_InterviewerMAGExclusions_UserId' AND object_id = OBJECT_ID(N'dbo.InterviewerMAGExclusions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_InterviewerMAGExclusions_UserId ON dbo.InterviewerMAGExclusions (UserId, Reason) INCLUDE (RestoredOn)
END
GO

-- Databases that ran an earlier version of this script: widen RemovedAssessmentGroupIds from varchar(500).
IF COL_LENGTH(N'dbo.InterviewerMAGExclusions', N'RemovedAssessmentGroupIds') = 500
    ALTER TABLE dbo.InterviewerMAGExclusions ALTER COLUMN RemovedAssessmentGroupIds varchar(max) NULL;
GO
