IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'SupportTickets')
BEGIN
    CREATE TABLE [dbo].[SupportTickets] (
        [Id]                        INT            IDENTITY(1,1) NOT NULL,
        [SenderEmail]               VARCHAR(150)   NOT NULL,
        [Subject]                   NVARCHAR(500)  NULL,
        [MailContent]               NVARCHAR(MAX)  NULL,
        [ReceivedAt]                DATETIME       NULL,
        [Intent]                    NVARCHAR(500)  NULL,
        [PrimarySpecialization]     NVARCHAR(500)  NULL,
        [MailBodyIntent]            NVARCHAR(500)  NULL,
        [AttachmentJobDescription]  BIT            NOT NULL CONSTRAINT [DF_ST_AttachJD]      DEFAULT (0),
        [AttachmentPdf]             BIT            NOT NULL CONSTRAINT [DF_ST_AttachPdf]     DEFAULT (0),
        [AttachmentResumes]         BIT            NOT NULL CONSTRAINT [DF_ST_AttachResumes] DEFAULT (0),
        [DriveFolderLink]           NVARCHAR(1000) NULL,
        [SummaryMail]               NVARCHAR(MAX)  NULL,
        [SummaryCandidates]         NVARCHAR(MAX)  NULL,
        [SummaryInterviewScores]    NVARCHAR(MAX)  NULL,
        [UserId]                    INT            NULL,
        [ClientId]                  INT            NULL,
        [AssignedTo]                VARCHAR(150)   NULL,
        [Status]                    VARCHAR(50)    NOT NULL CONSTRAINT [DF_SupportTickets_Status]    DEFAULT ('Open'),
        [CreatedAt]                 DATETIME       NOT NULL CONSTRAINT [DF_SupportTickets_CreatedAt] DEFAULT (GETUTCDATE()),
        [UpdatedAt]                 DATETIME       NULL,
        CONSTRAINT [PK_SupportTickets] PRIMARY KEY CLUSTERED ([Id] ASC)
            WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF,
                  ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    )
END
ELSE
BEGIN
    -- Idempotent column additions for existing deployments
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'PrimarySpecialization'    AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [PrimarySpecialization]  NVARCHAR(500)  NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'MailBodyIntent'           AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [MailBodyIntent]         NVARCHAR(500)  NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AttachmentJobDescription' AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [AttachmentJobDescription] BIT NOT NULL  DEFAULT (0);
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AttachmentPdf'            AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [AttachmentPdf]          BIT NOT NULL    DEFAULT (0);
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AttachmentResumes'        AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [AttachmentResumes]      BIT NOT NULL    DEFAULT (0);
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'DriveFolderLink'          AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [DriveFolderLink]        NVARCHAR(1000) NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'SummaryMail'              AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [SummaryMail]            NVARCHAR(MAX)  NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'SummaryCandidates'        AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [SummaryCandidates]      NVARCHAR(MAX)  NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'SummaryInterviewScores'   AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [SummaryInterviewScores] NVARCHAR(MAX)  NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'UpdatedAt'                AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [UpdatedAt]              DATETIME       NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'AssignedTo'               AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [AssignedTo]             VARCHAR(150)   NULL;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE Name = N'JobTitle'                 AND Object_ID = Object_ID(N'SupportTickets'))
        ALTER TABLE dbo.SupportTickets ADD [JobTitle]               NVARCHAR(200)  NULL;
END
GO
