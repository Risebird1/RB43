IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAICodeInterviewAuditReport'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAICodeInterviewAuditReport
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Everything the expert report page needs for one session: interview context, both report copies,
-- their per question rows, the active assignment and the stored discrepancies.
Create Procedure usp_GetAICodeInterviewAuditReport
@SessionId varchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1: interview context
    select
        im.SessionId,
        im.id as InterviewId,
        irf.id as IRFIntId,
        irf.IRFId,
        irf.JDTiltle as JDTitle,
        im.candidateName as CandidateName,
        im.candidateEmail as CandidateEmail,
        im.interviewDate as InterviewDate
    from InterviewMeetings im
    join IRF irf on irf.IRFId = im.IRFId
    where im.SessionId = @SessionId;

    -- 2: both copies of the report
    select id, SessionId, InterviewId, ReportSource, OverallVerdict, OverallComment,
           OverallCommunicationRating, OverallCommunicationComment, RubricRatingsJson,
           CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
    from AICodeInterviewReport
    where SessionId = @SessionId
    order by ReportSource;

    -- 3: per question rows for both copies
    select qr.id, qr.ReportId, qr.QuestionId, qr.BotInterviewQuestionVideoId, qr.DisplayOrder,
           qr.QuestionText, qr.Rating, qr.Verdict, qr.Comment, qr.CommunicationRating,
           qr.CommunicationComment, qr.RubricRatingsJson, qr.TranscriptText
    from AICodeInterviewReportQuestionRating qr
    join AICodeInterviewReport r on r.id = qr.ReportId
    where r.SessionId = @SessionId
    order by r.ReportSource, qr.DisplayOrder;

    -- 4: active assignment
    select top 1 id, SessionId, InterviewId, IRFIntId, ExpertEmail, AssignedBy, AssignedAt,
           SubmittedAt, IsActive, WasDiscrepancyFound, MaxSeverity
    from AICodeInterviewExpertAssignment
    where SessionId = @SessionId and IsActive = 1
    order by id desc;

    -- 5: stored discrepancies, the durable record of what the expert changed
    select id, SessionId, AssignmentId, ExpertReportId, FieldType, QuestionId, DisplayOrder,
           FieldKey, AIValue, ExpertValue, IsDiscrepancy, Severity, CreatedAt
    from AICodeInterviewReportDiscrepancy
    where SessionId = @SessionId
    order by DisplayOrder, FieldType, FieldKey;
END
GO
