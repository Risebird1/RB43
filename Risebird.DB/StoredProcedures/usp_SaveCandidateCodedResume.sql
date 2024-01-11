IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_SaveCandidateCodedResume'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCandidateCodedResume
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_SaveCandidateCodedResume
@id int,
@CodedResumeLink varchar(1000),
@IsResumeRedactAttempted bit=null,
@IsResumeRedactFailed bit=null
AS
BEGIN
Update Candidate set CodedResumeLink=@CodedResumeLink, IsResumeRedactAttempted = @IsResumeRedactAttempted, IsResumeRedactFailed = @IsResumeRedactFailed From Candidate  where id=@id
END
Go