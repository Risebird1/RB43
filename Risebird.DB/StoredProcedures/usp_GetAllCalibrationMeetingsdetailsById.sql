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
Begin
if(@type='All')
begin
Select IRF.IRFId as IRFName,IRF.JDTiltle,cm.MeetingDate,cm.MeetingLink,cm.VideoLink,cm.CreatedOn,cm.CreatedBy 
From CalibrationMeetings cm join IRF on IRF.id=cm.IRFId
order by cm.MeetingDate desc
end
else
begin
Select IRF.IRFId as IRFName,IRF.JDTiltle,cm.MeetingDate,cm.MeetingLink,cm.VideoLink,cm.CreatedOn,cm.CreatedBy From CalibrationMeetings cm join IRF on IRF.id=cm.IRFId
join TransClientUser tcu join Userlist ul on ul.id=tcu.ClientUserid on tcu.IRFId=IRF.id 
Where ul.Emailid=@Emailid
order by cm.MeetingDate desc
end
end
Go


