IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'InterviewerTrainingChecklist')
BEGIN
    CREATE TABLE [dbo].[InterviewerTrainingChecklist]
    (
        [Id]                   INT            IDENTITY(1,1) NOT NULL,
        [InterviewerEmail]     VARCHAR(200)   NOT NULL,
        [TrainingModuleCode]   VARCHAR(50)    NOT NULL,
        [ChecklistStateJson]   NVARCHAR(MAX)  NULL,
        [TotalItems]           INT            NULL,
        [TotalChecked]         INT            NULL,
        [CreatedBy]            VARCHAR(200)   NULL,
        [CreatedOn]            DATETIME       NULL,
        CONSTRAINT [PK_InterviewerTrainingChecklist] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_InterviewerTrainingChecklist_Email_Module] UNIQUE ([InterviewerEmail], [TrainingModuleCode])
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO
