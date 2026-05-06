IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'InterviewerProxyCandidateChecklist')
BEGIN
    CREATE TABLE [dbo].[InterviewerProxyCandidateChecklist]
    (
        [Id]                  INT            IDENTITY(1,1)  NOT NULL,
        [InterviewerEmail]    VARCHAR(200)                  NULL,
        [ChecklistStateJson]  NVARCHAR(MAX)                 NULL,
        [TotalItems]          INT                           NULL,
        [TotalChecked]        INT                           NULL,
        [CreatedBy]           VARCHAR(200)                  NULL,
        [CreatedOn]           DATETIME                      NULL,
        CONSTRAINT [PK_InterviewerProxyCandidateChecklist] PRIMARY KEY CLUSTERED
        (
            [Id] ASC
        ) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
