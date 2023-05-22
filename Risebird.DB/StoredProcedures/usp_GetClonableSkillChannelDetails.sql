
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

	select csc.MeetingPlatform, csc.IsAadhaarVerificationRequired, irf.Heuristicsscore, irf.JobCountry, irf.CompanyType 
	from ClonableSkillChannel csc join IRF on IRF.id = csc.CreatedFromIRFId
	where csc.id = @id

End
GO


