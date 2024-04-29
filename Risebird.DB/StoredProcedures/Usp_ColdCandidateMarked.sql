

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_ColdCandidateMarked'))
BEGIN
    DROP PROCEDURE dbo.Usp_ColdCandidateMarked
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[Usp_ColdCandidateMarked]
@ColdMarkedBY varchar(100),
@ColdMarkedDate datetime,
@FKCandidateId INT,
@InterviewId int,
@type int,
@Currentdate dateTime
AS
Begin
if(@type=1)
begin
IF EXISTS(select * from ColdCandidateInfos where FKCandidateId=@FKCandidateId)
Begin
update ColdCandidateInfos set IsColdCandidate=1,ColdMarkedBY=@ColdMarkedBY,ColdMarkedDate=@ColdMarkedDate where FKCandidateId=@FKCandidateId
End
Else
Begin
Insert Into ColdCandidateInfos(IsColdCandidate,ColdMarkedBY,ColdMarkedDate,FKCandidateId) values(1,@ColdMarkedBY,@ColdMarkedDate,@FKCandidateId)
End
if(@InterviewId is not null and @InterviewId<>0)
Begin
--update InterviewMeetings set confirmationStatus=2,Cancellationsource='Cold Candidate' where id=@InterviewId
declare @IRFId varchar(50)
declare @CandidateEmail varchar(150)
select @IRFId=IRFId,@CandidateEmail= candidateEmail from InterviewMeetings where id=@InterviewId 
update InterviewMeetings set confirmationStatus=2,Cancellationsource='Cold Candidate' where IRFID=@IRFId and candidateEmail=@CandidateEmail and Interviewdate>@Currentdate
select interviewer,interviewDate from InterviewMeetings where IRFID=@IRFId and candidateEmail=@CandidateEmail and Interviewdate>@Currentdate
End
END
else
Begin
update ColdCandidateInfos set IsColdCandidate=0,ModifyBy=@ColdMarkedBY,ModifyOn=@ColdMarkedDate where FKCandidateId=@FKCandidateId
END
End
GO


