  IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_FirstInterviewMovetoTechQC'))
BEGIN
    DROP PROCEDURE dbo.usp_FirstInterviewMovetoTechQC
END

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
Create Procedure usp_FirstInterviewMovetoTechQC
@interviewid int
AS
BEGIN
Declare @IRFId varchar(15)
Declare @interviewer varchar(150)
Declare @firstinterviewid int
Select @IRFId=IRFId,@interviewer=interviewer From InterviewMeetings where id=@interviewid
;with CTE as(Select top 1 im.id,im.IRFId,im.interviewer,im.Interviewdate,ic.MeetingStatus from InterviewMeetings im join InterviewComments ic on im.id=ic.InterviewId where IRFId=@IRFId and interviewer=@interviewer and confirmationStatus=1 
and (InterviewStatus=2 or InterviewStatus=3 or InterviewStatus=4 or InterviewStatus=5) 
order by interviewdate)
Select @firstinterviewid=id From CTE

if(@interviewid=@firstinterviewid)
begin
declare @Createdon datetime=DateAdd(MInute,330,Getdate())
Exec usp_SaveTechQCDetails 'System',@interviewid,1,@Createdon
end
END
GO