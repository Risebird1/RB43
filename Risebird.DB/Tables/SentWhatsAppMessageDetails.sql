/****** Object:  Table [dbo].[SentMailDetails]   ******/
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'SentWhatsAppMessageDetails')
BEGIN

CREATE TABLE [dbo].[SentWhatsAppMessageDetails](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[Recipient] [varchar](60) NULL,
	[CountryCode] [varchar](5) NULL,
	[WhatsappNumber] [varchar](15) NULL,
	[Message] [varchar](max) NULL,
	[ButtonLink] [varchar](500) NULL,
	[Event] [varchar](500) NULL,
	[IRFId] [int] NULL,
	[InterviewId] [int] NULL,
	[InterviewDate] datetime Null,
	[Header] [varchar](500) NULL,
	[CreatedAt] datetime Null,
	[SentBy] [varchar](60) NULL,
 CONSTRAINT [PK_SentWhatsAppMessageDetails] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

End

GO

-- One row per send attempt: 'Sent' | 'Failed' | 'Skipped'. Only 'Sent' counts toward the 3-per-IRF WhatsApp limit.
-- Rows written before this column existed stay NULL and are counted as sent (no backfill: the table is large).
IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'Status')
      AND Object_ID = Object_ID(N'SentWhatsAppMessageDetails'))
BEGIN
    ALTER TABLE SentWhatsAppMessageDetails
    ADD [Status] [varchar](10) NULL
END

GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SentWhatsAppMessageDetails_Recipient_IRFId_Event' AND object_id = OBJECT_ID(N'SentWhatsAppMessageDetails'))
    CREATE INDEX IX_SentWhatsAppMessageDetails_Recipient_IRFId_Event
        ON SentWhatsAppMessageDetails (Recipient, IRFId, [Event]) INCLUDE ([Status]);

GO

