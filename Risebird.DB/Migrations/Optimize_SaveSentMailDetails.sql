-- Supports usp_SaveSentMailDetails counter upsert (was table scan on IRFId)

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_RisebirdCandidateCommunicationCount_IRFId'
      AND object_id = OBJECT_ID(N'dbo.RisebirdCandidateCommunicationCount')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_RisebirdCandidateCommunicationCount_IRFId
    ON dbo.RisebirdCandidateCommunicationCount (IRFId)
    INCLUDE (MailCount, ModifiedOn, CreatedOn);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_SentMailDetails_sg_message_id'
      AND object_id = OBJECT_ID(N'dbo.SentMailDetails')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_SentMailDetails_sg_message_id
    ON dbo.SentMailDetails (sg_message_id)
    INCLUDE (IRFId, InterviewId, CreatedAt, [To]);
END
GO
