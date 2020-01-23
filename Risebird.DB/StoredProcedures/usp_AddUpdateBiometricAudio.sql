/****** Object:  StoredProcedure [dbo].[usp_AddUpdateBiometricAudio]    Script Date: 23-01-2020 7.23.24 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddUpdateBiometricAudio'))
BEGIN
    DROP PROCEDURE dbo.usp_AddUpdateBiometricAudio
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_AddUpdateBiometricAudio]
@IRFId int,
@CandidateId int,
@UploadedByUserId int,
@AudioFileLink nvarchar(max),
@PhraseDetail nvarchar(max),
@IsVoiceBiometric bit,
@Description nvarchar(max)

AS
Begin

IF NOT EXISTS (SELECT 1   FROM   CandidateAudio   WHERE  CandidateId=@CandidateId and IRFId=@IRFId and [Description]=@Description)
Begin
INSERT INTO [dbo].[CandidateAudio]
           (
		   IRFId,
		   CandidateId,	
		   UploadedByUserId,
		   AudioFileLink,
		   PhraseDetail,
		   IsVoiceBiometric,
		   [Description]
		   
           )
     VALUES
           (@IRFId,
@CandidateId,
@UploadedByUserId,
@AudioFileLink,
@PhraseDetail,
@IsVoiceBiometric,
@Description
)


End

Else

Begin

update CandidateAudio set 

UploadedByUserId=@UploadedByUserId,
AudioFileLink=@AudioFileLink,
PhraseDetail=@PhraseDetail,
IsVoiceBiometric=@IsVoiceBiometric

where CandidateId=@CandidateId and IRFId=@IRFId and [Description]=@Description

End

END

GO


