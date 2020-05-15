/****** Object:  StoredProcedure [dbo].[usp_AddRecruiter]    Script Date: 08-01-2020 6.44.13 PM ******/
IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID('usp_AddSuitableCandidate'))
BEGIN
    DROP PROCEDURE dbo.usp_AddSuitableCandidate
END

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE Procedure usp_AddSuitableCandidate  
@InterviewId int,  
@EmailId varchar(max),  
@Comment varchar(400)  
AS  
BEGIN  
DECLARE @Id int  
Select @Id=id from UserList where EmailId=@EmailId  
Insert Into SuitableCandidate values(@InterviewId,@Id,'Interest Sent',@Comment,GetDate())  
Select CandidateEmail from InterviewMeetings where id=@InterviewId  
END
GO
