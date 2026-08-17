IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AssignAICodeInterviewExpert'))
BEGIN
    DROP PROCEDURE dbo.usp_AssignAICodeInterviewExpert
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
-- Assigns an expert to audit one completed AI code interview report.
-- Returns Status: 1 = newly assigned, 2 = already held by this expert, 3 = held by another expert,
-- 0 = session is not an auditable AI code interview.
Create Procedure usp_AssignAICodeInterviewExpert
@SessionId varchar(50),
@ExpertEmail varchar(150),
@AssignedBy varchar(150) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    declare @InterviewId int, @IRFIntId int, @AssignmentId int = 0, @Status int = 0;
    declare @ExistingId int, @ExistingExpert varchar(150);

    select @InterviewId = im.id, @IRFIntId = irf.id
    from InterviewMeetings im
    join IRF irf on irf.IRFId = im.IRFId
    join InterviewComments ic on ic.InterviewId = im.id
    where im.SessionId = @SessionId
      and isnull(irf.IsAIInterview, 0) = 1
      and irf.AIInterviewType = 2 -- 2 means preset
      and exists (
            select 1 from SkillChannelSampleQuestions sq
            where sq.IRFId = irf.id and isnull(sq.IsCodingQuestion, 0) = 1)
      and ic.MeetingStatus = 4 -- 4 means completed
      and ic.ReportOfAIInterviewByAssistant is not null
      and ic.ReportOfAIInterviewByAssistant <> '';

    if @InterviewId is null
    begin
        select 0 as AssignmentId, 0 as Status;
        return;
    end

    select top 1 @ExistingId = id, @ExistingExpert = ExpertEmail
    from AICodeInterviewExpertAssignment
    where SessionId = @SessionId and IsActive = 1;

    if @ExistingId is not null
    begin
        set @AssignmentId = @ExistingId;
        set @Status = case when @ExistingExpert = @ExpertEmail then 2 else 3 end;
    end
    else
    begin
        insert into AICodeInterviewExpertAssignment
            (SessionId, InterviewId, IRFIntId, ExpertEmail, AssignedBy, AssignedAt, IsActive)
        values
            (@SessionId, @InterviewId, @IRFIntId, @ExpertEmail, isnull(@AssignedBy, @ExpertEmail), GETDATE(), 1);

        set @AssignmentId = SCOPE_IDENTITY();
        set @Status = 1;
    end

    select @AssignmentId as AssignmentId, @Status as Status;
END
GO
