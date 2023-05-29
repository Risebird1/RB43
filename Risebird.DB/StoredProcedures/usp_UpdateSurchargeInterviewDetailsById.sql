IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateSurchargeInterviewDetailsById'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateSurchargeInterviewDetailsById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateSurchargeInterviewDetailsById
@interviewid int,
@surchargecomment varchar(500),
@surchargeamt int
AS
BEGIN
Update InterviewMeetings set Surcharge=@surchargeamt,SurchargeComment=@surchargecomment where id=@interviewid
END
GO