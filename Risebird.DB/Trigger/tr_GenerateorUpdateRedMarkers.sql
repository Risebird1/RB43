IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_GenerateorUpdateRedMarkers')
BEGIN
    Drop trigger tr_GenerateorUpdateRedMarkers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
    
CREATE TRIGGER tr_GenerateorUpdateRedMarkers    
   ON  [dbo].[TransInterviewersReliabilityScores]    
   AFTER  INSERT,UPDATE    
AS     
BEGIN    
  Declare @InterviewerId int    
  Declare @InterviewerEmail varchar(200)    
  Declare @RedMarker int    
  Declare @InterviewMeetingId int,@IRFId varchar(20),@IRFintid int,@IRFRedMarker int  
 Select @InterviewerId = InterviewerId,@InterviewMeetingId=InterviewMeetingId from inserted    
 Select @InterviewerEmail=ul.EmailId From InterviewerProfile ip join UserList ul on ip.UserId=ul.id where ip.id=@InterviewerId    
 set @RedMarker=(Select dbo.fn_FecthRedMarkers(@InterviewerEmail) as RedMarker)    
 Update InterviewerProfile set RedMarker=@RedMarker where id=@InterviewerId    
 Select @IRFId=IRF.IRFId,@IRFintid=IRF.id from InterviewMeetings im join IRF on im.IRFId=IRF.IRFId  where im.id=@InterviewMeetingId  
 set @IRFRedMarker=(Select dbo.fn_FecthRedMarkersByIRFId(@InterviewerEmail,@IRFId) as RedMarker)    
 
if exists(select top 1 id from TransInterviewerRedMarker where IRFId=@IRFintid and InterviewerId=@InterviewerId)
begin
	Update TransInterviewerRedMarker set RedMarker=@IRFRedMarker where IRFId=@IRFintid and InterviewerId=@InterviewerId  
end
else
begin
	Insert into TransInterviewerRedMarker (IRFId,InterviewerId,RedMarker) values(@IRFintid,@InterviewerId,@IRFRedMarker)
end
END     
Go