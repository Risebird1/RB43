IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidatesForAdvancedResumeScreening'))
BEGIN
    DROP PROCEDURE dbo.usp_GetCandidatesForAdvancedResumeScreening
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Candidates on an IRF that bulk advanced screening should pick up: a resume on file and no
-- advanced screening yet. Candidates screened only by the older V2 screener are included, so a
-- bulk run brings the whole channel onto the advanced rubric.
Create PROCEDURE [dbo].[usp_GetCandidatesForAdvancedResumeScreening]
@IRFId int
AS
Begin
	select ca.IRFId, ca.CandidateMasterId, ul.EmailId, ca.ResumeLink
	from Candidate ca join CandidatesMaster cm on ca.CandidateMasterId = cm.id
	join UserList ul on ul.id = cm.UserId
	where ca.IRFId = @IRFId
	and ISNULL(ca.ResumeLink, '') <> ''
	and ISNULL(ca.AIAdvancedScreeningResultJson, '') = ''
End
GO
