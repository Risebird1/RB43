/****** Object:  StoredProcedure [dbo].[usp_GetIRFInterviewerMembers]    Script Date: 04-09-2020 12.06.07 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetIRFInterviewerMembers'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFInterviewerMembers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[usp_GetIRFInterviewerMembers]

@IRFId int

AS
Begin

declare @commonTable table (IRFId int, [Type] varchar(25), EmailId varchar(100), ResumeLink varchar(500),TotalInterviews int, TotalQCInterviews int, MobileNumber varchar(20))


	insert into @commonTable 

		select ti.IRFId, 'Interviewer' as [Type] ,ul.EmailId, ipr.ResumeLink,
		count(im.id) as TotalInterviews,
		0 as TotalQCInterviews, ul.MobileNumber 
		from TransInterviewer ti join Interviewer ipr on ipr.UserId = ti.UserId
		join UserList ul on ul.id = ti.UserId
		join IRF on irf.id = ti.IRFId
		join InterviewMeetings im on im.IRFId=irf.IRFId 
		where irf.id=@IRFId and im.interviewer=ul.EmailId and im.confirmationStatus=1
		group by ti.IRFId, ul.EmailId, ipr.ResumeLink, ul.MobileNumber           --6
		
	insert into @commonTable 

		select tqc.IRFId, 'QCInterviewer' as [Type] ,ul.EmailId, ipr.ResumeLink,
		0 as TotalInterviews,
		count(im.id) as TotalQCInterviews,
		ul.MobileNumber 
		from TransQualityController tqc join Interviewer ipr on ipr.UserId = tqc.UserId
		join UserList ul on ul.id = tqc.UserId
		join IRF on irf.id = tqc.IRFId
		join InterviewMeetings im on im.IRFId=irf.IRFId 
		where tqc.IRFId = @IRFId and im.QCInterviewer=ul.EmailId and im.confirmationStatus=1
		group by tqc.IRFId, ul.EmailId, ipr.ResumeLink, ul.MobileNumber

		

	insert into @commonTable 
		
		select tei.IRFId, 'Emergency Interviewer' as [Type] ,ul.EmailId, ipr.ResumeLink,
		count(im.id) as TotalInterviews,
		0 as TotalQCInterviews,  
		ul.MobileNumber 
		from TransEmergencyInterviewer tei join Interviewer ipr on ipr.UserId = tei.UserId
		join UserList ul on ul.id = tei.UserId
		join IRF on irf.id = tei.IRFId
		join InterviewMeetings im on im.IRFId=irf.IRFId 
		where  tei.IRFId = @IRFId and im.interviewer=ul.EmailId and im.confirmationStatus=1
		group by tei.IRFId, ul.EmailId, ipr.ResumeLink, ul.MobileNumber 

select * from @commonTable		 

End

GO


