/****** Object:  StoredProcedure [dbo].[usp_DeleteInterviewerProfile]    Script Date: 5/1/2020 4:03:27 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_DeleteInterviewerProfile'))
BEGIN
    DROP PROCEDURE [dbo].[usp_DeleteInterviewerProfile]
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_DeleteInterviewerProfile]
@UserId int
AS
  Declare @v_ProfileId int;
		
	IF EXISTS (SELECT 1 FROM UserList where Id = @UserId) 
	  BEGIN

		-- Delete from Each Trans Interviewer Table
		
		 select @v_ProfileId = id from [dbo].[InterviewerProfile] where UserId = @UserId;

			delete from TransInterviewerSkill where InterviewerProfileId=@v_ProfileId

			delete from TransInterviewerEducation where InterviewerProfileId=@v_ProfileId

			delete from TransInterviewerProfessional where InterviewerProfileId=@v_ProfileId

			delete from TransInterviewerProjects where InterviewerProfileId=@v_ProfileId

			delete from TransInterviewerAvailability where InterviewerProfileId=@v_ProfileId

			delete from TransInterviewerAccDetails where InterviewerProfileId=@v_ProfileId

			delete from TransInterviewerCertificate where InterviewerProfileId=@v_ProfileId

			delete from TransInterviewerListInterviewerSkills where UserId = @UserId

			delete from TransInterviewerSkillsLeadInterviewer where UserId = @UserId

		  -- Now delete from InterviewerProfile Table
		  delete from InterviewerProfile where UserId = @UserId
		  --delete from Interviewer where UserId = @UserId
	      -- At last, Delete from User List table
		  delete from UserList where Id = @UserId
   
	   END

 




GO

