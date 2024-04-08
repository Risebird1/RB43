
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetClonableSkillChannelDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_GetClonableSkillChannelDetails
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetClonableSkillChannelDetails]  
@id int
AS  
Begin

	select csc.MeetingPlatform, csc.IsAadhaarVerificationRequired, irf.Heuristicsscore, irf.JobCountry, irf.CompanyType, asg.AssessmentGroup as AssessmentGroupName, asg.id as AssessmentGroupId 
	from ClonableSkillChannel csc join IRF on IRF.id = csc.CreatedFromIRFId
	join AssessmentGroup asg on asg.id = irf.AssessmentGroupId
	where csc.id = @id

End
GO


