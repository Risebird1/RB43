IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateIntervieweravailbilityStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateIntervieweravailbilityStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE usp_UpdateIntervieweravailbilityStatus
@IRFId int,
@uniqueid varchar(50),
@Status int
AS
BEGIN
Declare @Count int
Declare @InterviewerId int
Declare @UserId int
Select @Count=Count(*) from TransInterviewer ti join UserList ul on ul.id=ti.Userid Where IRFId=@IRFId and ul.uniqueid=@uniqueid
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
END
Go