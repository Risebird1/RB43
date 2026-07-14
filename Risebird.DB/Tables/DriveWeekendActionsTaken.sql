IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'DriveWeekendActionsTaken')
BEGIN
    CREATE TABLE [dbo].[DriveWeekendActionsTaken](
        [id] [int] IDENTITY(1,1) NOT NULL,
        [IrfIntId] [int] NOT NULL,
        [WeekendStart] [date] NOT NULL,
        [ActionsTaken] [varchar](500) NULL,
        [UpdatedBy] [varchar](200) NULL,
        [UpdatedOn] [datetime] NOT NULL CONSTRAINT [DF_DriveWeekendActionsTaken_UpdatedOn] DEFAULT (GETDATE()),
        CONSTRAINT [PK_DriveWeekendActionsTaken] PRIMARY KEY CLUSTERED ([id] ASC),
        CONSTRAINT [UQ_DriveWeekendActionsTaken_IrfWeekend] UNIQUE ([IrfIntId], [WeekendStart])
    ) ON [PRIMARY]
END
GO
