IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.sp_RisebirdInterviewer'))
BEGIN
    DROP PROCEDURE dbo.sp_RisebirdInterviewer
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_RisebirdInterviewer] 
  
AS
BEGIN
select inter.id, ul.FirstName,ul.LastName,ul.EmailId,Ul.MobileNumber,ul.idProfile, inter.PrimarySkill,inter.ResumeLink,inter.Slots, inter.NoOfInterview,inter.SuccessRate,inter.IsApproved from [Interviewer] inter
inner join [dbo].[UserList] ul on inter.UserId = ul.id
where ul.IsActive =1

END
GO 




