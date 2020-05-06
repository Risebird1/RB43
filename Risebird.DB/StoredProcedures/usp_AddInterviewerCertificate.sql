/****** Object:  StoredProcedure [dbo].[usp_AddInterviewerCertificate]    Script Date: 5/1/2020 4:00:35 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInterviewerCertificate'))
BEGIN
    DROP PROCEDURE [dbo].[usp_AddInterviewerCertificate]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



Create PROCEDURE [dbo].[usp_AddInterviewerCertificate]


@InterviewerProfileId int,
@CertificateID nvarchar(200),
@CertificateName nvarchar(200),
@Version nvarchar(100),
@ExpiryDate datetime

AS
Begin

INSERT INTO [dbo].[TransInterviewerCertificate]
           ([InterviewerProfileId]
           ,[CertificateID]
           ,[CertificateName]
           ,[Version]
           ,[ExpiryDate])
     VALUES
           (@InterviewerProfileId,
			@CertificateID,
			@CertificateName,
			@Version,
			@ExpiryDate)


End
GO


