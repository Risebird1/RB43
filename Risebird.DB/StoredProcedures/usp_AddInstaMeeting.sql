IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddInstaMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_AddInstaMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
Create PROCEDURE [dbo].[usp_AddInstaMeeting]  
@RequiredSkill varchar(150),
@CandidateName varchar(100),
@AssessmentGroup int,
@ResumeLink varchar(1000),
@JDLink varchar(1000),
@RequestedBy varchar(150)
AS  
Begin  
  
IF NOT EXISTS (SELECT 1   FROM   InstaInterviewMeetings    WHERE  CandidateName = @CandidateName and ResumeLink=@ResumeLink)  
Begin  
 Insert into [InstaInterviewMeetings](RequestedDate,RequiredSkill,CandidateName,AssessmentGroup,ResumeLink,JDLink,RequestedBy)
 values(dateadd(minute,330,GetDate()),@RequiredSkill,@CandidateName,@AssessmentGroup,@ResumeLink,@JDLink,@RequestedBy)

End  
END  