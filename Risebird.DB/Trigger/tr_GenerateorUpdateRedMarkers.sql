IF EXISTS (SELECT 1 FROM sys.triggers 
           WHERE Name = 'tr_GenerateorUpdateRedMarkers')
BEGIN
    Drop trigger tr_GenerateorUpdateRedMarkers
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create TRIGGER tr_GenerateorUpdateRedMarkers
   ON  [dbo].[TransInterviewersReliabilityScores]
   AFTER  INSERT,UPDATE
AS 
BEGIN
	 Declare @InterviewerId int
	 Declare @InterviewerEmail varchar(200)
	 Declare @RedMarker int
	Select @InterviewerId = InterviewerId from inserted
	Select @InterviewerEmail=ul.EmailId From InterviewerProfile ip join UserList ul on ip.UserId=ul.id where ip.id=@InterviewerId
	set @RedMarker=(Select dbo.fn_FecthRedMarkers(@InterviewerEmail) as RedMarker)
	Update InterviewerProfile set RedMarker=@RedMarker where id=@InterviewerId
END
Go