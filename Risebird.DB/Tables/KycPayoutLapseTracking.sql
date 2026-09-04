IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'KycPayoutLapseTracking')
BEGIN
CREATE TABLE [dbo].[KycPayoutLapseTracking](
    [Id]                INT           IDENTITY(1,1) NOT NULL,
    [InterviewerUserid] INT           NOT NULL,
    [Month]             VARCHAR(7)    NOT NULL,
    [Remark]            VARCHAR(500)  NULL,
    [ReminderCount]     INT           NOT NULL CONSTRAINT DF_KycPayoutLapseTracking_ReminderCount DEFAULT(0),
    [PendingAmount]     DECIMAL(18,2) NULL,
    [TrackingStatus]    VARCHAR(20)   NOT NULL CONSTRAINT DF_KycPayoutLapseTracking_Status DEFAULT('Active'),
    [CreatedOn]         DATETIME      NOT NULL CONSTRAINT DF_KycPayoutLapseTracking_CreatedOn DEFAULT(GETDATE()),
    [CreatedBy]         VARCHAR(200)  NULL,
    [LastReminderOn]    DATETIME      NULL,
    [KYCCompletedOn]    DATETIME      NULL,
    [LapsedOn]          DATETIME      NULL,
    CONSTRAINT [PK_KycPayoutLapseTracking] PRIMARY KEY CLUSTERED ([Id] ASC)
) ON [PRIMARY]

CREATE UNIQUE INDEX UX_KycPayoutLapseTracking_User_Month ON [dbo].[KycPayoutLapseTracking]([InterviewerUserid],[Month])
END
GO
