/****** Object:  StoredProcedure [dbo].[usp_GetIRFDetailsForCandidate]  ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetIRFDetailsForCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetIRFDetailsForCandidate
END
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
    
CREATE PROCEDURE [dbo].[usp_GetIRFDetailsForCandidate]     
  @Email varchar(200),     
  @irfId int           
AS            
BEGIN
declare @EmailParam varchar(200),     
		@irfIdParam int

set @EmailParam = @Email
set @irfIdParam =@irfId

declare @IRFName varchar(25) = (select IRFId from IRF where id =@irfIdParam)

declare @candidateIsActive bit = 0
set @candidateIsActive = (select IsActive from UserList  where trim(EmailId) = trim(@EmailParam))

if(@candidateIsActive = 1)
Begin
	select case       
	when ul.LastName is null then ul.FirstName      
	else RTRIM(LTRIM(ul.FirstName)) + SPACE(1) + RTRIM(LTRIM(ul.LastName))      
	end as [Name],ir.IRfId as IRFName,ul.EmailId, ul.MobileNumber, ul.uniqueid,
	ir.JDTiltle, ir.JDLink, ir.ClientName, ir.CalendlyLink, ir.PrimarySkillInvite, cl.CompanyLink, 
	cl.CompanyVideoLink, ulr.EmailId as ChannelOwnerEmail, 
	ulr.MobileNumber as ChannelOwnerMobile,ca.ClientHRAttemptContact
	from Candidate ca join irf ir on ca.IRFId=ir.id
	join CandidatesMaster cm on cm.id=ca.CandidateMasterId
	join Client cl on cl.id = ir.ClientId 
	join UserList ul on ul.id = cm.UserId 
	join UserList ulr on ulr.id = ir.RisebirdHr 
	where ul.IsActive =1 and ul.emailid =@EmailParam and ir.Id = @irfIdParam

	select im.id as Interviewid, im.resumeLink, im.zoomLink, im.interviewDate, im.candidateEmail, im.interviewSlot,
	im.ConfirmationStatus,ic.MeetingStatus,ic.InterviewStatus 
	from InterviewMeetings im join InterviewComments ic on ic.interviewid=im.id where IRFId = @IRFName and candidateEmail = @EmailParam

	Select Uniqueid From ScheduleInterviewEvent where SkillChannelRelation=@IRFName and CandidateEmail=@EmailParam and IsNull(IsOnetimeUsed,0)=0

End
Else
Begin
	select @candidateIsActive 
End

           
END 
Go
