IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = N'PaymentLogs'
)
BEGIN
    CREATE TABLE [dbo].[PaymentLogs] (
        [Id]                 INT            IDENTITY(1,1) NOT NULL,
        [Endpoint]           NVARCHAR(200)  NULL,
        [RequestBody]        NVARCHAR(MAX)  NULL,
        [ResponseBody]       NVARCHAR(MAX)  NULL,
        [HttpStatusCode]     INT            NULL,
        [LogLevel]           NVARCHAR(50)   NULL,
        [EventType]          NVARCHAR(100)  NULL,
        [TransferId]         NVARCHAR(50)   NULL,
        [CfTransferId]       NVARCHAR(100)  NULL,
        [TransferStatus]     NVARCHAR(50)   NULL,
        [SignatureTimestamp] NVARCHAR(50)   NULL,
        [SignatureReceived]  NVARCHAR(500)  NULL,
        [RowsUpdated]        INT            NULL,
        [ErrorMessage]       NVARCHAR(MAX)  NULL,
        [CallerIp]           NVARCHAR(50)   NULL,
        [CreatedAt]          DATETIME       NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_PaymentLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
            WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY];
END
GO
