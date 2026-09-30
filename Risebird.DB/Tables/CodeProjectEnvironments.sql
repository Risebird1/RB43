IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'CodeProjectEnvironments')
BEGIN
    CREATE TABLE [dbo].[CodeProjectEnvironments](
        [id]                [int] IDENTITY(1,1) NOT NULL,
        [SessionId]         [varchar](50)   NOT NULL,
        [QuestionId]        [int]           NOT NULL,
        [StackImageCode]    [varchar](50)   NULL,
        [State]             [varchar](20)   NOT NULL,   -- requested | provisioning | ready | handedover | submitted | failed | released
        [EnvironmentHandle] [varchar](200)  NULL,       -- provisioner's id for the machine
        [ConnectUrl]        [varchar](1000) NULL,
        [VMPropertyId]      [int]           NULL,
        [Message]           [nvarchar](500) NULL,
        [RequestedAtUtc]    [datetime2]     NOT NULL CONSTRAINT DF_CodeProjectEnvironments_RequestedAtUtc DEFAULT SYSUTCDATETIME(),
        [ReadyAtUtc]        [datetime2]     NULL,
        [HandedOverAtUtc]   [datetime2]     NULL,
        [SubmittedAtUtc]    [datetime2]     NULL,
     CONSTRAINT [PK_CodeProjectEnvironments] PRIMARY KEY CLUSTERED ([id] ASC),
     CONSTRAINT [UQ_CodeProjectEnvironments_SessionQuestion] UNIQUE ([SessionId], [QuestionId])
    ) ON [PRIMARY];
END
GO
