IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_GetAllCalibrationMeetingsdetailsById'))
BEGIN
    DROP PROCEDURE dbo.usp_GetAllCalibrationMeetingsdetailsById
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_GetAllCalibrationMeetingsdetailsById
@Emailid varchar(150),
@type varchar(5)
as
BEGIN 
if(@type='All')
begin
Declare @Profile varchar(150) 
Select @Profile=pr.Name from UserList ul join Profile pr on pr.id=ul.idProfile where EmailId=@Emailid
if (@Profile = 'SeniorHR' or @Profile = 'HR')
			begin
			Select IRF.IRFId as IRFName,IRF.JDTiltle,cm.MeetingDate,cm.MeetingLink,cm.VideoLink,cm.CreatedOn,cm.CreatedBy, ul.EmailId as ChannelOwner
From CalibrationMeetings cm join IRF on IRF.id=cm.IRFId
Join UserList ul on IRF.RisebirdHr=ul.id where ul.EmailId=@Emailid
order by cm.MeetingDate desc 
			End
			Else
			Begin
Select IRF.IRFId as IRFName,IRF.JDTiltle,cm.MeetingDate,cm.MeetingLink,cm.VideoLink,cm.CreatedOn,cm.CreatedBy ,ul.EmailId as ChannelOwner
From CalibrationMeetings cm join IRF on IRF.id=cm.IRFId
left Join UserList ul on IRF.RisebirdHr=ul.id 
order by cm.MeetingDate desc
end
end

else
begin
Select IRF.IRFId as IRFName,IRF.JDTiltle,cm.MeetingDate,cm.MeetingLink,cm.VideoLink,cm.CreatedOn,cm.CreatedBy From CalibrationMeetings cm join IRF on IRF.id=cm.IRFId
join TransClientUser tcu join Userlist ul on ul.id=tcu.ClientUserid on tcu.IRFId=IRF.id 
Where ul.Emailid=@Emailid
order by cm.MeetingDate desc
end
End
Go


