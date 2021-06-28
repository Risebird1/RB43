-- ==============================================
-- Create dml trigger template Azure SQL Database 
-- ==============================================
-- Drop the dml trigger if it already exists
IF EXISTS(
  SELECT *
    FROM sys.triggers
   WHERE name = N'ZoomMeetingCombination'
     AND parent_class_desc = N'OBJECT_OR_COLUMN'
)
	DROP TRIGGER ZoomMeetingCombination
GO

CREATE TRIGGER ZoomMeetingCombination
   ON  ZoomMeetingTransmission
   AFTER INSERT
AS 
BEGIN
	SET NOCOUNT ON;
	Declare @Meetingid varchar(25)
	Declare @ParticipantName varchar(200)
	Select @Meetingid=i.Meetingid,@ParticipantName=i.ParticipantName from inserted i
	Select @Meetingid
	Declare @joinedCount int
	Declare @leftcount int
	Declare @Whojoined varchar(max)
	Select @joinedCount=Count(*) From ZoomMeetingTransmission where Meetingid=@Meetingid and Event='PARTICIPANT_JOINED'
	Select @leftcount=Count(*) From ZoomMeetingTransmission where Meetingid=@Meetingid and Event='PARTICIPANT_LEFT'

	set @Whojoined=(select distinct STUFF((SELECT distinct ', ' + ISNULL(ul.EmailId,t3.ParticipantName) from 
	(Select * from ZoomMeetingTransmission Where Event='PARTICIPANT_JOINED' and Meetingid=@Meetingid )t3 
	left join InterviewerProfile ip on ip.NickName=t3.ParticipantName 
	left join USERList ul on ul.id=ip.UserId FOR XML PATH(''), TYPE).value('.', 'NVARCHAR(MAX)'),1,2,'')WhoJoined 
	from ZoomMeetingTransmission t where t.Meetingid=@Meetingid)
	Select 1 from TransZoomMeetingExtendDetails where Meetingid=@Meetingid
	if Not Exists (Select 1 from TransZoomMeetingExtendDetails where Meetingid=@Meetingid)
	BEGIN
	Insert into TransZoomMeetingExtendDetails(Meetingid,Joinedcount,Leftcount,WhoJoined) values(@Meetingid,@joinedCount,@leftcount,@Whojoined)
	END
	Else
	BEGIN
	Update TransZoomMeetingExtendDetails Set WhoJoined=@Whojoined,Joinedcount=@joinedCount,Leftcount=@leftcount Where Meetingid=@Meetingid
	END
END
GO
