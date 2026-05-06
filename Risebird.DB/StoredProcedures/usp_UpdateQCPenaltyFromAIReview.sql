IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateQCPenaltyFromAIReview'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateQCPenaltyFromAIReview
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Called after the Payment-Ticket AI review assistant returns a "reduce" or "approve" verdict.
-- Updates:
--   1. QcInterviewAIevaluation  – revised total penalty % and merged details
--   2. InterviewPoints          – QCPoints revised to the new penalty %
--   3. PaymentSLADetails        – RecommendationComments updated with AI reasoning
CREATE PROCEDURE usp_UpdateQCPenaltyFromAIReview
    @InterviewId           int,
    @RevisedPenaltyPct     int,           -- revised_penalty_amount.revised_percentage
    @UpdatedMergedDetails  nvarchar(max), -- full AI response JSON stored as new MergedAIQCDetails
    @ReasoningComment      nvarchar(2000) -- updated_reasoning_comment from AI
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Update QcInterviewAIevaluation
    UPDATE QcInterviewAIevaluation
    SET AIMergeTotalPenalty = @RevisedPenaltyPct
    WHERE InterviewId = @InterviewId;

    -- 2. Update QCPoints in InterviewPoints for the interviewer's record
    UPDATE ip
    SET ip.QCPoints = @RevisedPenaltyPct
    FROM InterviewPoints ip
    INNER JOIN UserList ul ON ul.id = ip.userid
    INNER JOIN InterviewMeetings im ON im.id = @InterviewId AND im.Interviewer = ul.EmailId
    WHERE ip.Interviewid = @InterviewId;

    -- 3. Update reasoning comment in PaymentSLADetails
    UPDATE PaymentSLADetails
    SET RecommendationComments = @ReasoningComment
    WHERE Interviewid = @InterviewId;
END
GO
