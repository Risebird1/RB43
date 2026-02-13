IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_LogTranscript'))
BEGIN
    DROP PROCEDURE dbo.usp_LogTranscript
END
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE usp_LogTranscript
    @SessionId UNIQUEIDENTIFIER,
    @Role NVARCHAR(20),
    @Message NVARCHAR(MAX),
    @Timestamp DATETIME,
    @DeliveryHeadQuestionId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- For interview questions (DeliveryHeadQuestionId provided):
    -- Each question should have ONE assistant message and ONE user response
    -- Duplicate key: SessionId + Role + DeliveryHeadQuestionId
    
    -- For general conversation (DeliveryHeadQuestionId is NULL):
    -- Prevent rapid duplicates within 5 seconds
    -- Duplicate key: SessionId + Role + Message + within 5 seconds
    
    DECLARE @ExistingId INT;
    
    IF @DeliveryHeadQuestionId IS NOT NULL
    BEGIN
        -- Interview question context - check for existing entry for this Session + Role + Question
        SELECT @ExistingId = Id
        FROM TransCriptsConv
        WHERE SessionId = @SessionId
          AND Role = @Role
          AND DeliveryHeadQuestionId = @DeliveryHeadQuestionId;
        
        IF @ExistingId IS NOT NULL
        BEGIN
            -- Update existing record (handles resubmissions/edits)
            UPDATE TransCriptsConv
            SET 
                Message = @Message,
                Timestamp = @Timestamp
            WHERE Id = @ExistingId;
            
            SELECT @ExistingId AS NewId;
        END
        ELSE
        BEGIN
            -- Insert new record
            INSERT INTO TransCriptsConv (
                SessionId,
                Role,
                Message,
                Timestamp,
                DeliveryHeadQuestionId
            )
            VALUES (
                @SessionId,
                @Role,
                @Message,
                @Timestamp,
                @DeliveryHeadQuestionId
            );
            
            SELECT SCOPE_IDENTITY() AS NewId;
        END
    END
    ELSE
    BEGIN
        -- General conversation (no question context) - time-windowed deduplication
        SELECT TOP 1 @ExistingId = Id
        FROM TransCriptsConv
        WHERE SessionId = @SessionId
          AND Role = @Role
          AND Message = @Message
          AND DeliveryHeadQuestionId IS NULL
          AND DATEDIFF(SECOND, Timestamp, @Timestamp) BETWEEN -5 AND 5
        ORDER BY Timestamp DESC;
        
        IF @ExistingId IS NULL
        BEGIN
            -- No duplicate found, insert new record
            INSERT INTO TransCriptsConv (
                SessionId,
                Role,
                Message,
                Timestamp,
                DeliveryHeadQuestionId
            )
            VALUES (
                @SessionId,
                @Role,
                @Message,
                @Timestamp,
                @DeliveryHeadQuestionId
            );
            
            SELECT SCOPE_IDENTITY() AS NewId;
        END
        ELSE
        BEGIN
            -- Duplicate found within 5 seconds, return existing ID
            SELECT @ExistingId AS NewId;
        END
    END
END
GO
