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
select InterviewMeetings.*, irf.id as irfIntId, irf.JDLink from InterviewMeetings
inner join IRF irf on InterviewMeetings.IRFId=irf.IRFId left join InterviewComments ic on ic.InterviewId= InterviewMeetings.id
where IRF.id in (select IRFId from TransQualityController tqc join UserList ul on tqc.UserId= ul.id where EmailId = @emailID)
and InterviewMeetings.confirmationStatus=1 and ic.MeetingStatus=4 and InterviewMeetings.interviewer!=@emailID
END

END   
GO


