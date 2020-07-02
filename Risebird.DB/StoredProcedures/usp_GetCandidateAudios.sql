/****** Object:  StoredProcedure [dbo].[usp_GetCandidateAudios]    Script Date: 27-03-2020 6.53.28 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetCandidateAudios'))
BEGIN
    DROP PROCEDURE usp_GetCandidateAudios
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetCandidateAudios]

@CandidateId int,
@IRFId int

AS
Begin

select cau.*, CASE WHEN ul.LastName is not null THEN ul.FirstName + ' ' + ul.LastName ELSE ul.FirstName END as CandidateName, ca.VoiceItUserId, irf.IRFId as IRFName
from CandidateAudio cau join CandidatesMaster cm on cau.CandidateId= cm.id 
join Candidate ca on cm.id = ca.CandidateMasterId  
join UserList ul on cm.UserId = ul.id
join irf on irf.id=cau.IRFId
where cau.CandidateId =@CandidateId and ca.IRFId =@IRFId
group by cau.id, cau.IRFId, cau.CandidateId,cau.UploadedByUserId, cau.PhraseDetail, cau.IsVoiceBiometric, cau.AudioFileLink,
cau.[Description], cau.IsEnrolled, cau.EnrollmentMessage, ul.FirstName, ul.LastName, ca.VoiceItUserId, irf.IRFId

End


GO


