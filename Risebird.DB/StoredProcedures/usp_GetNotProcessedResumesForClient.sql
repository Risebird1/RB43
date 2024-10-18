
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
declare @clientId int,@ClientRoleid int  
set @clientId = (select ClientId from ClientUser where UserId = (select id from UserList where EmailId = @clientEmailId and idProfile =4))  
set @ClientRoleid = (select RoleId from ClientUser where UserId = (select id from UserList where EmailId = @clientEmailId and idProfile =4))  
if(@clientId is not null and @clientId > 0)  
Begin 
if(@ClientRoleid=1 or @ClientRoleid=4)
begin
select irf.IRFId as IRFName, cr.ResumeLink, cr.IsResumeParsed, cr.IsResumeParsingFailed, cr.IsEmailMissing, cr.AddedOn, cr.AddedBy, cr.IRFId,cr.id,ul.EmailId as ClientHREmailId
from CandidateResume cr   
join IRF on irf.id = cr.IRFId  
left join UserList ul on ul.id=cr.AddedBy
where irf.ClientId =@clientId and cr.IsProcessed = 0  and ul.EmailId=@clientEmailId
order by irf.IRFId  
end
else
begin
 select irf.IRFId as IRFName, cr.ResumeLink, cr.IsResumeParsed, cr.IsResumeParsingFailed, cr.IsEmailMissing, cr.AddedOn, cr.AddedBy, cr.IRFId,cr.id,ul.EmailId as ClientHREmailId
 from CandidateResume cr   
 join IRF on irf.id = cr.IRFId 
 left join UserList ul on ul.id=cr.AddedBy
 where irf.ClientId =@clientId and cr.IsProcessed = 0   
 order by irf.IRFId  
end
End  
End  
GO


