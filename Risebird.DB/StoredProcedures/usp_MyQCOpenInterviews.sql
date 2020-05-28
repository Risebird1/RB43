/****** Object:  StoredProcedure [dbo].[usp_MyQCOpenInterviews]    Script Date: 14-11-2019 7.03.24 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_MyQCOpenInterviews'))
BEGIN
    DROP PROCEDURE dbo.usp_MyQCOpenInterviews 
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_MyQCOpenInterviews]
@emailID varchar(200)
AS
BEGIN
select im.*,irf.id as irfIntId, irf.JDLink from InterviewMeetings im
inner join InterviewComments ic on ic.InterviewId=im.id
inner join IRF irf on im.IRFId=irf.IRFId 
inner join TransQualityController tqc on tqc.IRFId= IRF.id
inner join UserList ul on tqc.UserId= ul.id
where im.id not in(select InterviewId from QCFeedbackReport) and
im.interviewer!=@emailID and ul.EmailId=@emailID and
confirmationStatus=1 and interviewer is not null and interviewDate <= GETDATE() 
and (ic.InterviewStatus=2 or ic.InterviewStatus=4)

END
GO




