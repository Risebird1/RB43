/****** Object:  StoredProcedure [dbo].[usp_CheckIfInterviewerExistsInSystemAndOrSkillChannel]    Script Date: 25-09-2019 7.40.51 PM ******/

IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_CheckIfInterviewerExistsInSystemAndOrSkillChannel'))
BEGIN
    DROP PROCEDURE usp_CheckIfInterviewerExistsInSystemAndOrSkillChannel
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

create PROCEDURE [dbo].[usp_CheckIfInterviewerExistsInSystemAndOrSkillChannel]     
@emailID varchar(200),
@irfId int
AS      
BEGIN   
declare @userId int = (select id from UserList where EmailId = @emailID)

declare @trInterviewerId int = (select id from TransInterviewer where irfId =@irfId and UserId = @userId)

select case when @userId > 0 then 1 else 0 End  as IsExistsInSystem,
case when @trInterviewerId > 0 then 1 else 0 End  as AlreadyExistsInSkillChannel

      
END   
GO




