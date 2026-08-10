IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetRaisedIssueIndexBatch'))
BEGIN
    DROP PROCEDURE dbo.usp_GetRaisedIssueIndexBatch
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Returns denormalized raised-issue rows for Azure AI Search index (raisedissueindex).
-- @RaisedIssueId NULL = all rows (FullCrawl)
-- @RaisedIssueId set  = single row by rs.id (Upsert)
-- No profileName filter here; role/list-type visibility is applied via OData filters in
-- RaisedIssueListingSearchRepository.SearchAsync.
CREATE PROCEDURE [dbo].[usp_GetRaisedIssueIndexBatch]
    @RaisedIssueId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rs.id                                                 AS Id,
        rs.RequestType,
        rs.Comment,
        rs.AttachmentURL,
        rs.RequestedBy,
        rs.RequestedOn,
        rs.Status,
        rs.StatusComment,
        rs.ClosedBy,
        rs.ClosedOn,
        rs.Requester,
        rs.interviewId                                        AS InterviewId,
        rs.AIReviewResponse,
        ul.MobileNumber,
        ul.FirstName                                          AS Name,
        IRF.id                                                AS IrfIntId,
        im.candidateEmail                                     AS CandidateEmail,
        im.Interviewer,
        CAST(CASE WHEN EXISTS (
            SELECT 1 FROM dbo.RaisedIssuesHistory rih
            WHERE rih.RaisedIssueId = rs.id AND rih.Action = 'Reopened'
        ) THEN 1 ELSE 0 END AS bit)                            AS IsReopened
    FROM       dbo.RaisedIssues rs
    LEFT JOIN  dbo.UserList ul ON ul.EmailId = rs.RequestedBy
    LEFT JOIN  dbo.InterviewMeetings im ON im.Id = rs.interviewId
    LEFT JOIN  dbo.IRF ON IRF.IRFID = im.IRFId
    WHERE (@RaisedIssueId IS NULL OR rs.id = @RaisedIssueId)
    ORDER BY rs.RequestedOn DESC;
END
GO
