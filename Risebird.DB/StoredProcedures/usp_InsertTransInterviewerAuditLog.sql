CREATE OR ALTER PROCEDURE [dbo].[usp_InsertTransInterviewerAuditLog]

    @TransInterviewerId    INT,

    @EventType             VARCHAR(50),

    @FromValue             NVARCHAR(500) = NULL,

    @ToValue               NVARCHAR(500) = NULL,

    @ReasonCategory        NVARCHAR(100) = NULL,

    @TriggeringInterviewId INT           = NULL,

    @ChangedBy             VARCHAR(150)  = 'System',

    @AuditTag              VARCHAR(50)   = NULL,

    @AuditSource           VARCHAR(50)   = NULL,

    @BackfillBatchId       UNIQUEIDENTIFIER = NULL,

    @ExtraMetadata         NVARCHAR(MAX) = NULL

AS

BEGIN

    SET NOCOUNT ON;



    INSERT INTO dbo.TransInterviewerAuditLog

        (TransInterviewerId, EventType, FromValue, ToValue, ReasonCategory, TriggeringInterviewId, ChangedBy, ChangedAt,

         AuditTag, AuditSource, BackfillBatchId, ExtraMetadata)

    VALUES

        (@TransInterviewerId, @EventType, @FromValue, @ToValue, @ReasonCategory, @TriggeringInterviewId, @ChangedBy, GETUTCDATE(),

         @AuditTag, @AuditSource, @BackfillBatchId, @ExtraMetadata);

END

GO

