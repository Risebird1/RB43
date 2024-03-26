IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_AssignYourSelfQC'))
BEGIN
    DROP PROCEDURE dbo.usp_AssignYourSelfQC
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_AssignYourSelfQC
@id int,
@emailId varchar(100)
As
Begin
Update TechQualityDetails set QcInterviewer=@emailId where Interviewid=@id
End
GO