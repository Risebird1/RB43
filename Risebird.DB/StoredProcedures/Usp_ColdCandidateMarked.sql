

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
@InterviewId int
AS
Begin
IF EXISTS(select * from ColdCandidateInfos where FKCandidateId=@FKCandidateId)
Begin
update ColdCandidateInfos set IsColdCandidate=0,ColdMarkedBY=@ColdMarkedBY,ColdMarkedDate=@ColdMarkedDate where FKCandidateId=@FKCandidateId
End
Else
Begin
Insert Into ColdCandidateInfos(IsColdCandidate,ColdMarkedBY,ColdMarkedDate,FKCandidateId) values(1,@ColdMarkedBY,@ColdMarkedDate,@FKCandidateId)
End
if(@InterviewId is not null and @InterviewId<>0)
Begin
update InterviewMeetings set confirmationStatus=2,Cancellationsource='Cold Candidate' where id=@InterviewId
End
End
GO


