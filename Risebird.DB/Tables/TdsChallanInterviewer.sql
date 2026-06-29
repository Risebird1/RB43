IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'TdsChallanInterviewer')
BEGIN
CREATE TABLE [dbo].[TdsChallanInterviewer](
    [id]          INT           IDENTITY(1,1) NOT NULL,
    [ChallanId]   INT           NOT NULL,
    [Userid]      INT           NOT NULL,
    [PAN]         NVARCHAR(10)  NULL,
    [AmountPaid]  DECIMAL(18,2) NOT NULL DEFAULT 0,
    [TdsAmount]   DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT [PK_TdsChallanInterviewer] PRIMARY KEY CLUSTERED ([id] ASC),
    CONSTRAINT [FK_TdsChallanInterviewer_ChallanId]
        FOREIGN KEY ([ChallanId]) REFERENCES [dbo].[TdsChallan]([id]) ON DELETE CASCADE
) ON [PRIMARY]
END
GO
