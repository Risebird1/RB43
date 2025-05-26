IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetApplicantById'))
BEGIN
    DROP PROCEDURE usp_GetApplicantById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_GetApplicantById    
@applicantId int         
AS                  
BEGIN         
    

	select ida.id, ida.EmailId, ida.Phone, ida.ResumeLink, ida.CreatedAt, ida.IRFId, irf.IRFId as SkillChannelName, ida.MAGIds,
	irf.JDTiltle, irf.JDTitleTwo, ida.AIVerdict, ida.AIEvaluationGeneratedAt, ida.AIEvaluationPerformedBy,
	ida.isVerified, ida.IsEmployed, ida.CompanyName, ida.ReferredBy, ida.EVGeneratedAt, ida.EVGeneratedBy,
	irf.IsDeleted as IsIrfDeleted, ag.AssessmentGroup, ul.EmailId as ChannelOwner, irf.CreatedOn as ChannelCreationDate, ida.AIAnalysisScore,
	CASE 
        WHEN ulI.IsActive = 1 THEN 'Acknowledged'
        ELSE ''
    END AS AcknowledgementStatus
	from InterviewerDirectApplication ida
	join IRF on irf.id = ida.irfId
	join AssessmentGroup ag on ag.id = irf.AssessmentGroupId
	join UserList ul on ul.id = irf.RisebirdHr
	join Client cl on cl.id = irf.ClientId
	left join UserList ulI on ulI.EmailId = ida.EmailId
	where ida.id = @applicantId



END 
GO