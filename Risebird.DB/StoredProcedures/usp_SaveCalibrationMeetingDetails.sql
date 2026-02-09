IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_SaveCalibrationMeetingDetails'))
BEGIN
    DROP PROCEDURE dbo.usp_SaveCalibrationMeetingDetails
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE usp_SaveCalibrationMeetingDetails    
@irfid int,    
@Emailid varchar(150),    
@meetinglink varchar(500),    
@meetingdate datetime,    
@CreatedOn datetime,
@meetingid varchar(200)
AS    
BEGIN    
IF NOT EXISTS (SELECT 1   FROM   CalibrationMeetings    WHERE  MeetingDate=@meetingdate and Irfid= @irfid)      
begin    
Insert into CalibrationMeetings (Irfid,MeetingDate,MeetingLink,CreatedOn,CreatedBy,ZoomMeetingId) values(@irfid,@meetingdate,@meetinglink,@CreatedOn,@Emailid,@meetingid)    
end    
else     
begin    
Update CalibrationMeetings set MeetingLink=@meetinglink,CreatedBy=@Emailid,CreatedOn=@CreatedOn,ZoomMeetingId=@meetingid    
WHERE  MeetingDate=@meetingdate and Irfid= @irfid    
end    
Select ul.EmailId from TransClientUser tc join UserList ul on tc.ClientUserId=ul.id where IRFId=@irfid  
union  
Select ul.EmailId From SkillChannelSMEs scs Join InterviewerProfile ip on ip.id=scs.InterviewerProfileId join UserList ul on ip.UserId=ul.id  
where scs.IRFId=@irfid  
END    
GO