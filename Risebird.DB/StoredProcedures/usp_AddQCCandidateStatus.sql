/****** Object:  StoredProcedure [dbo].[usp_AddQCCandidateStatus    Script Date: 24-11-2020 2.34.29 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddQCCandidateStatus'))
BEGIN
    DROP PROCEDURE dbo.usp_AddQCCandidateStatus
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


Create PROCEDURE [dbo].[usp_AddQCCandidateStatus]
@InterviewId int,
@Date datetime,
@PerformedBy varchar(100)
    
AS
BEGIN

declare @irfId int, @candidateMasterId int, @status int, @existingInterviewStatusComment varchar(max), @statusDescription varchar(max);
	set @irfId = (select irf.id from InterviewMeetings im join IRF on irf.IRFId=im.IRFId where im.id = @InterviewId)
	set @candidateMasterId = (select cm.id from InterviewMeetings im join UserList ul on ul.EmailId=im.candidateEmail join CandidatesMaster cm on cm.UserId=ul.id where im.id = @InterviewId)
	set @status = (select InterviewStatus from InterviewComments where InterviewId=@InterviewId)
	
	set @existingInterviewStatusComment = (select top 1 Comments from CandidateStatus where CandidateId=@candidateMasterId and IRFId =@irfId and InterviewId =@InterviewId and Status = 15  order by id desc)
	
	if(@status=2)
		Begin
			set @statusDescription  = 'Selected'
		End
	else if(@status=4)
		Begin
			set @statusDescription  = 'Border Line'

		End
	if(@existingInterviewStatusComment is null or (@existingInterviewStatusComment != @statusDescription))
		Begin
			--if(@status=2)
			--	Begin
			--		INSERT INTO CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], [PerformedBy], InterviewId)
			--		VALUES (@irfId, @candidateMasterId, 'Selected', @Date, 15, @PerformedBy, @InterviewId)
			--	End
			--if(@status=4)

			--	Begin
			--	  INSERT INTO CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], [PerformedBy], InterviewId)
			--	  VALUES (@irfId, @candidateMasterId, 'Border Line', @Date, 15, @PerformedBy, @InterviewId)
			--	End

				INSERT INTO CandidateStatus (IRFId, CandidateId, Comments, [Date], [Status], [PerformedBy], InterviewId)
				VALUES (@irfId, @candidateMasterId, @statusDescription, @Date, 15, @PerformedBy, @InterviewId)
		End
	
	
	
      
END

GO

