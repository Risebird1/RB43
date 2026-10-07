/****** Object:  StoredProcedure [dbo].[usp_DeactivateSkillChannelInterviewer]    ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeactivateSkillChannelInterviewer'))
BEGIN
    DROP PROCEDURE [dbo].[usp_DeactivateSkillChannelInterviewer]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

Create PROCEDURE [dbo].[usp_DeactivateSkillChannelInterviewer]
@irfId int,
@interviewerEmail varchar(100),
@removedBy varchar(150) = null, -- session user who removed the interviewer (QC Activity dashboard); null = not logged
@logType varchar(30) = null    -- RedMarkerStatusDetails.Type for the log row: IRF (Total Red Marker page) or SkillChannelRemoval
AS
BEGIN
Declare @IRF varchar(50)
Select @IRF=@IRFId From IRF where id=@irfId
  Declare @interviewerUserId int = (select id from UserList where EmailId = @interviewerEmail)
		
	IF EXISTS (SELECT 1 FROM TransInterviewer where IRFId = @irfId and UserId = @interviewerUserId) 
	BEGIN
		update TransInterviewer set IsActive = 0,IsAvailable=0 where IRFId = @irfId and UserId = @interviewerUserId
		update InterviewMeetings set Interviewer=null where IRFId=@IRF and Interviewer=@interviewerEmail and interviewDate>DateAdd(MINUTE,330, Getdate())
		IF NULLIF(LTRIM(RTRIM(@removedBy)), '') IS NOT NULL
		BEGIN
			-- Who removed the interviewer: Status 3 = Removed, IST time like usp_UpdateRedMarkeredInterviewerstatus
			Insert into RedMarkerStatusDetails(InterviewerUserid,IRFId,Comment,Type,CreatedOn,CreatedBy,Status)
			values(@interviewerUserId,@irfId,'Removed from skill channel',ISNULL(NULLIF(LTRIM(RTRIM(@logType)), ''),'SkillChannelRemoval'),DateAdd(Minute,330,GETDATE()),LTRIM(RTRIM(@removedBy)),3)
		END
	END
END
GO

