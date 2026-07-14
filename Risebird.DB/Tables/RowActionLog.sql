IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'RowActionLog')
BEGIN
    CREATE TABLE [dbo].[RowActionLog](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [RefType] [varchar](20) NOT NULL,
        [RefId] [varchar](20) NOT NULL,
        [ActionsTaken] [varchar](500) NULL,
        [UpdatedBy] [varchar](100) NULL,
        [UpdatedAt] [datetime] NOT NULL CONSTRAINT [DF_RowActionLog_UpdatedAt] DEFAULT (GETDATE()),
        CONSTRAINT [PK_RowActionLog] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [UQ_RowActionLog_Ref] UNIQUE ([RefType], [RefId])
    ) ON [PRIMARY]
END
GO
