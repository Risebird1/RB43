IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'ExpertInLoopClientConfirmation')
BEGIN
CREATE TABLE [dbo].[ExpertInLoopClientConfirmation](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IRFId] [int] NOT NULL,
	[SEId] [int] NULL,
	[InterviewerProfileId] [int] NULL,
	[Scope] [tinyint] NOT NULL CONSTRAINT [DF_EIL_Scope] DEFAULT (0),
	[Status] [tinyint] NOT NULL CONSTRAINT [DF_EIL_Status] DEFAULT (0),
	[ClientUserId] [varchar](150) NULL,
	[ConfirmedOn] [datetime] NULL,
	[ClientComment] [nvarchar](1000) NULL,
 CONSTRAINT [PK_ExpertInLoopClientConfirmation] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UX_EIL_ScopeIrf'
      AND object_id = OBJECT_ID('ExpertInLoopClientConfirmation')
)
BEGIN
    CREATE UNIQUE INDEX UX_EIL_ScopeIrf ON dbo.ExpertInLoopClientConfirmation (IRFId, Scope) WHERE SEId IS NULL
END
GO

IF NOT EXISTS (
    SELECT * FROM sys.indexes
    WHERE name = 'UX_EIL_ScopeSe'
      AND object_id = OBJECT_ID('ExpertInLoopClientConfirmation')
)
BEGIN
    CREATE UNIQUE INDEX UX_EIL_ScopeSe ON dbo.ExpertInLoopClientConfirmation (IRFId, Scope, SEId) WHERE SEId IS NOT NULL
END
GO
