
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetNotProcessedResumesForClient'))
BEGIN
    DROP PROCEDURE dbo.usp_GetNotProcessedResumesForClient
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetNotProcessedResumesForClient]  
@clientEmailId varchar(100)
AS  
Begin

declare @clientId int
set @clientId = (select id from ClientUser where UserId = (select id from UserList where EmailId = @clientEmailId and idProfile =4))


if(@clientId is not null and @clientId > 0)
Begin
	select irf.IRFId as IRFName, cr.ResumeLink, cr.IsResumeParsed, cr.IsResumeParsingFailed, cr.IsEmailMissing, cr.AddedOn, cr.AddedBy, cr.IRFId from CandidateResume cr 
	join IRF on irf.id = cr.IRFId
	where irf.ClientId =@clientId and cr.IsProcessed = 0 
	order by irf.IRFId
End
	

End
GO


