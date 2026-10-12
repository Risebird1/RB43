IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateReportDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateReportDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Edit Interviewer Comment on the client report page (/Interview/UpdateInterviewReport).
-- QC Activity: when the interview row exists and the text actually changed (exact, case-sensitive
-- comparison), one ReportEditLog row keeps the full text before and after (HTML-encoded, as saved)
-- and the editor's session email. Logging is skipped while ReportEditLog is missing.
CREATE PROCEDURE usp_UpdateReportDetails
@interviewId int,
@comment varchar(max),
@emailid varchar(100)
AS
BEGIN
DECLARE @RowExists bit = 0;
DECLARE @OldComment varchar(max) = NULL;
-- Taken before the update, so the log row is never later than the InterviewCommentsHistory snapshot the
-- update trigger writes: the collector rebuilds history only before the first log row (no double count).
DECLARE @EditedAtUtc datetime = GETUTCDATE();
SELECT TOP 1 @RowExists = 1, @OldComment = InterviewerComment
FROM InterviewComments WHERE interviewid = @interviewId ORDER BY id DESC;

update InterviewComments set InterviewerComment=ISNull(@comment,InterviewerComment),ModifiedBy=@emailid,
ModifiedOn=dateadd(MINUTE, 330, getdate()) where interviewid=@interviewId

IF @RowExists = 1
   AND @comment IS NOT NULL
   AND (@OldComment IS NULL
        OR DATALENGTH(@OldComment) <> DATALENGTH(@comment)
        OR @OldComment COLLATE Latin1_General_BIN2 <> @comment COLLATE Latin1_General_BIN2)
   AND OBJECT_ID(N'dbo.ReportEditLog', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.ReportEditLog (InterviewId, FieldName, OldValue, NewValue, EditedBy, EditedAtUtc, Source)
    VALUES (@interviewId, 'InterviewerComment', @OldComment, @comment,
            NULLIF(LTRIM(RTRIM(@emailid)), ''), @EditedAtUtc, 'usp_UpdateReportDetails')
END
END
GO
