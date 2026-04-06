-- usp_InsertPendingResume
-- Inserts a pending resume record for HR review.
-- Called by the webhook instead of directly inserting into CandidateResume.
-- Stores original resolved IRF for accuracy tracking.
CREATE OR ALTER PROCEDURE [dbo].[usp_InsertPendingResume]
    @SupportTicketId            INT,
    @ResumeUrl                  NVARCHAR(1000),
    @ResumeFileName             NVARCHAR(500)   = NULL,
    @CandidateName              NVARCHAR(200)   = NULL,
    @CandidateEmail             VARCHAR(150)    = NULL,
    @ResolvedIRFId              INT             = NULL,
    @ResolvedIRFStringId        VARCHAR(20)     = NULL,
    @ResolvedJobTitle           NVARCHAR(200)   = NULL,
    @ResolvedHREmail            VARCHAR(150)    = NULL,
    @HasSchedulingData          BIT             = 0,
    @ScheduledInterviewDateTime DATETIME        = NULL,
    @ScheduleTimezone           VARCHAR(20)     = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.SupportTicketPendingResumes (
        SupportTicketId, ResumeUrl, ResumeFileName, CandidateName, CandidateEmail,
        ResolvedIRFId, ResolvedIRFStringId, ResolvedJobTitle, ResolvedHREmail,
        OriginalResolvedIRFId, OriginalResolvedIRFStringId,
        HasSchedulingData, ScheduledInterviewDateTime, ScheduleTimezone,
        Status, IRFChangedByHR, CreatedAt
    )
    VALUES (
        @SupportTicketId, @ResumeUrl, @ResumeFileName, @CandidateName, @CandidateEmail,
        @ResolvedIRFId, @ResolvedIRFStringId, @ResolvedJobTitle, @ResolvedHREmail,
        @ResolvedIRFId, @ResolvedIRFStringId,  -- Store original values
        @HasSchedulingData, @ScheduledInterviewDateTime, @ScheduleTimezone,
        'Pending', 0, GETUTCDATE()
    );
    
    -- Update ticket to indicate pending resumes
    UPDATE dbo.SupportTickets 
    SET HasPendingResumes = 1 
    WHERE Id = @SupportTicketId;
    
    SELECT SCOPE_IDENTITY() AS NewId;
END
GO
