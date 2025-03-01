  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateVideoConsentForm'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateVideoConsentForm
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_UpdateVideoConsentForm
@irfid int,
@uploadedFileURL varchar(1000),
@CreatedOn datetime
AS
BEGIN
Update IRF set CandidateVideo=@uploadedFileURL where id=@irfid
END
GO