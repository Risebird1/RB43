IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_UpdateIRFInterviewerActiveStatus'))

BEGIN

    DROP PROCEDURE dbo.usp_UpdateIRFInterviewerActiveStatus

END

SET ANSI_NULLS ON

GO

SET QUOTED_IDENTIFIER ON

GO

Create PROCEDURE usp_UpdateIRFInterviewerActiveStatus

@userId int,

@IsActive bit,

@irfId int,

@timeNow datetime,

@PerformedBy varchar(150),

@preserveAvailability bit = 0,

@IsAvailable bit = NULL

AS

BEGIN



if(@IsActive = 0)

Begin

Update TransInterviewer set IsActive = @IsActive, ModifiedBy = @PerformedBy, ModifiedAt = @timeNow,

    IsAvailable = CASE

        WHEN @IsAvailable IS NOT NULL THEN @IsAvailable

        WHEN @preserveAvailability = 1 THEN IsAvailable

        ELSE 0 END

where IRFId= @irfId and UserId = @userId

Update InterviewMeetings set Interviewer=null where IRFId in(Select IRFId From IRF where id=@IRFId) and interviewDate>DateAdd(Minute,330,GetDate())

and Interviewer in(Select EmailId From UserList where id=@userId)

End

Else

Begin

Update TransInterviewer set IsActive = @IsActive, ModifiedBy = @PerformedBy, ModifiedAt = @timeNow, ActivatedAt = @timeNow,

    IsAvailable = CASE WHEN @IsAvailable IS NOT NULL THEN @IsAvailable ELSE IsAvailable END

where IRFId= @irfId and UserId = @userId

End

END

Go



