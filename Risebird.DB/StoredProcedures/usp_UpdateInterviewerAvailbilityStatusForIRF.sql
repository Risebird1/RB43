IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInterviewerAvailbilityStatusForIRF'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInterviewerAvailbilityStatusForIRF
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateInterviewerAvailbilityStatusForIRF
@IRFId int,
@uniqueid varchar(50),
@Status int,
@timeNow datetime
AS
BEGIN
Declare @Count int
Declare @totalInterviewersCount int
Declare @InterviewerId int
Declare @UserId int

declare @timeDifference int = (select DATEDIFF(MINUTE, (select CreatedOn from IRF where id = @IRFId), @timeNow))
Select @Count=Count(*) from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid = @uniqueid
Select @totalInterviewersCount =Count(*) from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ti.IsAvailable = 1

if(@timeDifference < 180 and @totalInterviewersCount < 100)
Begin
if(@Count=0)
		BEGIN
			if(@Status=1)
				begin
					Select @UserId=id from UserList where uniqueid=@uniqueid
					Select @InterviewerId=id From InterviewerProfile where UserId=@UserId

					Insert into TransInterviewer(IRFId,InterviewerId,UserId,IsActive,IsAvailable) 
					values(@IRFId,@InterviewerId,@UserId,1,1)
				end
		END
ELSE
BEGIN
	if(@Status=1)
	begin
		Update ti set ti.IsAvailable=1 from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid=@uniqueid
	end
else
begin
	Update ti set ti.IsActive=0,IsAvailable=0 from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid=@uniqueid
end
END

End


if(@totalInterviewersCount>=100 and @Status != 1)
Begin

	Update ti set ti.IsActive=0,IsAvailable=0 from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid=@uniqueid

End

END
Go