/****** Object:  StoredProcedure [dbo].[usp_UpdateCandidateVoiceUserId]    Script Date: 24-01-2020 2.20.58 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateCandidateVoiceUserId'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateCandidateVoiceUserId
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_UpdateCandidateVoiceUserId]
@IRFId int,
@CandidateId int,
@VoiceItUserId nvarchar(max)

AS
Begin
declare
@transCandidateId int = (select ca.id from CandidatesMaster cm join Candidate ca on cm.EmailId=ca.EmailId where ca.IRFId=@IRFId and cm.id=@CandidateId)

update Candidate set 
VoiceItUserId=@VoiceItUserId
where id =@transCandidateId

END

GO


