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

 
select distinct userlist.EmailId from [dbo].[TransInterviewer] tr
join [dbo].[IRF] irf on tr.IRFId = irf.id
join [dbo].[UserList] userlist on tr.UserId = userlist.id
where irf.IRFId like @IRFID


	
	
End



GO



