

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('Usp_ColdCandidateMarked'))
BEGIN
    DROP PROCEDURE dbo.Usp_ColdCandidateMarked
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- @ColdSource: AICall | HumanCall, or NULL for an ordinary portal mark (the convention the WhatsApp opt-out also uses); shown as the
-- "Opted out: AI call / Human call" badge on the cold lists. @ColdReason: opt-out quote or popup reason.
-- @LogCallOptOut = 1 (Mark cold popup "Opted out on call") also logs a candidate OptOut row in CallPreferences;
-- usp_RecordCandidateCallOptOut logs its own rows and leaves it 0. "Undo cold" (@type <> 1) clears source and reason.
CREATE PROCEDURE [dbo].[Usp_ColdCandidateMarked]
@ColdMarkedBY varchar(100),
@ColdMarkedDate datetime,
@FKCandidateId INT,
@InterviewId int,
@type int,
@Currentdate dateTime,
@ColdSource varchar(150) = NULL,
@ColdReason nvarchar(300) = NULL,
@LogCallOptOut bit = 0
AS
Begin
if(@type=1)
begin
SET @ColdSource = CASE WHEN @ColdSource IN ('AICall', 'HumanCall') THEN @ColdSource ELSE NULL END
Declare @oldIscandidateCold bit
select @oldIscandidateCold=IsColdCandidate From ColdCandidateInfos where FKCandidateId=@FKCandidateId
IF EXISTS(select * from ColdCandidateInfos where FKCandidateId=@FKCandidateId)
Begin
-- A call opt-out replaces an earlier mark (e.g. WhatsApp), so its ColdInfo text would describe the wrong mark.
update ColdCandidateInfos set IsColdCandidate=1,ColdMarkedBY=@ColdMarkedBY,ColdMarkedDate=@ColdMarkedDate,ColdSource=@ColdSource,ColdReason=@ColdReason,ColdInfo=CASE WHEN @ColdSource IS NOT NULL THEN NULL ELSE ColdInfo END where FKCandidateId=@FKCandidateId
End
Else
Begin
Insert Into ColdCandidateInfos(IsColdCandidate,ColdMarkedBY,ColdMarkedDate,FKCandidateId,ColdSource,ColdReason) values(1,@ColdMarkedBY,@ColdMarkedDate,@FKCandidateId,@ColdSource,@ColdReason)
End
declare @IRFId varchar(50)
declare @CandidateEmail varchar(150)
if(@InterviewId is not null and @InterviewId<>0)
Begin
--update InterviewMeetings set confirmationStatus=2,Cancellationsource='Cold Candidate' where id=@InterviewId
select @IRFId=IRFId,@CandidateEmail= candidateEmail from InterviewMeetings where id=@InterviewId
update InterviewMeetings set confirmationStatus=2,Cancellationsource=@ColdMarkedBY,CancellationReason='Cold Candidate' where IRFID=@IRFId and candidateEmail=@CandidateEmail and Interviewdate>@Currentdate
select CASE WHEN ISNULL(ul.IsActive, 0) = 1 THEN im.interviewer ELSE NULL END as interviewer,im.interviewDate,im.candidateName,im.id as InterviewId from InterviewMeetings im left join UserList ul on ul.EmailId = im.interviewer where im.IRFID=@IRFId and im.candidateEmail=@CandidateEmail and im.Interviewdate>@Currentdate
End
-- No interview passed: take the IRF code and email from the candidate, so the client status webhook still fires.
if(@IRFId is null or @CandidateEmail is null)
Begin
select @IRFId=ISNULL(@IRFId, irf.IRFId),@CandidateEmail=ISNULL(@CandidateEmail, ul.EmailId)
from Candidate c join IRF irf on irf.id=c.IRFId
left join CandidatesMaster cm on cm.id=c.CandidateMasterId left join UserList ul on ul.id=cm.UserId
where c.id=@FKCandidateId
End
--Add Cold Marked for webhook
 if(ISNULL(@oldIscandidateCold,0)!=1 and @CandidateEmail is not null and @IRFId is not null)
 begin
  Exec usp_AddCandidateStatusWebHookData @CandidateEmail, @IRFId, 'coldcandidate'
  end
if(@LogCallOptOut=1)
Begin
insert into CallPreferences (PersonType, CandidateId, IRFId, Phone, EmailId, Action, Source, Reason, CreatedOn, CreatedBy)
select 'Candidate', c.id, c.IRFId, dbo.fn_NormalizePhone(c.Phone), @CandidateEmail, 'OptOut', 'Portal', @ColdReason, @ColdMarkedDate, @ColdMarkedBY
from Candidate c where c.id=@FKCandidateId
End
END
else
Begin
update ColdCandidateInfos set IsColdCandidate=0,ModifyBy=@ColdMarkedBY,ModifyOn=@ColdMarkedDate,ColdSource=NULL,ColdReason=NULL where FKCandidateId=@FKCandidateId
END
End
GO
