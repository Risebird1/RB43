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

select top 1 ida.*, irf.JDTiltle, irf.AssessmentGroupId from InterviewerDirectApplication ida
join IRF on irf.id = ida.IRFId
where ida.id =@id order by ida.CreatedAt desc

END
Go