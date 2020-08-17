IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateSharedDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidateSharedDetails
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_GetCandidateSharedDetails
@id int
AS
BEGIN
Select CandidateName as Name,CandidateEmail as EmailId,InterviewerComment as InterviewerComments from SharedInterviewcomments where id=@id

END