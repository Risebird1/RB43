
IF Not EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = N'TransCriptsConv')
BEGIN
CREATE TABLE TransCriptsConv (
    Id INT  IDENTITY(1,1) not null,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    Role NVARCHAR(20),
    Message NVARCHAR(MAX),
    Timestamp DATETIME NOT NULL,
    DeliveryHeadQuestionId int null,
 CONSTRAINT [PK_TransCriptsConv] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY];
End

IF NOT EXISTS(
    SELECT *
    FROM sys.columns
    WHERE Name IN (N'DeliveryHeadQuestionId')
      AND Object_ID = Object_ID(N'TransCriptsConv'))
BEGIN
    ALTER TABLE TransCriptsConv
    Add DeliveryHeadQuestionId int null
END
GO

-- =====================================================
-- Add indexes for duplicate prevention and performance
-- =====================================================

-- Index for interview questions (Session + Role + Question = unique)
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_TransCriptsConv_SessionRoleQuestion' 
    AND object_id = OBJECT_ID('TransCriptsConv')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TransCriptsConv_SessionRoleQuestion
    ON TransCriptsConv (SessionId, Role, DeliveryHeadQuestionId)
    WHERE DeliveryHeadQuestionId IS NOT NULL;
    PRINT '✓ Index added to TransCriptsConv for interview question duplicate checking';
END
GO

-- Index for general conversation time-windowed deduplication
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_TransCriptsConv_GeneralConversation' 
    AND object_id = OBJECT_ID('TransCriptsConv')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TransCriptsConv_GeneralConversation
    ON TransCriptsConv (SessionId, Role, Timestamp)
    INCLUDE (Message)
    WHERE DeliveryHeadQuestionId IS NULL;
    PRINT '✓ Index added to TransCriptsConv for general conversation duplicate checking';
END
GO