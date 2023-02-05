/****** Object:  StoredProcedure [dbo].[usp_CheckIfSME]    Script Date: 25-09-2019 7.40.51 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('dbo.usp_CheckIfSME'))
BEGIN
    DROP PROCEDURE dbo.usp_CheckIfSME
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[usp_CheckIfSME]               
@emailID varchar(100)                  
AS                
BEGIN          
      
declare @emailIDParam varchar(100), @interviewerProfileId int, @isSME bit = 0      
set @emailIDParam=@emailID     

set @interviewerProfileId = (select ipr.Id from InterviewerProfile ipr join UserList ul on ul.id = ipr.UserId where ul.EmailId like @emailIDParam)

if exists(select top 1 id from SkillChannelSMEs where InterviewerProfileId = @interviewerProfileId and (IsDeleted is null or IsDeleted=0))  
Begin
	set @isSME = 1
End

select @isSME                       
                    
END
GO

