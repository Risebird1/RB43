IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetApplicantInterviewerAIEvaluation'))
BEGIN
    DROP PROCEDURE dbo.usp_GetApplicantInterviewerAIEvaluation
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetApplicantInterviewerAIEvaluation

@id int
AS
BEGIN

select top 1
    ida.*,
    COALESCE(irf.JDTiltle, ag.AssessmentGroup) AS JDTiltle,
    COALESCE(irf.AssessmentGroupId, ida.AssessmentGroupId) AS AssessmentGroupId
from InterviewerDirectApplication ida
left join IRF irf on irf.id = ida.IRFId and ida.IRFId > 0
left join AssessmentGroup ag on ag.id = ida.AssessmentGroupId
where ida.id = @id order by ida.CreatedAt desc

END
Go
