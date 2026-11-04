/****** Object:  Table [dbo].[WhatsAppOptOut]   ******/
-- WhatsApp-only opt-out, separate from email. Candidates are scoped per IRF (IRFId NULL = all IRFs).
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = N'WhatsAppOptOut')
BEGIN

CREATE TABLE [dbo].[WhatsAppOptOut](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Audience] [varchar](20) NOT NULL,        -- 'Candidate' | 'Interviewer' | 'Applicant'
	[EmailId] [nvarchar](256) NULL,           -- lower-cased, trimmed
	[ProspectId] [int] NULL,                  -- applicants
	[IRFId] [int] NULL,                       -- IRF.id for candidates; NULL = all
	[Source] [varchar](30) NOT NULL,          -- 'UnsubscribeButton' | 'StopKeyword' | 'Manual'
	[CreatedAt] [datetime] NOT NULL CONSTRAINT [DF_WhatsAppOptOut_CreatedAt] DEFAULT GETUTCDATE(),
 CONSTRAINT [PK_WhatsAppOptOut] PRIMARY KEY CLUSTERED
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End

GO

-- Which message the unsubscribe came from (the Interakt event in the link's &src=, e.g. "Schedule Interview"),
-- and the optional reason picked on the confirmation page.
IF COL_LENGTH('dbo.WhatsAppOptOut', 'TemplateEvent') IS NULL
    ALTER TABLE dbo.WhatsAppOptOut ADD [TemplateEvent] [varchar](100) NULL;
GO
IF COL_LENGTH('dbo.WhatsAppOptOut', 'Reason') IS NULL
    ALTER TABLE dbo.WhatsAppOptOut ADD [Reason] [nvarchar](300) NULL;
GO

-- History: a re-enable (admin page /WhatsAppOptOut) stamps the row instead of deleting it. Only rows with
-- ReEnabledAt NULL are active opt-outs; a later opt-out by the same person is a new row.
IF COL_LENGTH('dbo.WhatsAppOptOut', 'ReEnabledAt') IS NULL
    ALTER TABLE dbo.WhatsAppOptOut ADD [ReEnabledAt] [datetime] NULL;
GO
IF COL_LENGTH('dbo.WhatsAppOptOut', 'ReEnabledBy') IS NULL
    ALTER TABLE dbo.WhatsAppOptOut ADD [ReEnabledBy] [varchar](150) NULL;
GO

-- One active opt-out per person and scope; re-enabled rows stay as history.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WhatsAppOptOut' AND object_id = OBJECT_ID(N'WhatsAppOptOut'))
    DROP INDEX UX_WhatsAppOptOut ON dbo.WhatsAppOptOut;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WhatsAppOptOut_Active' AND object_id = OBJECT_ID(N'WhatsAppOptOut'))
    CREATE UNIQUE INDEX UX_WhatsAppOptOut_Active ON dbo.WhatsAppOptOut (Audience, EmailId, ProspectId, IRFId) WHERE ReEnabledAt IS NULL;
GO
