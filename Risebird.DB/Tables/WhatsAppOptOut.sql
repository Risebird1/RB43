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

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WhatsAppOptOut' AND object_id = OBJECT_ID(N'WhatsAppOptOut'))
    CREATE UNIQUE INDEX UX_WhatsAppOptOut ON WhatsAppOptOut (Audience, EmailId, ProspectId, IRFId);

GO
