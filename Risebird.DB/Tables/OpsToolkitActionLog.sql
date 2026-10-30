/****** Object:  Table [dbo].[OpsToolkitActionLog] ******/
-- Audit trail for every corrective action taken from the Ops Toolkit page (/EmailLookup/OpsToolkit).
-- One row per action. ActionType names the tool, e.g. 'RemoveUnassignPenalty'.
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'OpsToolkitActionLog')
BEGIN

CREATE TABLE [dbo].[OpsToolkitActionLog](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ActionType] [varchar](50) NOT NULL,
	[InterviewId] [int] NULL,
	[InterviewPointsId] [int] NULL,
	[UserId] [int] NULL,
	[OldPoints] [int] NULL,
	[NewPoints] [int] NULL,
	[Reason] [nvarchar](500) NULL,
	[PerformedBy] [varchar](200) NOT NULL,
	[PerformedAt] [datetime] NOT NULL CONSTRAINT [DF_OpsToolkitActionLog_PerformedAt] DEFAULT (DATEADD(MINUTE, 330, GETDATE())),
 CONSTRAINT [PK_OpsToolkitActionLog] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_OpsToolkitActionLog_InterviewPointsId]
    ON [dbo].[OpsToolkitActionLog] ([InterviewPointsId], [ActionType])

END
GO
