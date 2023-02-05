 
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetSkillChannelDetailsForSMEByIdToUpdate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetSkillChannelDetailsForSMEByIdToUpdate
END


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetSkillChannelDetailsForSMEByIdToUpdate]    
@irfId int,
@interviewerProfileId int
AS    
Begin   

declare @smeEmail varchar(100) = (select trim(ul.EmailId) from InterviewerProfile ipr join UserList ul on ul.id = ipr.UserId where ipr.id = @interviewerProfileId)

declare @existingAudioCommentBy varchar(100) = (select trim(AudioCommentBy) from IRF where id = @irfId)

if(@smeEmail = @existingAudioCommentBy)
begin
	select id, IRFId, JDLink, AudioComment from IRF where id = @irfId 
End
Else
Begin
	select id, IRFId, JDLink from IRF where id = @irfId 
End
    
End  

