-- Priority MAGs for the parallel "priority intent collection" automation service.
-- Operators flag up to 2 MAGs per day as priority. The PriorityIntentCallJob picks up
-- prospects mapped to these MAGs that are stuck at the InterestCollection stage and calls
-- them aggressively (bypassing the usual anti-spam caps) to capture intent quickly.
--
-- "Resets daily to none" is achieved by stamping each row with the IST PriorityDate and
-- only treating rows whose PriorityDate = today's IST date (and IsActive = 1) as active.
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'NaukriPriorityMag')
BEGIN
    CREATE TABLE [dbo].[NaukriPriorityMag](
        [Id]                [int] IDENTITY(1,1) NOT NULL,
        [AssessmentGroupId] [int] NOT NULL,
        [MagName]           [nvarchar](200) NOT NULL,
        [PriorityDate]      [date] NOT NULL,
        [SetBy]             [nvarchar](150) NULL,
        [SetAt]             [datetime] NOT NULL DEFAULT GETUTCDATE(),
        [ClearedAt]         [datetime] NULL,
        [IsActive]          [bit] NOT NULL DEFAULT 1,
        CONSTRAINT [PK_NaukriPriorityMag] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_NaukriPriorityMag_DateActive' AND object_id = OBJECT_ID(N'NaukriPriorityMag'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_NaukriPriorityMag_DateActive]
        ON [dbo].[NaukriPriorityMag] ([PriorityDate], [IsActive])
        INCLUDE ([AssessmentGroupId], [MagName]);
END
GO
