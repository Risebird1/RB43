-- =============================================
-- SP Name:  dbo.usp_ListOfIRFInterviwer
-- Author: Prem     
-- Create date: 7th July 2019
-- Description: Add Meeting
-- =============================================
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_ListOfIRFInterviwer'))
BEGIN
    DROP PROCEDURE dbo.usp_ListOfIRFInterviwer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_ListOfIRFInterviwer]
@IRFID varchar(200)
AS
Begin


IF EXISTS (Select 1 from IRF where IRFId =@IRFId)
	BEGIN
		select distinct userlist.EmailId from [dbo].[TransInterviewer] tr
		join [dbo].[IRF] irf on tr.IRFId = irf.id
		join [dbo].[UserList] userlist on tr.UserId = userlist.id
		where irf.IRFId like @IRFID and tr.IsActive=1
	END
Else
	BEGIN
		select distinct userlist.EmailId from [dbo].[TransInterviewerSkillsLeadInterviewer] tr
		join [dbo].[InterviewerSkillsChannel] ISC on tr.InterviewerSkillId = ISC.id
		join [dbo].[UserList] userlist on tr.UserId = userlist.id
		where ISC.InterviewerSkillId like @IRFID

	END
 
End

GO



