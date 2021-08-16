IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_UpdateInstaMeeting'))
BEGIN
    DROP PROCEDURE dbo.usp_UpdateInstaMeeting
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO  
Create Procedure usp_UpdateInstaMeeting
@Id int,
@RequiredSkill varchar(150),
@CandidateName varchar(100),
@AssessmentGroup int,
@ResumeLink varchar(1000),
@JDLink varchar(1000),
@RequestedBy varchar(150)
As
BEGIN
IF NOT EXISTS (SELECT 1   FROM   InstaInterviewMeetings WHERE Id=@Id)  
Begin  
Update InstaInterviewMeetings Set RequiredSkill=ISNULL(@RequiredSkill,RequiredSkill),
CandidateName=ISNULL(@CandidateName,CandidateName),
AssessmentGroup=ISNULL(@AssessmentGroup,AssessmentGroup),
ResumeLink=ISNULL(@ResumeLink,ResumeLink),
JDLink=ISNULL(@JDLink,JDLink),
RequestedBy=ISNULL(@RequestedBy,RequestedBy)
where Id=@Id
END
END
