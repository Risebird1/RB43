-- Call opt-out / opt-in log, for interviewers and candidates. One row per event.
-- OptIn rows (acknowledgement email "Yes") are the UCC consent proof: they keep IP address and user agent.
-- Unmatched opt-outs (no person found) are still written with whatever ids were sent, so ops can reconcile them.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CallPreferences')
BEGIN
CREATE TABLE [dbo].[CallPreferences](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PersonType] [varchar](20) NOT NULL,          -- Interviewer | Candidate
	[UserId] [int] NULL,                          -- interviewer UserList.id
	[CandidateId] [int] NULL,                     -- Candidate.id
	[IRFId] [int] NULL,                           -- IRF.id, candidates only
	[Phone] [varchar](20) NULL,                   -- dbo.fn_NormalizePhone: last 10 digits
	[EmailId] [varchar](200) NULL,
	[Action] [varchar](10) NOT NULL,              -- OptOut | OptIn | Undo
	[Source] [varchar](50) NULL,                  -- Plivo | ElevenLabs | AcknowledgementEmail | Portal | Unknown | ?source=
	[ProviderCallId] [varchar](100) NULL,         -- Plivo CallUUID / ElevenLabs conversation_id
	[Reason] [nvarchar](500) NULL,           -- the opt-out quote (may be Hindi etc.)
	[RawPayload] [nvarchar](max) NULL,            -- provider JSON, capped at 100,000 chars by the app
	[IPAddress] [varchar](64) NULL,               -- opt-in only
	[UserAgent] [varchar](500) NULL,              -- opt-in only
	[CreatedOn] [datetime] NOT NULL,              -- IST
	[CreatedBy] [varchar](200) NULL,
PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CallPreferences_Phone' AND object_id = OBJECT_ID(N'dbo.CallPreferences'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CallPreferences_Phone ON dbo.CallPreferences (Phone) INCLUDE (PersonType, Action, CreatedOn)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CallPreferences_UserId' AND object_id = OBJECT_ID(N'dbo.CallPreferences'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CallPreferences_UserId ON dbo.CallPreferences (UserId)
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_CallPreferences_CandidateId' AND object_id = OBJECT_ID(N'dbo.CallPreferences'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CallPreferences_CandidateId ON dbo.CallPreferences (CandidateId)
END
GO

-- Databases that ran an earlier version of this script: Reason was varchar(500), which turned non-Latin quotes into ???.
IF EXISTS (SELECT 1 FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
           WHERE c.object_id = OBJECT_ID(N'dbo.CallPreferences') AND c.name = N'Reason' AND t.name = N'varchar')
    ALTER TABLE dbo.CallPreferences ALTER COLUMN Reason nvarchar(500) NULL;
GO
