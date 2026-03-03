IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'AIInterviewExceptionLog')
BEGIN
CREATE TABLE [dbo].[AIInterviewExceptionLog](
    [Id]                    [int]           IDENTITY(1,1) NOT NULL,
    [SessionId]             [varchar](50)   NULL,           -- Interview session GUID; NULL when exception fires before session is resolved
    [HttpMethod]            [varchar](10)   NOT NULL,       -- 'GET' or 'POST'
    [ExceptionMessage]      [nvarchar](max) NULL,
    [InnerExceptionMessage] [nvarchar](max) NULL,
    [StackTrace]            [nvarchar](max) NULL,
    [LogLevel]              [int]           NOT NULL DEFAULT 4, -- matches LogLevelInfo enum (4 = Exception)
    [OccurredAt]            [datetime]      NOT NULL DEFAULT GETDATE(),
    [IsResolved]            [bit]           NOT NULL DEFAULT 0,
 CONSTRAINT [PK_AIInterviewExceptionLog] PRIMARY KEY CLUSTERED
(
    [Id] ASC
) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
END
GO
