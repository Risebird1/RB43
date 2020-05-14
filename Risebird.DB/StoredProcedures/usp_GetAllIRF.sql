/****** Object:  StoredProcedure [dbo].[usp_GetAllIRF]    Script Date: 20-03-2020 12.16.55 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetAllIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllIRF
END
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetAllIRF]  
@EmailId varchar(200),   
@Profile varchar(100),
@timeNow datetime
AS  
Begin 

DECLARE @tbl_ConfirmedMeetings TABLE (
    IRFId INT,
    ConfirmedMeetings int 
);
INSERT INTO @tbl_ConfirmedMeetings
select IRF.id as IRFId, count(im.confirmationStatus) as ConfirmedMeetings 
from IRF left join InterviewMeetings im on IRF.IRFId = im.IRFId 
where im.confirmationStatus=1 group by IRF.id

DECLARE @tbl_TotalMeetings TABLE (
    IRFId INT,
    TotalMeetings int 
);
INSERT INTO @tbl_TotalMeetings
select IRF.id as IRFId, count(im.id) as TotalMeetings
from IRF left join InterviewMeetings im on IRF.IRFId = im.IRFId 
group by IRF.id

DECLARE @tbl_SelectedMeetings TABLE (
    IRFId INT,
    SelectedMeetings int 
);
INSERT INTO @tbl_SelectedMeetings
select irf.id as IRFId, count(im.id) as SelectedMeetings
from irf left join InterviewMeetings im on irf.IRFId= im.IRFId left join InterviewComments ic on im.id= ic.InterviewId
where ic.InterviewStatus= 2 or ic.InterviewStatus=4 
group by irf.id

DECLARE @tbl_ScheduledMeetings TABLE (
    IRFId INT,
    ScheduledMeetings int 
);
INSERT INTO @tbl_ScheduledMeetings
select irf.id as IRFId, count(im.id) as ScheduledMeetings 
from irf left join InterviewMeetings im on irf.IRFId = im.IRFId 
left join InterviewComments ic on ic.InterviewId= im.id 
where ic.MeetingStatus is null and im.confirmationStatus=1 and im.interviewDate>= @timeNow
group by irf.id


DECLARE @tbl_SubscribedTo TABLE (
    SubId int,
    IRFId INT
);
INSERT INTO @tbl_SubscribedTo
select id,IRFId from Subscribers
where UserId= (select id from  UserList ul where ul.EmailId = @EmailId and idProfile= (select id from [Profile] where [Name]=@Profile))


IF (@Profile = 'Client')  
	BEGIN  
		declare
		@ClientId int = (select ClientId from  ClientUser cu left join UserList ul on cu.UserId = ul.id  where ul.EmailId = @EmailId)
		
		select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,
		irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails, irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel, irf.IsPaused, irf.ClientName,
		Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings, tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed, 1 as IsClientUser
		from irf left join [dbo].[TransClientUser] tc on tc.IRFId = IRF.id left join [dbo].[UserList] ul on ul.id = tc.ClientUserId
		left join ClientUser cu on tc.ClientUserId = cu.UserId
		left join InterviewMeetings im on irf.IRFId = im.IRFId left join Client on IRF.ClientId = Client.id left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId
		left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId
		left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId
		left join @tbl_ScheduledMeetings tschm on irf.id= tschm.IRFId
		where ul.EmailId = @EmailId and cu.ClientId=@ClientId
		group by irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,
		irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails,irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel, irf.IsPaused, irf.ClientName,
		Client.[Name], tcm.ConfirmedMeetings, ttm.TotalMeetings, tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings)
	End  
Else if(@Profile='Recruiter')
	Begin
		select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,
		irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails,irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel, irf.IsPaused, irf.ClientName,
		Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings, tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed
		from irf left join [dbo].[TransRecruiter] tr on tr.IRFId = IRF.id left join [dbo].[UserList] ul on ul.id = tr.UserId
		left join InterviewMeetings im on irf.IRFId = im.IRFId left join Client on IRF.ClientId = Client.id left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId
		left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId
		left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId
		left join @tbl_ScheduledMeetings tschm on irf.id= tschm.IRFId
		where ul.EmailId = @EmailId 
		group by irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,
		irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails,irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel, irf.IsPaused, irf.ClientName,
		Client.[Name], tcm.ConfirmedMeetings, ttm.TotalMeetings, tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings)
	End
Else if(@Profile='CuratedUser')
	Begin

		select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.IsDeleted,
		irf.CalendlyLink, irf.IsPaused, irf.ClientName,
		Client.[Name] as ClientCompName, tsm.SelectedMeetings, sub.SubId
		from irf left join Client on IRF.ClientId = Client.id
		left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId
		left join @tbl_SubscribedTo sub on irf.id=sub.IRFId
		where Client.CuratedLink=1 
		group by irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.IsDeleted,
		irf.CalendlyLink, irf.IsPaused, irf.ClientName,
		Client.[Name], tsm.SelectedMeetings, sub.SubId
	End
Else
begin
		select irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,
		irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails, irf.Discount, irf.IsBiometricEnabled, irf.PriorityLevel,Client.CuratedLink as CuratedLink, irf.IsPaused, irf.ClientName,
		Client.[Name] as ClientCompName, tcm.ConfirmedMeetings, ttm.TotalMeetings, tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings) as TotalBudgetConsumed
		from irf left join InterviewMeetings im on irf.IRFId = im.IRFId inner join Client on IRF.ClientId = Client.id left join @tbl_ConfirmedMeetings tcm on irf.id = tcm.IRFId
		left join @tbl_TotalMeetings ttm on irf.id=ttm.IRFId
		left join @tbl_SelectedMeetings tsm on irf.id=tsm.IRFId
		left join @tbl_ScheduledMeetings tschm on irf.id= tschm.IRFId
		group by irf.id, irf.IRFId, irf.JDTiltle, irf.StartDate, irf.EndDate, irf.JDLink,irf.RisebirdHr, irf.PerMintuteRate, irf.IsDeleted,
		irf.TotalInvites, irf.CalendlyLink, irf.TotalBudget, irf.CompanyDetails,irf.Discount,Client.CuratedLink , irf.IsBiometricEnabled, irf.PriorityLevel, irf.IsPaused, irf.ClientName,
		Client.[Name], tcm.ConfirmedMeetings, ttm.TotalMeetings, tsm.SelectedMeetings, tschm.ScheduledMeetings, (irf.PerMintuteRate*tcm.ConfirmedMeetings)
End
End

GO


