IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_GetInterviewerQCRate'))
BEGIN
    DROP PROCEDURE dbo.usp_GetInterviewerQCRate
END

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[usp_GetInterviewerQCRate]          
@userId int
AS          
Begin  
declare @country int, @rate int

set @country = (select Country from InterviewerProfile where UserId =@userId)

if(@country =26) --India
	begin
		set @rate = 350
	end
else
	begin
		set @rate = 500
	end

select @rate

End 
GO